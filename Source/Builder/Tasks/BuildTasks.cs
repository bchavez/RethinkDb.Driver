using System;
using System.Collections.Generic;
using System.Diagnostics;
using Builder.Extensions;
using Fluent.IO;
using FluentBuild;
using NUnit.Framework;

namespace Builder.Tasks
{
    [TestFixture]
    public class BuildTasks
    {
        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {
            Defaults.Logger.Verbosity = VerbosityLevel.Full;

            System.IO.Directory.SetCurrentDirectory( @"..\..\..\.." );
        }

        [Test]
        [Explicit]
        public void Clean()
        {
            Console.WriteLine($"Removing {Folders.CompileOutput}");
            Folders.CompileOutput.Wipe();
            Console.WriteLine($"Removing {Folders.Package}");
            Folders.Package.Wipe();
        }

        [Test]
        [Explicit]
        public void Meta()
        {
            Task.CreateAssemblyInfo.Language.CSharp(aid =>
            {
                Projects.DriverProject.AssemblyInfo(aid);
                var outputPath = Projects.DriverProject.Folder.SubFolder("Properties").File("AssemblyInfo.cs");
                Console.WriteLine($"Creating AssemblyInfo file: {outputPath}");
                aid.OutputPath(outputPath);
            });
        }

        [Test]
        [Explicit]
        public void Package()
        {
            Defaults.Logger.WriteHeader("PACKAGE");
            //copy compile directory to package directory
            Fluent.IO.Path.Get(Projects.DriverProject.OutputDirectory.ToString())
                .Copy(Projects.DriverProject.PackageDir.ToString(), Overwrite.Always, true);

            var version = BuildContext.Version;

            Defaults.Logger.Write("RESULTS", "NuGet packing");

            Fluent.IO.Path nuget = Fluent.IO.Path.Get(Folders.Lib.ToString())
                .Files("NuGet.exe", true).First();

            Task.Run.Executable(e => e.ExecutablePath(nuget.FullPath)
                .WithArguments("pack", Projects.DriverProject.NugetSpec.Path, "-Version", version, "-OutputDirectory",
                    Folders.Package.ToString()));

            Defaults.Logger.Write("RESULTS", "Setting NuGet PUSH script");

            //Defaults.Logger.Write( "RESULTS", pushcmd );
            System.IO.File.WriteAllText("nuget.push.bat",
                "{0} push {1}".With(nuget.MakeRelative().ToString(),
                    Path.Get(Projects.DriverProject.NugetNupkg.ToString()).MakeRelative().ToString()) +
                Environment.NewLine);
        }

    }
}