#if INTERACTIVE
open System
let workingDir = "C:/Code/Projects/Public/RethinkDb.Driver"
//Environment.CurrentDirectory <- workingDir
#else
let workingDir = ".."
#endif

// include Fake lib
#I @"packages/build/FAKE/tools"
#r @"FakeLib.dll"

#load @"Utils.fsx"

open Fake
open Utils
open System.Reflection
open Helpers

let ProjectName = "RethinkDb.Driver";

let Folders = Setup.Folders(workingDir)
let Files = Setup.Files(Folders)
let Projects = Setup.Projects(ProjectName, Folders)

let DriverProject = NugetProject("RethinkDb.Driver", "RethinkDb Driver for .NET", Folders)
let GridProject = NugetProject("RethinkDb.Driver.ReGrid", "RethinkDb Large Object Storage for .NET", Folders)

type TestProject = {Project : Project; Zip : string}
let TestProject = {Project = Project("Templates", Folders); Zip = "RethinkDb.Driver.Tests.zip"}


// Default target
Target "astgen" (fun _ ->
    
    trace "ReQL AST Generation Task Starting ..."
    
    let metadata = "Metadata";
    let templates = Project("Templates", Folders);
    
    !! templates.ProjectFile
    |> MSBuildDebug null "Build"
    |> Log "AppBuild-Output: "

    let path = templates.Folder @@ "bin" @@ "Debug" @@ sprintf "%s.dll" templates.Name

    let assembly = Assembly.LoadFrom(path)

    let gen = assembly.CreateInstance("Templates.GeneratorForAst")

    trace DriverProject.Folder

    DynInvoke gen "SetPaths" [| DriverProject.Folder |]
    DynInvoke gen "EnsurePathsExist" [||]
    DynInvoke gen "Generate_All" [||]
    
)

Target "msb" (fun _ ->
    
    let tag = "msb_build";

    !! DriverProject.ProjectFile
    |> MSBuildRelease (DriverProject.OutputDirectory @@ tag) "Build"
    |> Log "AppBuild-Output: "

    !! GridProject.ProjectFile
    |> MSBuildRelease (GridProject.OutputDirectory @@ tag) "Build"
    |> Log "AppBuild-Output: "

)



Target "dnx" (fun _ ->
    trace "DNX Build Task"

    let tag = "dnx_build"
    
    DnvmUpdate()
    DnvmInstall Projects.DnvmVersion
    DnvmUse Projects.DnvmVersion
    
    // PROJECTS
    Dnu DnuCommands.Restore DriverProject.Folder
    DnuBuild DriverProject.Folder (DriverProject.OutputDirectory @@ tag)

    Dnu DnuCommands.Restore GridProject.Folder
    DnuBuild GridProject.Folder (GridProject.OutputDirectory @@ tag)
)

Target "mono" (fun _ ->
     trace "Mono Task"

     let tag = "mono_build/"

     //Setup
     XBuild DriverProject.ProjectFile (DriverProject.OutputDirectory @@ tag)
     XBuild GridProject.ProjectFile (GridProject.OutputDirectory @@ tag)
)

Target "pack" (fun _ ->
    trace "Pack Task"
    
    let driverConfig = NuGetConfig DriverProject Folders Files     
    NuGet ( fun p -> driverConfig) DriverProject.NugetSpec

    let gridConfig = NuGetConfig GridProject Folders Files     
    NuGet ( fun p -> gridConfig) GridProject.NugetSpec
     
)

Target "push" (fun _ ->
     trace "Push Task"
)



Target "BuildInfo" (fun _ ->
    
    trace "Writing Assembly Build Info"

    MakeBuildInfo DriverProject Folders
    MakeBuildInfo GridProject Folders

)


Target "Clean" (fun _ ->
    CleanDirs [Folders.CompileOutput; Folders.Package]
)



//"Clean" 
//    ==> "astgen"
//    ==> "BuildInfo"
//    ==> "dnx"
//    ==> "pack"


// start build
RunTargetOrDefault "pack"