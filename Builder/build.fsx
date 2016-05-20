//#if INTERACTIVE
//open System
//Environment.CurrentDirectory <- workingDir
//#else
//#endif

let serverDownload = "https://download.rethinkdb.com/windows/rethinkdb-2.3.2.zip"


// include Fake lib
#I @"packages/build/FAKE/tools"
#I @"packages/build/DotNetZip/lib/net20"
#r @"FakeLib.dll"
#r @"Ionic.Zip.dll"


#load @"Utils.fsx"

open Fake
open Utils
open System.Reflection
open Helpers

let workingDir = ChangeWorkingFolder()
//let workingDir = "C:/Code/Projects/Public/RethinkDb.Driver"


trace (sprintf "WORKING DIR: %s" workingDir)

let ProjectName = "RethinkDb.Driver";
let GitHubUrl = "https://github.com/bchavez/RethinkDb.Driver"

let Folders = Setup.Folders(workingDir)
let Files = Setup.Files(Folders)
let Projects = Setup.Projects(ProjectName, Folders)

let DriverProject = NugetProject("RethinkDb.Driver", "RethinkDb Driver for .NET", Folders)
let LinqProject = NugetProject("RethinkDb.Driver.Linq", "A LINQ to ReQL provider for the RethinkDb Driver", Folders)
let GridProject = NugetProject("RethinkDb.Driver.ReGrid", "RethinkDb Large Object Storage for .NET", Folders)
let TestDriverProject = TestProject("RethinkDb.Driver.Tests", Folders)
let TestLinqProject = TestProject("RethinkDb.Driver.Linq.Tests", Folders)
let TestGridProject = Project("RethinkDb.Driver.ReGrid.Tests", Folders)


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

    !! LinqProject.ProjectFile
    |> MSBuildRelease (LinqProject.OutputDirectory @@ tag) "Build"
    |> Log "AppBuild-Output: "

    !! GridProject.ProjectFile
    |> MSBuildRelease (GridProject.OutputDirectory @@ tag) "Build"
    |> Log "AppBuild-Output: "

    !! TestDriverProject.ProjectFile
    |> MSBuildDebug "" "Build"
    |> Log "AppBuild-Output: "

    !! TestLinqProject.ProjectFile
    |> MSBuildDebug "" "Build"
    |> Log "AppBuild-Output: "

//    !! GridTestProject.ProjectFile
//    |> MSBuildDebug "" "Build"
//    |> Log "AppBuild-Output: "
)



Target "dnx" (fun _ ->
    trace "DNX Build Task"

    let tag = "dnx_build"
    
    // PROJECTS
    Dotnet DotnetCommands.Restore DriverProject.Folder
    DotnetBuild DriverProject (DriverProject.OutputDirectory @@ tag)

    Dotnet DotnetCommands.Restore LinqProject.Folder
    DotnetBuild LinqProject (LinqProject.OutputDirectory @@ tag)

    Dotnet DotnetCommands.Restore GridProject.Folder
    DotnetBuild GridProject (GridProject.OutputDirectory @@ tag)
)

Target "mono" (fun _ ->
     trace "Mono Task"

     let tag = "mono_build/"

     //Setup
     XBuild DriverProject.ProjectFile (DriverProject.OutputDirectory @@ tag)
     XBuild LinqProject.ProjectFile (LinqProject.OutputDirectory @@ tag)
     XBuild GridProject.ProjectFile (GridProject.OutputDirectory @@ tag)
)

Target "restore" (fun _ -> 
     trace "MS NuGet Project Restore"
     Projects.SolutionFile
     |> RestoreMSSolutionPackages (fun p ->
            { p with OutputPath = (Folders.Source @@ "packages" )}
        )
 )

Target "nuget" (fun _ ->
    trace "NuGet Task"
    
    DotnetPack DriverProject Folders.Package
    DotnetPack LinqProject Folders.Package
    DotnetPack GridProject Folders.Package
)

Target "push" (fun _ ->
    trace "NuGet Push Task"
    
    failwith "Only CI server should publish on NuGet"
)



