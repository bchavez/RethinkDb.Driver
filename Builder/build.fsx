//#if INTERACTIVE
//open System
//Environment.CurrentDirectory <- workingDir
//#else
//#endif


// include Fake lib
#I @"packages/build/FAKE/tools"
#r @"FakeLib.dll"

#load @"Utils.fsx"

open Fake
open Utils
open System.Reflection
open Helpers

//let workingDir = ChangeWorkingFolder()
let workingDir = "C:/Code/Projects/Public/RethinkDb.Driver"


trace (sprintf "WORKING DIR: %s" workingDir)

let ProjectName = "RethinkDb.Driver";

let Folders = Setup.Folders(workingDir)
let Files = Setup.Files(Folders)
let Projects = Setup.Projects(ProjectName, Folders)

let DriverProject = NugetProject("RethinkDb.Driver", "RethinkDb Driver for .NET", Folders)
let GridProject = NugetProject("RethinkDb.Driver.ReGrid", "RethinkDb Large Object Storage for .NET", Folders)
let DriverTestProject = TestProject("RethinkDb.Driver.Tests", Folders)
//let TestGridProject = Project("RethinkDb.Driver.ReGrid.Tests", Folders)


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

    !! DriverTestProject.ProjectFile
    |> MSBuildDebug "" "Build"
    |> Log "AppBuild-Output: "

//    !! GridTestProject.ProjectFile
//    |> MSBuildDebug "" "Build"
//    |> Log "AppBuild-Output: "
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

Target "restore" (fun _ -> 
     trace "MS NuGet Project Restore"
     Projects.SolutionFile
     |> RestoreMSSolutionPackages (fun p ->
            { p with OutputPath = (Folders.Source @@ "packages" )}
        )
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
    if( System.IO.File.Exists(Files.TestResultFile)) then
        System.IO.File.Delete(Files.TestResultFile)
    
    CleanDirs [Folders.CompileOutput; Folders.Package]
)


Target "serverup" (fun _ ->

    let url = "https://ipfs.io/ipfs/QmeaNpgJd2Uwharad1o1dcDx9K1m3i93vx7BUkiEcCJqi5/RethinkDB-windows-alpha2.zip"

    use client = new System.Net.WebClient()
    let zipfile = (Folders.Test @@ "RethinkDb.Server.zip")
    let serverExe = (Folders.Test @@ "RethinkDB-windows-alpha2.exe")
    let serverArgs = ""

    client.DownloadFile(url, zipfile)

    Unzip Folders.Test zipfile

    trace "STARTING RETHINKDB SERVER ON WINDOWS ;) ATnNn STYLE"
    fireAndForget( fun psi -> 
                         psi.FileName <- serverExe
                         psi.WorkingDirectory <- Folders.Test
                         psi.Arguments <- serverArgs )
    trace "STARTED RETHINKDB SERVER ON WINDOWS ;)"

)

Target "test" (fun _ ->
    trace "CI TEST"

    let nunit = findToolInSubPath "nunit-console.exe" Folders.Lib
    let nunitFolder = System.IO.Path.GetDirectoryName(nunit)

    !! DriverTestProject.TestAssembly
    |> NUnit (fun p -> { p with 
                            ToolPath = nunitFolder
                            OutputFile = Files.TestResultFile })
)

open Fake.AppVeyor

Target "ci" (fun _ ->
    trace "ci Task"
)

Target "citest" (fun _ ->
    Run "test"
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

"zip"
    ==> "ci"

//test task depends on msbuild
"msb"
    ==> "test"


"serverup"
    ==> "citest"



// start build
RunTargetOrDefault "zip"