﻿//#if INTERACTIVE
//open System
//Environment.CurrentDirectory <- workingDir
//#else
//#endif

let serverDownload = "https://download.rethinkdb.com/windows/rethinkdb-2.3.5.zip"


// include Fake lib
#I @"packages/build/FAKE/tools"
#I @"packages/build/DotNetZip/lib/net20"
#r @"FakeLib.dll"
#r @"DotNetZip.dll"


#load @"Utils.fsx"

open Fake
open Utils
open System.Reflection
open Helpers
open Fake.Testing.NUnit3

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
let TestGridProject = TestProject("RethinkDb.Driver.ReGrid.Tests", Folders)


open AssemblyInfoFile

let MakeAttributes (includeSnk:bool) (testProjects : string list ) =
    let attrs = [
                    Attribute.Description GitHubUrl
                ]

    let mapInternalName (projectName : string) = 
        if includeSnk then
                let pubKey = ReadFileAsHexString Projects.SnkFilePublic
                let visibleTo = sprintf "%s, PublicKey=%s" projectName pubKey
                Attribute.InternalsVisibleTo(visibleTo)
            else
                Attribute.InternalsVisibleTo(projectName)

    testProjects
        |> List.map( mapInternalName )
        |> List.append attrs



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

Target "testgen" (fun _ ->
    
    trace "ReQL YAML Unit Test Generation Task Starting ..."
    
    let metadata = "Metadata";
    let templates = Project("Templates", Folders);
    
    !! templates.ProjectFile
    |> MSBuildDebug null "Build"
    |> Log "AppBuild-Output: "

    let path = templates.Folder @@ "bin" @@ "Debug" @@ sprintf "%s.dll" templates.Name

    let assembly = Assembly.LoadFrom(path)

    let gen = assembly.CreateInstance("Templates.GeneratorForUnitTests")

    trace DriverProject.Folder

    DynInvoke gen "BeforeRunningTestSession" [||]
    DynInvoke gen "Generate_All" [||]
    
)
Target "msb" (fun _ ->
    
    let tag = "msb_build";

    let buildProps = [ 
                        "AssemblyOriginatorKeyFile", Projects.SnkFile
                        "SignAssembly", BuildContext.IsTaggedBuild.ToString()
                     ]

    !! DriverProject.ProjectFile
    |> MSBuildReleaseExt (DriverProject.OutputDirectory @@ tag) buildProps "Build"
    |> Log "AppBuild-Output: "

    !! LinqProject.ProjectFile
    |> MSBuildReleaseExt (LinqProject.OutputDirectory @@ tag) buildProps "Build"
    |> Log "AppBuild-Output: "

    !! GridProject.ProjectFile
    |> MSBuildReleaseExt (GridProject.OutputDirectory @@ tag) buildProps "Build"
    |> Log "AppBuild-Output: "

    !! TestDriverProject.ProjectFile
    |> MSBuild "" "Build" (("Configuration", "Debug")::buildProps)
    |> Log "AppBuild-Output: "

    !! TestLinqProject.ProjectFile
    |> MSBuild "" "Build" (("Configuration", "Debug")::buildProps)
    |> Log "AppBuild-Output: "

    !! TestGridProject.ProjectFile
    |> MSBuild "" "Build" (("Configuration", "Debug")::buildProps)
    |> Log "AppBuild-Output: "
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

     let buildProps = [ 
                        "AssemblyOriginatorKeyFile", Projects.SnkFile
                        "SignAssembly", BuildContext.IsTaggedBuild.ToString()
                      ]

     //Setup
     XBuild DriverProject.ProjectFile (DriverProject.OutputDirectory @@ tag) buildProps
     XBuild LinqProject.ProjectFile (LinqProject.OutputDirectory @@ tag) buildProps
     XBuild GridProject.ProjectFile (GridProject.OutputDirectory @@ tag) buildProps
)

Target "restore" (fun _ -> 
     trace "MS NuGet Project Restore"
     Projects.SolutionFile
     |> RestoreMSSolutionPackages (fun p ->
            { p with OutputPath = (Folders.Source @@ "packages" )}
        )
 )

open Ionic.Zip
open System.Xml

