#if INTERACTIVE
let workingDir = "C:/Code/Projects/Public/RethinkDb.Driver"
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
    
    let metadata = "Metadata";
    let templates = Project("Templates", Folders);

    !! templates.ProjectFile
    |> MSBuildWithDefaults "Build"
    |> Log "AppBuild-Output: "

    let path = templates.Folder @@ "bin" @@ "Debug" @@ sprintf "%s.dll" templates.Name

    let assembly = Assembly.LoadFrom(path)

    let gen = assembly.CreateInstance("Templates.GeneratorForAst")


    DynInvoke gen "EnsurePathsExist" [||]
    DynInvoke gen "Generate_All" [||]
    
)

Target "msb" (fun _ ->
    
    !! DriverProject.ProjectFile
    |> MSBuildRelease DriverProject.OutputDirectory "Build"
    |> Log "AppBuild-Output: "

    !! GridProject.ProjectFile
    |> MSBuildRelease GridProject.OutputDirectory "Build"
    |> Log "AppBuild-Output: "

)

Target "dnx" (fun _ ->
     trace "dnx task"
)

Target "mono" (fun _ ->
     trace "mono task"
)




Target "BuildInfo" (fun _ ->
    //RethinkDB.Driver
    //makeBuildInfo "RethinkDb.Driver" "RethinkDb Driver for .NET" 
    //makeBuildInfo "RethinkDb.Driver.ReGrid" "RethinkDb Large Object File Storage for .NET"
    MakeBuildInfo GridProject Folders
)


Target "Clean" (fun _ ->
    CleanDirs [Folders.CompileOutput; Folders.Package]
)


"Clean"
    ==> "BuildInfo"
    ==> "msb"
    ==> "dnx"
    ==> "mono"


// start build
RunTargetOrDefault "msb"