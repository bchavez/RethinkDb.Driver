using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Threading;
using BauCore;
using BauMSBuild;
using BauNuGet;
using BauExec;
using Builder.Extensions;
using FluentAssertions;
using FluentBuild;
using Microsoft.Owin.Hosting;
using Nancy;
using Nancy.Responses;
using Owin;
using RestSharp;
using Templates.Metadata;
using Z.ExtensionMethods;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Builder
{
    public static class BauBuild
    {
        //Build Tasks
        public const string MsBuild = "msb";
        public const string DnxBuild = "dnx";
        public const string MonoBuild = "mono";
        public const string Clean = "clean";
        public const string Restore = "restore";
        public const string DnxRestore = "dnxrestore";
        public const string BuildInfo = "buildinfo";
        public const string AstGen = "astgen";
        public const string YamlImport = "yamlimport";
        public const string TestGen = "testgen";
        public const string Pack = "pack";
        public const string Push = "push";

        public const string ci = "ci";
        public const string citest = "citest";

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~");
                    Console.WriteLine("     BUILDER ERROR    ");
                    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~");
                    Console.WriteLine(e.ExceptionObject);
                    Environment.Exit(1);
                };

            var nugetExe = FindNugetExe();

            bau = new Bau(Arguments.Parse(args));

            //By default, no build arguments...
            bau.DependsOn(Clean, Restore, MsBuild)
                //Define
                .MSBuild(MsBuild).Desc("Invokes MSBuild to build solution")
                .DependsOn(Clean, Restore, BuildInfo, AstGen)
                .Do(msb =>
                    {
                        msb.ToolsVersion = "14.0";
                        msb.MSBuildVersion = "VS14"; //Hack for MSBuild VS2015

                        msb.Solution = Projects.SolutionFile.ToString();
                        msb.Properties = new
                            {
                                Configuration = "Release",
                                RestorePackages = false, //we already got them from build.cmd
                                //OutDir = Folders.CompileOutput
                            };
                        msb.Targets = new[] {"Rebuild"};
                    })

                //Define
                .Exec(DnxBuild).Desc("Build .NET Core Assemblies")
                .DependsOn(Clean, DnxRestore, BuildInfo, AstGen)
                .Do(exec =>
                    {
                        exec.Run("powershell")
                            .With(
                                $"dnvm use {Projects.DnmvVersion} -r clr -p;",
                                $"dnu build --configuration Release --out {Projects.DriverProject.OutputDirectory};"
                            ).In(Projects.DriverProject.Folder.ToString());
                    })

                //Define
                .Task(DnxRestore).Desc("Restores .NET Core dependencies")
                .Do(() =>
                    {
                        Task.Run.Executable(e =>
                            {
                                e.ExecutablePath("powershell")
                                    .WithArguments(
                                        "dnvm update-self;",
                                        $"dnvm install {Projects.DnmvVersion} -r clr;",
                                        $"dnvm use {Projects.DnmvVersion} -r clr -p;",
                                        "dnu restore --fallbacksource https://www.myget.org/F/aspnetvnext/api/v2/"
                                    ).InWorkingDirectory(Projects.DriverProject.Folder);
                            });
                    })

                //Define
                .Task(BuildInfo).Desc("Creates dynamic AssemblyInfos for projects")
                .Do(() =>
                    {
                        task.LogInfo("Injecting AssemblyInfo.cs");
                        Task.CreateAssemblyInfo.Language.CSharp(aid =>
                            {
                                Projects.DriverProject.AssemblyInfo(aid);
                                var outputPath = Projects.DriverProject.Folder.SubFolder("Properties").File("AssemblyInfo.cs");
                                Console.WriteLine($"Creating AssemblyInfo file: {outputPath}");
                                aid.OutputPath(outputPath);
                            });

                        task.LogInfo("Injecting DNX project.json with Nuspec");
                        //version
                        WriteJson.Value(Projects.DriverProject.DnxProjectFile.ToString(), "version", BuildContext.FullVersion);
                        //description
                        WriteJson.Value(Projects.DriverProject.DnxProjectFile.ToString(), "description",
                            ReadXml.From(Projects.DriverProject.NugetSpec.ToString(), "package.metadata.summary"));
                        //projectUrl
                        WriteJson.Value(Projects.DriverProject.DnxProjectFile.ToString(), "projectUrl",
                            ReadXml.From(Projects.DriverProject.NugetSpec.ToString(), "package.metadata.projectUrl"));
                        //license
                        WriteJson.Value(Projects.DriverProject.DnxProjectFile.ToString(), "licenseUrl",
                            ReadXml.From(Projects.DriverProject.NugetSpec.ToString(), "package.metadata.licenseUrl"));
                    })

                //Define
                .Task(AstGen).Desc("Regenerates C# AST classes")
                .Do(() =>
                    {
                        Directory.SetCurrentDirectory(Projects.DriverProject.Folder.ToString());
                        MetaDb.Initialize(Projects.TemplatesProject.Metadata.ToString());

                        var gen = new Templates.GeneratorForAst();
                        gen.EnsurePathsExist();
                        gen.Generate_All();
                    })

                //Define
                .Task(YamlImport).Desc("Imports fresh YAML files and cleans them up")
                .Do(() =>
                    {
                        Directory.SetCurrentDirectory(Projects.Tests.Folder.ToString());

                        var gen = new Templates.GeneratorForUnitTests();
                        gen.EnsurePathsExist();
                        gen.CleanUpYamlTests();
                    })

                //Define
                .Task(TestGen).Desc("Generates C# unit tests from refined YAML files.")
                .Do(() =>
                    {
                        Directory.SetCurrentDirectory(Projects.Tests.Folder.ToString());

                        var gen = new Templates.GeneratorForUnitTests();
                        gen.EnsurePathsExist();
                        gen.Generate_All();
                    })

                //Define
                .Exec(MonoBuild).Desc("Produces runs the mono xbuild.")
                .Do(exec =>
                    {
                        var monopath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Mono\bin";
                        exec.Run("cmd.exe")
                            .With("/c",
                                $@"""{monopath}\setmonopath.bat"" & ",
                                $@"xbuild.bat {Projects.DriverProject.ProjectFile.ToString()} /p:OutDir={Projects.DriverProject.OutputDirectory}\Release\mono\"
                            ).In(Projects.DriverProject.Folder.ToString());
                    })

                //Define
                .Task(Clean).Desc("Cleans project files")
                .Do(() =>
                    {
                        Console.WriteLine($"Removing {Folders.CompileOutput}");
                        Folders.CompileOutput.Wipe();
                        Directory.CreateDirectory(Folders.CompileOutput.ToString());
                        Console.WriteLine($"Removing {Folders.Package}");
                        Folders.Package.Wipe();
                        Directory.CreateDirectory(Folders.Package.ToString());
                    })

                //Define
                .NuGet(Pack).Desc("Packs NuGet packages")
                .DependsOn(DnxBuild).Do(ng =>
                    {
                        ng.Pack(Projects.DriverProject.NugetSpec.ToString(),
                            p =>
                                {
                                    p.BasePath = Projects.DriverProject.OutputDirectory.ToString();
                                    p.Version = BuildContext.FullVersion;
                                    p.Symbols = true;
                                    p.OutputDirectory = Folders.Package.ToString();
                                })
                            .WithNuGetExePathOverride(nugetExe.FullName);
                    })

                //Define
                .NuGet(Push).Desc("Pushes NuGet packages")
                .DependsOn(Pack).Do(ng =>
                    {
                        ng.Push(Projects.DriverProject.NugetNupkg.ToString())
                            .WithNuGetExePathOverride(nugetExe.FullName);
                    })

                //Define
                .NuGet(Restore).Desc("Restores NuGet packages")
                .Do(ng =>
                    {
                        ng.Restore(Projects.SolutionFile.ToString())
                            .WithNuGetExePathOverride(nugetExe.FullName);
                    })

                //Define
                .Task(ci).Desc("Use by appveyor for continuous integration builds. Not to be used.")
                .DependsOn(MsBuild, MonoBuild, Pack)
                .Do(() =>
                    {
                        //We just use this task to depend on Pack (dnx build) and MSBuild
                        //to ensure MS build gets called so we know *everything* compiles, including
                        //unit tests.
                    })

                .Task(citest).Desc("Temporarily hosts build artifacts for testing.")
                .Do(() =>
                    {
                        while( true )
                        {
                            var hosturl = WebTestHost.RandomHostUrl();
                            try
                            {
                                using( WebApp.Start<Startup>(hosturl) )
                                {
                                    var hostEndpoint = hosturl.Replace("+", WebTestHost.GetPreferedIp());
                                    task.LogInfo($"WebTesthost on port: {hostEndpoint}");

                                    //Trigger Remote Test System
                                    var client = WebTestHost.GetRestClient();
                                    var test = WebTestHost.RequestUnitTests(hostEndpoint);
                                    var response = client.Execute(test);

                                    if ( response.StatusCode != HttpStatusCode.Accepted )
                                    {
                                        throw new RemotingException("Couldn't successfully trigger unit tests in remote system.");
                                    }

                                    while( !WebTestHost.Done )
                                    {
                                        Thread.Sleep(5000);
                                    }
                                }
                            }
                            catch(Exception e)
                            {
                                throw e;
                            }
                        }

                    });


            bau.Run();
        }

        

        private static FileInfo FindNugetExe()
        {
            Directory.SetCurrentDirectory(Folders.Lib.ToString());

            var nugetExe = NuGetFileFinder.FindFile();
            nugetExe.Should().NotBeNull();

            Directory.SetCurrentDirectory(Folders.WorkingFolder.ToString());

            return nugetExe;
        }

        private static Bau bau;

        public static IBauTask task => bau.CurrentTask;
    }

    public class WebTestHost : NancyModule
    {
        public WebTestHost()
        {
            GenericFileResponse.SafePaths.Add(Folders.Package.ToString());

            Get["/download"] = p => Response.AsFile(Projects.DriverProject.Zip.ToString());

            Post["/result"] = p =>
                {
                    Done = true;
                    return "OK";
                };
        }


        public static Random r = new Random();
        public static string RandomHostUrl()
        {
            var newPort = r.Next(49152, 65535);
            return $"http://+:{newPort}";
        }

        public static string GetPreferedIp()
        {
            using( var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0) )
            {
                s.Connect("111.111.111.111", 65530);
                var endPoint = s.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }

        }

        public static RestClient GetRestClient()
        {
            return new RestClient("https://circleci.com/api/v1/")
                {
                    Proxy = new WebProxy("http://localhost:8888"),
                };
        }
        public static RestRequest RequestUnitTests(string webhostUrl)
        {
            var req = new RestRequest($"/project/bchavez/RethinkDb.Driver/tree/master", Method.POST);

            req.AddQueryParameter("circle-token", Environment.GetEnvironmentVariable("test_token"));

            var body = new
                {
                    build_parameters = new
                        {
                            webhost = webhostUrl
                        }
                };

            req.AddJsonBody(body);

            return req;
        }

        public static bool Done { get; private set; }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var p = app.Properties;
            app.UseNancy();
        }
    }
}