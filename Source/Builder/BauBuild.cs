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
        public const string MsBuild = "msbuild";
        public const string DnxBuild = "dnxbuild";
        public const string Clean = "clean";
        public const string Restore = "restore";
        public const string DnxRestore = "dnxrestore";
        public const string BuildInfo = "buildinfo";
        public const string CodeGen = "codegen";
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

            new Bau(Arguments.Parse(args))
                //By default, no build arguments...
                .DependsOn(Clean, Restore, MsBuild)

                //Define
                .MSBuild(MsBuild).Desc("Invokes MSBuild to build solution")
                .DependsOn(Clean, BuildInfo, CodeGen)
                .Do(msb =>
                    {
                        msb.ToolsVersion = "14.0";
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
                .DependsOn(DnxRestore)
                .Do(exec =>
                    {
                        exec.Run("cmd.exe")
                            .With("/c dnu build --configuration Release")
                            .In(Projects.DriverProject.Folder.ToString());
                    })

                //Define
                .Task(DnxRestore).Desc("Restores .NET Core dependencies")
                .Do(() =>
                    {
                        //DNVM INSTALL
                        Task.Run.Executable(e =>
                            {
                                e.ExecutablePath("cmd.exe")
                                    .WithArguments($"/c dnvm install {Projects.DnmvVersion} -r clr")
                                    .InWorkingDirectory(Projects.DriverProject.Folder);
                            });
                        //USE
                        Task.Run.Executable(e =>
                        {
                            e.ExecutablePath("cmd.exe")
                                .WithArguments($"/c dnvm use {Projects.DnmvVersion} -r clr")
                                .InWorkingDirectory(Projects.DriverProject.Folder);
                        });
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
                        Task.CreateAssemblyInfo.Language.CSharp(aid =>
                            {
                                Projects.DriverProject.AssemblyInfo(aid);
                                var outputPath = Projects.DriverProject.Folder.SubFolder("Properties").File("AssemblyInfo.cs");
                                Console.WriteLine($"Creating AssemblyInfo file: {outputPath}");
                                aid.OutputPath(outputPath);
                            });
                    })

                //Define
                .Task(CodeGen).Desc("Regenerates C# AST classes")
                .Do(() =>
                    {
                        Directory.SetCurrentDirectory(Projects.DriverProject.Folder.ToString());
                        MetaDb.Initialize(Projects.TemplatesProject.Metadata.ToString());

                        var gen = new Templates.GeneratorForAst();
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
                .DependsOn(MsBuild).Do(ng =>
                    {
                        ng.Pack(Projects.DriverProject.NugetSpec.ToString(),
                            p =>
                                {
                                    p.BasePath = Folders.CompileOutput.ToString();
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

                .Run();
        }

        private static FileInfo FindNugetExe()
        {
            Directory.SetCurrentDirectory(Folders.Lib.ToString());
            var nugetExe = NuGetFileFinder.FindFile();
            nugetExe.Should().NotBeNull();
            Directory.SetCurrentDirectory(Folders.WorkingFolder.ToString());
            return nugetExe;
        }
    }
}