Target "nuget" (fun _ ->
    trace "NuGet Task"

    DotnetPack DriverProject Folders.Package
    DotnetPack LinqProject Folders.Package
    DotnetPack GridProject Folders.Package

    traceHeader "Injecting Version Ranges"

    let files = [
                    LinqProject.NugetPkg, LinqProject.NugetSpec
                    LinqProject.NugetPkgSymbols, LinqProject.NugetSpec

                    GridProject.NugetPkg, GridProject.NugetSpec
                    GridProject.NugetPkgSymbols, GridProject.NugetSpec
                ]

    let exactNugetVersion = [
                                 "RethinkDb.Driver"
                            ]
  
    let extractNugetPackage (pkg : string) (extractPath : string) = 
        use zip = new ZipFile(pkg)
        zip.ExtractAll( extractPath )
  
    let repackNugetPackage (folderPath : string) (pkg : string) =
        use zip = new ZipFile()
        zip.AddDirectory(folderPath) |> ignore
        zip.Save(pkg)
  
    for (pkg, spec) in files do 
        tracefn "FILE: %s" pkg
  
        let extractPath = Folders.Package @@ fileNameWithoutExt pkg
  
        extractNugetPackage pkg extractPath
        DeleteFile pkg
  
        let nuspecFile = extractPath @@ spec
  
        let xmlns = [("def", "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd")]
  
        let doc = new XmlDocument()
        doc.Load nuspecFile
  
        for exact in exactNugetVersion do
            let target = sprintf "//def:dependency[@id='%s']" exact
            let nodes = XPathSelectAllNSDoc doc xmlns target
            for node in nodes do
                let version = getAttribute "version" node
                node.Attributes.["version"].Value <- sprintf "[%s]" version
        
        doc.Save nuspecFile
    
        repackNugetPackage extractPath pkg
        DeleteDir extractPath
    
    
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
        -- (Folders.CompileOutput @@ "**" @@ "*.deps.json")
        |> Zip Folders.CompileOutput (Folders.Package @@ DriverProject.Zip)
)


Target "BuildInfo" (fun _ ->
    
    trace "Writing Assembly Build Info"

    MakeBuildInfo DriverProject Folders (fun bip ->
        { bip with ExtraAttrs = MakeAttributes BuildContext.IsTaggedBuild [LinqProject.Name; TestDriverProject.Name; TestLinqProject.Name] } )
    MakeBuildInfo LinqProject Folders (fun bip ->
        { bip with ExtraAttrs = MakeAttributes BuildContext.IsTaggedBuild [TestLinqProject.Name] } )
    MakeBuildInfo GridProject Folders (fun bip ->
        { bip with ExtraAttrs = MakeAttributes BuildContext.IsTaggedBuild [TestGridProject.Name] } )

    XmlPokeInnerText DriverProject.ProjectFile "/Project/PropertyGroup/Version" BuildContext.FullVersion
    //JsonPoke "version" BuildContext.FullVersion DriverProject.ProjectJson
    XmlPokeInnerText LinqProject.ProjectFile "/Project/PropertyGroup/Version" BuildContext.FullVersion
    //JsonPoke "version" BuildContext.FullVersion LinqProject.ProjectJson
    XmlPokeInnerText GridProject.ProjectFile "/Project/PropertyGroup/Version" BuildContext.FullVersion
    //JsonPoke "version" BuildContext.FullVersion GridProject.ProjectJson
    
    let releaseNotes = History.NugetText Files.History GitHubUrl
    //JsonPoke "packOptions.releaseNotes" releaseNotes DriverProject.ProjectJson
    XmlPokeInnerText DriverProject.ProjectFile "/Project/PropertyGroup/PackageReleaseNotes" releaseNotes
    //JsonPoke "packOptions.releaseNotes" releaseNotes LinqProject.ProjectJson
    XmlPokeInnerText LinqProject.ProjectFile "/Project/PropertyGroup/PackageReleaseNotes" releaseNotes
    //JsonPoke "packOptions.releaseNotes" releaseNotes GridProject.ProjectJson
    XmlPokeInnerText GridProject.ProjectFile "/Project/PropertyGroup/PackageReleaseNotes" releaseNotes

    //let version = sprintf "[%s]" BuildContext.FullVersion
    //SetDependency DriverProject.Name version GridProject.ProjectJson
    //SetDependency DriverProject.Name version LinqProject.ProjectJson
)


