using System;
using System.IO;
using System.Runtime.CompilerServices;
using BauCore;
using BauMSBuild;
using BauNuGet;
using BauExec;
using Builder.Extensions;
using FluentAssertions;
using FluentBuild;
using Templates.Metadata;

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

            var bau = new Bau(Arguments.Parse(args));
            
            //By default, no build arguments...
            bau.DependsOn(Clean, Restore, MsBuild)
                //Define
                .MSBuild(MsBuild).Desc("Invokes MSBuild to build solution")
                .DependsOn(Clean, BuildInfo, AstGen)
                .Do(msb =>
                    {
                        msb.ToolsVersion = "14.0";
                        msb.MSBuildVersion = "VS14"; //Hack for MSBuild VS2015

                        msb.Solution = Projects.SolutionFile.ToString();
                        msb.Properties = new
                            {
                                Configuration = "Release",
                                //OutDir = Folders.CompileOutput
                            };
                        msb.Targets = new[] {"Rebuild"};
                    })

                //Define
                .Exec(DnxBuild).Desc("Build .NET Core Assemblies")
                .DependsOn(Clean, DnxRestore, BuildInfo, AstGen)
                .Do(exec =>
                    {
                        exec.Run("cmd.exe")
                            .With($"/c dnu build --configuration Release --out {Projects.DriverProject.OutputDirectory}")
                            .In(Projects.DriverProject.Folder.ToString());
                    })

                //Define
                .Task(DnxRestore).Desc("Restores .NET Core dependencies")
                .Do(() =>
                    {
                        bau.CurrentTask.LogInfo("DNVM INSTALL");
                        //DNVM INSTALL
                        Task.Run.Executable(e =>
                            {
                                e.ExecutablePath("cmd.exe")
                                    .WithArguments($"/c dnvm install {Projects.DnmvVersion} -r clr")
                                    .InWorkingDirectory(Projects.DriverProject.Folder);
                            });

                        bau.CurrentTask.LogInfo("DNVM USE");
                        //USE
                        Task.Run.Executable(e =>
                            {
                                e.ExecutablePath("cmd.exe")
                                    .WithArguments($"/c dnvm use {Projects.DnmvVersion} -r clr")
                                    .InWorkingDirectory(Projects.DriverProject.Folder);
                            });

                        bau.CurrentTask.LogInfo("DNU RESTORE");
                        //DNU RESTORE
                        Task.Run.Executable(e =>
                            {
                                e.ExecutablePath("cmd.exe")
                                    .WithArguments("/c dnu restore")
                                    .InWorkingDirectory(Projects.DriverProject.Folder);
                            });
                    })

                //Define
                .Task(BuildInfo).Desc("Creates dynamic AssemblyInfos for projects")
                .Do(() =>
                    {
                        bau.CurrentTask.LogInfo("Injecting AssemblyInfo.cs");
                        Task.CreateAssemblyInfo.Language.CSharp(aid =>
                            {
                                Projects.DriverProject.AssemblyInfo(aid);
                                var outputPath = Projects.DriverProject.Folder.SubFolder("Properties").File("AssemblyInfo.cs");
                                Console.WriteLine($"Creating AssemblyInfo file: {outputPath}");
                                aid.OutputPath(outputPath);
                            });

                        bau.CurrentTask.LogInfo("Injecting DNX project.json with Nuspec");
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
                    });

            bau.Run();
        }

        private static FileInfo FindNugetExe()
        {
            Directory.SetCurrentDirectory(Folders.Lib.ToString());

            var nugetExe = NuGetFileFinder.FindFile();
            nugetExe.Should().NotBeNull();
            Console.WriteLine("FOUND NUGET HERE: " + nugetExe);

            Directory.SetCurrentDirectory(Folders.WorkingFolder.ToString());

            return nugetExe;
        }
    }
}