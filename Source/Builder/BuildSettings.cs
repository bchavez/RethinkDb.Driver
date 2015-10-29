using System;
using Builder.Extensions;
using FluentBuild;
using FluentBuild.AssemblyInfoBuilding;
using FluentFs.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Z.ExtensionMethods;

namespace Builder
{
    public class Folders
    {
        public static readonly Directory WorkingFolder = new Directory( Properties.CurrentDirectory );
        public static readonly Directory CompileOutput = WorkingFolder.SubFolder( "__compile" );
        public static readonly Directory Package = WorkingFolder.SubFolder( "__package" );
        public static readonly Directory Source = WorkingFolder.SubFolder( "Source" );
        public static readonly Directory Builder = Source.SubFolder("Builder");

        public static readonly Directory Lib = Source.SubFolder( "packages" );
    }

    public class BuildContext
    {
        public static readonly string FullVersion = VersionGetter.GetVersion();
        public static readonly string Version = FullVersion.WithoutPreReleaseName();
    }

    public class Projects
    {
        private static void GlobalAssemblyInfo(IAssemblyInfoDetails aid)
        {
            aid.Company( "Brian Chavez" )
               .Copyright( "Brian Chavez © " + DateTime.UtcNow.Year )
               .Version( BuildContext.Version )
               .FileVersion( BuildContext.Version )
               .InformationalVersion( $"{BuildContext.FullVersion} built on {DateTime.UtcNow} UTC" )
               .Trademark("Apache License v2.0")
               .Description( "http://www.github.com/bchavez/RethinkDb.Driver" )
               .ComVisible(false);
        }

        public static readonly File SolutionFile = Folders.Source.File( "RethinkDb.Driver.sln" );
        public static readonly File GlobalJson = Folders.Source.File("global.json");
        public static string DnmvVersion = ReadJson.From(GlobalJson.ToString(), "sdk.version");

        public class DriverProject
        {
            public const string Name = "RethinkDb.Driver";
            public static readonly Directory Folder = Folders.Source.SubFolder( Name );
            public static readonly File ProjectFile = Folder.File( $"{Name}.csproj" );
            public static readonly File DnxProjectFile = Folder.File("project.json");
            public static readonly Directory OutputDirectory = Folders.CompileOutput.SubFolder(Name);
            public static readonly File OutputDll = OutputDirectory.File( $"{Name}.dll" );
            public static readonly Directory PackageDir = Folders.Package.SubFolder( Name );
            
            public static readonly File NugetSpec = Folders.Builder.SubFolder("NuGet").File( $"{Name}.nuspec" );
            public static readonly File NugetNupkg = Folders.Package.File($"{Name}.{BuildContext.FullVersion}.nupkg");

            public static readonly Action<IAssemblyInfoDetails> AssemblyInfo =
                i =>
                    {
                        i.Title("RethinkDb Driver for .NET")
                            .Product(Name);

                        GlobalAssemblyInfo(i);
                    };
        }

        public class TemplatesProject
        {
            public const string Name = "Templates";
            public static readonly Directory Folder = Folders.Source.SubFolder(Name);
            public static readonly File ProjectFile = Folder.File($"{Name}.csproj");
            public static readonly Directory Metadata = Folder.SubFolder("Metadata");
        }

        public class Tests
        {
            public static readonly Directory Folder = Folders.Source.SubFolder( "RethinkDb.Driver.Tests" );
        }
    }
}