Target "zip" (fun _ -> 
    trace "Zip Task"

    !!(DriverProject.OutputDirectory @@ "**") 
        ++ (LinqProject.OutputDirectory @@ "**")
        ++ (GridProject.OutputDirectory @@ "**")
        |> Zip Folders.CompileOutput (Folders.Package @@ DriverProject.Zip)
)


Target "BuildInfo" (fun _ ->
    
    trace "Writing Assembly Build Info"

    MakeBuildInfo DriverProject Folders
    MakeBuildInfo LinqProject Folders
    MakeBuildInfo GridProject Folders

    JsonPoke "version" BuildContext.FullVersion DriverProject.ProjectJson
    JsonPoke "version" BuildContext.FullVersion LinqProject.ProjectJson
    JsonPoke "version" BuildContext.FullVersion GridProject.ProjectJson
    
    let releaseNotes = History.NugetText Files.History GitHubUrl
    JsonPoke "packOptions.releaseNotes" releaseNotes DriverProject.ProjectJson
    JsonPoke "packOptions.releaseNotes" releaseNotes LinqProject.ProjectJson
    JsonPoke "packOptions.releaseNotes" releaseNotes GridProject.ProjectJson

    let version = sprintf "[%s]" BuildContext.FullVersion
    SetDependency DriverProject.Name version GridProject.ProjectJson
    SetDependency DriverProject.Name version LinqProject.ProjectJson
)


Target "Clean" (fun _ ->
    DeleteFile Files.TestResultFile
    CleanDirs [Folders.CompileOutput; Folders.Package]

    JsonPoke "version" "0.0.0-localbuild" DriverProject.ProjectJson
    JsonPoke "version" "0.0.0-localbuild" LinqProject.ProjectJson
    JsonPoke "version" "0.0.0-localbuild" GridProject.ProjectJson
    
    //reset project deps.
    JsonPoke "packOptions.releaseNotes" "" DriverProject.ProjectJson
    JsonPoke "packOptions.releaseNotes" "" LinqProject.ProjectJson
    JsonPoke "packOptions.releaseNotes" "" GridProject.ProjectJson

    SetDependency DriverProject.Name "*" GridProject.ProjectJson
    SetDependency DriverProject.Name "*" LinqProject.ProjectJson
)

open Ionic.Zip

Target "serverup" (fun _ ->

    use client = new System.Net.WebClient()
    let zipfile = (Folders.Test @@ "RethinkDb.Server.zip")
    let serverExe = (Folders.Test @@ "rethinkdb.exe")
    let serverArgs = ""

    CreateDir (directory zipfile)
    
    trace ("Downloading RethinkDB for Windows ... : " + serverDownload)
    client.DownloadFile(serverDownload, zipfile)

    use zip = new ZipFile(zipfile)
    zip.FlattenFoldersOnExtract <- true;
    zip.ExtractAll(Folders.Test)


    trace "STARTING RETHINKDB SERVER ON WINDOWS ;)"
    fireAndForget( fun psi -> 
                         psi.FileName <- serverExe
                         psi.WorkingDirectory <- Folders.Test
                         psi.Arguments <- serverArgs )
    trace "STARTED RETHINKDB SERVER ON WINDOWS ;)"

)

let RunTests() =
    CreateDir Folders.Test
    let nunit = findToolInSubPath "nunit-console.exe" Folders.Lib
    let nunitFolder = System.IO.Path.GetDirectoryName(nunit)

    !! TestDriverProject.TestAssembly
    ++ TestLinqProject.TestAssembly
    |> NUnit (fun p -> { p with 
                            ToolPath = nunitFolder
                            OutputFile = Files.TestResultFile
                            ErrorLevel = TestRunnerErrorLevel.Error })

open Fake.AppVeyor

Target "ci" (fun _ ->
    trace "ci Task"
)

Target "test" (fun _ ->
    trace "CI TEST"
    RunTests()
)

Target "citest" (fun _ ->
    RunTests()
    UploadTestResultsXml TestResultsType.NUnit Folders.Test
)



"Clean"
    ==> "restore"
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


"nuget"
    ==> "ci"

"nuget"
    ==> "push"

"zip"
    ==> "ci"

//test task depends on msbuild
"msb"
    ==> "test"


"serverup"
    ==> "citest"


// start build
RunTargetOrDefault "msb"
