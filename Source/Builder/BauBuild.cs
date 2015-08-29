using System;
using System.IO;
using BauCore;
using BauMSBuild;
using BauNuGet;
using Builder.Tasks;
using FluentAssertions;

namespace Builder
{
    public static class BauBuild
    {
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

            var buildTasks = new BuildTasks();

            new Bau(Arguments.Parse(args))
                .DependsOn("clean", "restore", "build")
                .MSBuild("build").DependsOn("meta")
                .Do(msb =>
                    {
                        msb.ToolsVersion = "14.0";
                        //msb.MSBuildVersion = "net45";
                        msb.Solution = Projects.SolutionFile.ToString();
                        msb.Properties = new
                            {
                                Configuration = "Release",
                                OutDir = Folders.CompileOutput
                            };
                        msb.Targets = new[] {"Rebuild"};
                    })
                .Task("meta").Do(() =>
                    {
                        buildTasks.Meta();
                    })
                .Task("clean").Do(() =>
                    {
                        buildTasks.Clean();
                    })
                .NuGet("pack").DependsOn("build")
                .Do(ng =>
                    {
                        ng.Pack(Projects.DriverProject.NugetSpec.ToString(),
                            p =>
                                {
                                    p.BasePath = Folders.CompileOutput.ToString();
                                    p.Version = BuildContext.Version;
                                    p.Symbols = true;
                                    p.OutputDirectory = Folders.Package.ToString();
                                })
                            .WithNuGetExePathOverride(nugetExe.FullName);
                    })
                .NuGet("push").DependsOn("pack")
                .Do(ng =>
                    {
                        ng.Push(Projects.DriverProject.NugetNupkg.ToString())
                            .WithNuGetExePathOverride(nugetExe.FullName);
                    })
                .NuGet("restore")
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