Target "Clean" (fun _ ->
    DeleteFile Files.TestResultFile
    CleanDirs [Folders.CompileOutput; Folders.Package]

    //JsonPoke "version" "0.0.0-localbuild" DriverProject.ProjectJson
    XmlPokeInnerText DriverProject.ProjectFile "/Project/PropertyGroup/Version" "0.0.0-localbuild"
    //JsonPoke "version" "0.0.0-localbuild" LinqProject.ProjectJson
    XmlPokeInnerText LinqProject.ProjectFile "/Project/PropertyGroup/Version" "0.0.0-localbuild"
    //JsonPoke "version" "0.0.0-localbuild" GridProject.ProjectJson
    XmlPokeInnerText GridProject.ProjectFile "/Project/PropertyGroup/Version" "0.0.0-localbuild"
    
    //reset project deps.
    //JsonPoke "packOptions.releaseNotes" "" DriverProject.ProjectJson
    XmlPokeInnerText DriverProject.ProjectFile "/Project/PropertyGroup/PackageReleaseNotes" ""
    //JsonPoke "packOptions.releaseNotes" "" LinqProject.ProjectJson
    XmlPokeInnerText LinqProject.ProjectFile "/Project/PropertyGroup/PackageReleaseNotes" ""
    //JsonPoke "packOptions.releaseNotes" "" GridProject.ProjectJson
    XmlPokeInnerText GridProject.ProjectFile "/Project/PropertyGroup/PackageReleaseNotes" ""

    //JsonPoke "buildOptions.keyFile" "" DriverProject.ProjectJson
    XmlPokeInnerText DriverProject.ProjectFile "/Project/PropertyGroup/AssemblyOriginatorKeyFile" ""
    XmlPokeInnerText DriverProject.ProjectFile "/Project/PropertyGroup/SignAssembly" "false"

    //JsonPoke "buildOptions.keyFile" "" LinqProject.ProjectJson
    XmlPokeInnerText LinqProject.ProjectFile "/Project/PropertyGroup/AssemblyOriginatorKeyFile" ""
    XmlPokeInnerText LinqProject.ProjectFile "/Project/PropertyGroup/SignAssembly" "false"

    //JsonPoke "buildOptions.keyFile" "" GridProject.ProjectJson
    XmlPokeInnerText GridProject.ProjectFile "/Project/PropertyGroup/AssemblyOriginatorKeyFile" ""
    XmlPokeInnerText GridProject.ProjectFile "/Project/PropertyGroup/SignAssembly" "false"

    //SetDependency DriverProject.Name "*" GridProject.ProjectJson
    //SetDependency DriverProject.Name "*" LinqProject.ProjectJson

    let defaultBuildDate = System.DateTime.Parse("1/1/2015");
    MakeBuildInfo DriverProject Folders (fun bip ->
        { bip with 
            DateTime = defaultBuildDate
            ExtraAttrs = MakeAttributes false [LinqProject.Name; TestDriverProject.Name; TestLinqProject.Name] } )
    MakeBuildInfo LinqProject Folders (fun bip ->
        { bip with 
            DateTime = defaultBuildDate
            ExtraAttrs = MakeAttributes false [TestLinqProject.Name] } )
    MakeBuildInfo GridProject Folders (fun bip ->
        { bip with 
            DateTime = defaultBuildDate
            ExtraAttrs = MakeAttributes false [TestGridProject.Name] } )
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
    let nunit = findToolInSubPath "nunit3-console.exe" Folders.Lib
    let nunitFolder = System.IO.Path.GetDirectoryName(nunit)

    !! TestDriverProject.TestAssembly
    ++ TestLinqProject.TestAssembly
    ++ TestGridProject.TestAssembly
    |> NUnit3 (fun p -> { p with 
                            ProcessModel = NUnit3ProcessModel.SingleProcessModel
                            ToolPath = nunit
                            ResultSpecs = [Files.TestResultFile]
                            ErrorLevel = TestRunnerErrorLevel.Error })

open Fake.AppVeyor

Target "ci" (fun _ ->
    trace "ci Task"
)

Target "test" (fun _ ->
    trace "TEST"
    RunTests()
)

Target "citest" (fun _ ->
    RunTests()
    UploadTestResultsXml TestResultsType.NUnit3 Folders.Test
)

Target "setup-snk"(fun _ ->
    trace "Decrypting Strong Name Key (SNK) file."
    let decryptSecret = environVarOrFail "SNKFILE_SECRET"
    decryptFile Projects.SnkFile decryptSecret

    //JsonPoke "buildOptions.keyFile" Projects.SnkFile DriverProject.ProjectJson
    XmlPokeInnerText DriverProject.ProjectFile "/Project/PropertyGroup/AssemblyOriginatorKeyFile" Projects.SnkFile
    XmlPokeInnerText DriverProject.ProjectFile "/Project/PropertyGroup/SignAssembly" "true"

    //JsonPoke "buildOptions.keyFile" Projects.SnkFile LinqProject.ProjectJson
    XmlPokeInnerText LinqProject.ProjectFile "/Project/PropertyGroup/AssemblyOriginatorKeyFile" Projects.SnkFile
    XmlPokeInnerText LinqProject.ProjectFile "/Project/PropertyGroup/SignAssembly" "true"

    //JsonPoke "buildOptions.keyFile" Projects.SnkFile GridProject.ProjectJson
    XmlPokeInnerText GridProject.ProjectFile "/Project/PropertyGroup/AssemblyOriginatorKeyFile" Projects.SnkFile
    XmlPokeInnerText GridProject.ProjectFile "/Project/PropertyGroup/SignAssembly" "true"
)


"Clean"
    ==> "restore"
    ==> "astgen"
    ==> "BuildInfo"

//build systems
"BuildInfo"
    =?> ("setup-snk", BuildContext.IsTaggedBuild)
    ==> "dnx"
    ==> "zip"

"BuildInfo"
    =?> ("setup-snk", BuildContext.IsTaggedBuild)
    ==> "msb"
    ==> "zip"

"BuildInfo"
    =?> ("setup-snk", BuildContext.IsTaggedBuild)
    //==> "mono" - AppVeyor doesn't have mono on VS 2017 image
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
