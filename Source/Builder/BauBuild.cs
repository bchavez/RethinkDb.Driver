using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Remoting;
using System.Threading;
using BauCore;
using BauMSBuild;
using BauNuGet;
using BauExec;
using Builder.Extensions;
using FluentAssertions;
using FluentBuild;
using FluentFs.Core;
using Newtonsoft.Json.Linq;
using RestSharp;
using Templates.Metadata;
using Z.ExtensionMethods;
using Directory = System.IO.Directory;


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
                                        "dnu restore"
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
                        var nuspec = Projects.DriverProject.NugetSpec.WithExt("history.nuspec");
                        nuspec.Delete(OnError.Continue);

                        Projects.DriverProject.NugetSpec
                            .Copy
                            .ReplaceToken("history")
                            .With(History.NugetText())
                            .To(nuspec.ToString());

                        ng.Pack(nuspec.ToString(),
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
                        var tag = Environment.GetEnvironmentVariable("APPVEYOR_REPO_TAG_NAME");
                        if( tag.IsNotNullOrWhiteSpace() )
                        {
                            task.LogInfo("Setting HISTORY_ID notes...");
                            var historyId = BuildContext.FullVersion.Replace(".", "");
                            AppVeyor.SetBuildVariable("HISTORY_ID", historyId);
                        }
                    })

                .Task(citest).Desc("Triggers unit tests.")
                .Do(() =>
                    {
                        task.LogInfo("Triggering unit test system.");
                        var circleToken = Environment.GetEnvironmentVariable("circleci_token");
                        var jobId = Environment.GetEnvironmentVariable("APPVEYOR_JOB_ID");

                        if ( circleToken.IsNullOrWhiteSpace() )
                        {
                            task.LogInfo("Skipping Unit tests. This must be a pull request. Encrypted test token isn't available.");
                            return;
                        }


                        var client = CircleCi.GetRestClient();
                        var testReq = CircleCi.GetTestRequest(jobId, circleToken);
                        var startTestResp = client.Execute(testReq);

                        if (startTestResp.StatusCode != HttpStatusCode.Created)
                        {
                            throw new RemotingException("Couldn't successfully trigger unit tests in remote system.");
                        }

                        var startTest = JObject.Parse(startTestResp.Content);

                        var buildUrl = startTest["build_url"].ToObject<string>();
                        task.LogInfo($"Test System URL: {buildUrl}");

                        var buildNum = startTest["build_num"].ToObject<int>();

                        var initialWait = TimeSpan.FromMinutes(5);
                        task.LogInfo($"Waiting for about {initialWait}.");
                        Thread.Sleep(initialWait);

                       
                        while ( true )
                        {
                            var summaryReq = CircleCi.GetBuildStatus(buildNum, circleToken);
                            var summaryResp = client.Execute(summaryReq);

                            if( summaryResp.StatusCode != HttpStatusCode.OK )
                            {
                                task.LogError("Problem getting build summary.");
                                break;
                            }

                            var summary = JObject.Parse(summaryResp.Content);
                            var status = summary["status"].ToObject<string>();

                            if( status != "running" )
                            {
                                task.LogInfo($"Done waiting. Test system status: {status}");
                                break;
                            }
                                                        
                            task.LogInfo("Waiting for unit tests to complete.");
                            Thread.Sleep(10000);
                        }

                        var testResultsUrl = string.Empty;
                        var atrifactsReq = CircleCi.GetTestArtifacts(buildNum, circleToken);
                        var artifactsResp = client.Execute(atrifactsReq);

                        if (artifactsResp.StatusCode == HttpStatusCode.OK)
                        {
                            var artifacts = JArray.Parse(artifactsResp.Content);
                            if (artifacts.Count > 0)
                            {
                                testResultsUrl = artifacts[0]["url"].ToString();
                            }
                        }

                        if( !testResultsUrl.IsNullOrWhiteSpace() )
                        {
                            task.LogInfo($"Got results: {testResultsUrl}");

                            AppVeyor.UploadTestResults(jobId, testResultsUrl);
                        }
                        else
                        {
                            task.LogWarn("Couldn't find any tests results.");
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

    public static class AppVeyor
    {
        public static void UploadTestResults(string jobId, string xmlResultsUrl)
        {
            const string FileName = "TestResults.xml";
            var web = new WebClient();
            web.DownloadFile(xmlResultsUrl, FileName); //download from circle CI
            var uploadUrl = $"https://ci.appveyor.com/api/testresults/nunit/{jobId}";
            web.UploadFile(uploadUrl, FileName); //upload to AppVeyor
        }


        private static RestClient GetClient()
        {
            var api = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");
            var client = new RestClient(api);
            return client;
        }
        public static void SetBuildVariable(string name, string value)
        {
            var client = GetClient();
            var req = new RestRequest("/api/build/variables", Method.POST);
            req.AddJsonBody(new {name, value});
            var resp = client.Execute(req);
        }
    }

    public static class CircleCi
    {
        public static RestClient GetRestClient()
        {
            return new RestClient("https://circleci.com/api/v1/")
                {
                    //Proxy = new WebProxy("http://localhost:8888"),
                };
        }

        public static RestRequest GetTestRequest(string jobId, string circleciToken)
        {

            var body = new
                {
                    build_parameters = new
                        {
                            AppVeyorJobId = jobId
                        }
                };


            var req = new RestRequest("/project/bchavez/RethinkDb.Driver/tree/master", Method.POST);

            req.AddQueryParameter("circle-token", circleciToken);

            req.AddJsonBody(body);

            return req;

        }

        public static RestRequest GetBuildStatus(int buildNum, string circleciToken)
        {
            var req = new RestRequest("/project/bchavez/RethinkDb.Driver/{build_num}", Method.GET);
            req.AddUrlSegment("build_num", buildNum.ToString());
            req.AddQueryParameter("circle-token", circleciToken);
            return req;
        }

        public static RestRequest GetTestArtifacts(int buildNum, string circleciToken)
        {
            var req = new RestRequest("/project/bchavez/RethinkDb.Driver/{build_num}/artifacts");
            req.AddUrlSegment("build_num", buildNum.ToString());

            req.AddQueryParameter("circle-token", circleciToken);
            return req;
        }
    }

}