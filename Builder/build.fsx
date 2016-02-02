//#if INTERACTIVE
//open System
//let workingDir = "C:/Code/Projects/Public/RethinkDb.Driver"
//Environment.CurrentDirectory <- workingDir
//#else
//#endif
open System
let workingDir = Environment.CurrentDirectory

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

Target "nuget" (fun _ ->
    trace "NuGet Task"
    
    let driverConfig = NuGetConfig DriverProject Folders Files     
    NuGet ( fun p -> driverConfig) DriverProject.NugetSpec

    let gridConfig = NuGetConfig GridProject Folders Files     
    NuGet ( fun p -> gridConfig) GridProject.NugetSpec
)

Target "zip" (fun _ -> 
    trace "Zip Task"

    !!(DriverProject.OutputDirectory @@ "**") |> Zip Folders.CompileOutput (Folders.Package @@ DriverProject.Zip)
    !!(GridProject.OutputDirectory @@ "**") |> Zip Folders.CompileOutput (Folders.Package @@ GridProject.Zip)
)


Target "BuildInfo" (fun _ ->
    
    trace "Writing Assembly Build Info"

    MakeBuildInfo DriverProject Folders
    MakeBuildInfo GridProject Folders

)


Target "Clean" (fun _ ->
    CleanDirs [Folders.CompileOutput; Folders.Package]
)

Target "test" (fun _ ->
    trace "CI BUILT"
)

Target "ci" (fun _ ->
    trace "CI BUILT"
)



"Clean"
    ==> "astgen"
    ==> "BuildInfo"

//build systems
"BuildInfo"
    ==> "dnx"
    ==> "zip"

"BuildInfo"
    ==> "msb"
    ==> "zip"

"BuildInfo"
    ==> "mono"
    ==> "zip"

"dnx"
    ==> "nuget"

//none are dependent on each other, any of them requires dependencies of dnx
"dnx" <=> "msb" <=> "mono"


"nuget"
    ==> "ci"

"zip"
    ==> "ci"


// start build
RunTargetOrDefault "zip"