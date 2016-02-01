module Utils

// include Fake lib
#I @"packages/build/FAKE/tools"
#I @"packages/build/FSharp.Data/lib/net40"

#r @"FakeLib.dll"
#r @"FSharp.Data.dll"

open Fake
open AssemblyInfoFile
open FSharp.Data
open FSharp.Data.JsonExtensions




module BuildContext =     

    let private WithoutPreReleaseName (ver : string) =
        let dash = ver.IndexOf("-")
        if dash > 0 then ver.Substring(0, dash) else ver.Trim()
    
    let private PreReleaseName (ver : string) =
        let dash = ver.IndexOf("-")
        if dash > 0 then Some(ver.Substring(dash + 1)) else None

    let FullVersion = 
        let forced = environVarOrNone "FORCE_VERSION"
        let tagname = environVarOrNone "APPVEYOR_REPO_TAG_NAME"
        let buildver = environVarOrNone "APPVEYOR_BUILD_VERSION"

        match (forced, tagname, buildver) with
        | (Some f, _, _) -> f
        | (_, Some t, _) -> t
        | (_, _, Some b) -> sprintf "0.0.%s-ci" b
        | (_, _, _     ) -> "0.0.0-localbuild"


    let Version = WithoutPreReleaseName FullVersion

        
//        let picks = [
//                        (forced, "forced")
//                        (tagname, "tag")
//                        (buildver, "ci")
//                        (Some "0.0.0-localbuild", "local")
//                    ]
//
//        picks |> List.pick (fun ele -> 
//                                match ele with
//                                | (Some value, "forced") -> value
//                                | (Some value, "tag") -> value
//                                | (Some value, "ci") -> sprintf "0.0.%s" value
//                                | (Some value, "local") -> value
//                                | (_, _) -> None )



module Setup =
    type Folders(workingFolder : string) =
        let compileOutput = workingFolder @@ "__compile"
        let package = workingFolder @@ "__package"
        let source = workingFolder @@ "Source"
        let lib = source @@ "packages"
        let builder = source @@ "builder"
    
        member this.WorkingFolder = workingFolder
        member this.CompileOutput = compileOutput
        member this.Package = package
        member this.Source = source
        member this.Lib = lib
        member this.Builder = builder

    type Files(folders : Folders) =
        let history = folders.WorkingFolder @@ "HISTORY.md"

        member this.History = history

    type Projects(projectName : string, folders : Folders) = 
        let solutionFile = folders.Source @@ sprintf "%s.sln" projectName
        let globalJson = folders.Source @@ "global.json"
        let dnvmVersion = 
            let json = JsonValue.Parse(System.IO.File.ReadAllText(globalJson))
            json?sdk?version.AsString()

        member this.SolutionFile = solutionFile
        member this.GlobalJson = globalJson
        member this.DnvmVersion = dnvmVersion

        //module Folders =
        //    let WorkingFolder = System.IO.Directory.GetCurrentDirectory()
        //    let CompileOutput = WorkingFolder @@ "__compile"
        //    let Package = WorkingFolder @@ "__package"
        //    let Source = WorkingFolder @@ "Source"
        //    let Lib = Source @@ "packages"
        //    let Builder = Source @@ "builder"
        //
        //module Files =
        //    let History = Folders.WorkingFolder @@ "HISTORY.md"
        //
        //
        //module Projects =
        //    let SolutionFile = Folders.Source @@ sprintf "%s.sln" ProjectName
        //    let GlobalJson = Folders.Source @@ "global.json"
        //    let DnvmVersion = 
        //        let json = JsonValue.Parse(System.IO.File.ReadAllText(GlobalJson))
        //        json?sdk?version.AsString()
        //
        //




open Setup


type Project(name : string, folders : Folders) =
    let folder = folders.Source @@ name
    let projectFile = folder @@ sprintf "%s.csproj" name
    member this.Folder = folder
    member this.ProjectFile = projectFile
    member this.Name = name

//type ProjectWithZip = {
//        Project : Project
//        Zip : string
//    }

type NugetProject(name : string, assemblyTitle : string, folders : Folders) =
    inherit Project(name, folders)
    
    let dnxProjectFile = base.Folder @@ "project.json"
    let outputDirectory = folders.CompileOutput @@ name
    let outputDll = outputDirectory @@ sprintf "%s.dll" name
    let packageDir = folders.Package @@ name

    let nugetSpec = folders.Builder @@ "NuGet" @@ sprintf "%s.nuspec" name
    let nugetPkg = folders.Package @@ sprintf "%s.%s.nupkg" name BuildContext.FullVersion

    let zip = folders.Package @@ sprintf "%s.zip" name

    member this.DnxProjectFile = dnxProjectFile
    member this.OutputDirectory = outputDirectory
    member this.OutputDll = outputDll
    
    member this.NugetSpec = nugetSpec
    member this.NugetPkg = nugetPkg

    member this.Zip = zip
    
    member this.Title = assemblyTitle



let MakeBuildInfo (project: NugetProject) (folders : Folders) = 
    let path = folders.Source @@ project.Name @@ "/Properties/AssemblyInfo.cs"
    let infoVersion = sprintf "%s built on %s" BuildContext.FullVersion (System.DateTime.UtcNow.ToString())
    let copyright = sprintf "Brian Chavez © %i" (System.DateTime.UtcNow.Year)
    let attrs = 
          [
              Attribute.Title project.Title
              Attribute.Product project.Name
              Attribute.Company "Brian Chavez"  
              Attribute.Copyright copyright
              Attribute.Version BuildContext.Version
              Attribute.FileVersion BuildContext.Version
              Attribute.InformationalVersion infoVersion
              Attribute.Trademark "Apache License v2.0"
              Attribute.Description "http://www.github.com/bchavez/RethinkDb.Driver"
          ]
    CreateCSharpAssemblyInfo path attrs


open System.Reflection

let DynInvoke (instance : obj) (methodName : string) (args : obj[]) =
    let objType = instance.GetType();
    let invoke = objType.InvokeMember(methodName, BindingFlags.Instance ||| BindingFlags.Public, null, instance, args )
    ()