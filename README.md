<img src="https://raw.githubusercontent.com/bchavez/RethinkDb.Driver/master/Docs/logo.png" align='right' />
![Build Status](https://bchavez.visualstudio.com/DefaultCollection/_apis/public/build/definitions/0e63b37e-487a-4bcd-83d7-c43e7feb96af/3/badge) ![Nuget](https://img.shields.io/nuget/v/RethinkDb.Driver.svg) ![Users](https://img.shields.io/nuget/dt/RethinkDb.Driver.svg) ![Twitter](https://img.shields.io/twitter/url/https/github.com/bchavez/RethinkDb.Driver.svg?style=social)

RethinkDb.Driver
================

Project Description
-------------------
A RethinkDB database driver written in C# striving for 100% API compatibility and completeness.

This driver is based on the *official* [Java Driver](https://github.com/rethinkdb/rethinkdb/tree/josh/java-driver).

This driver and the official Java Driver are *still under active development*.

The code here is a one-to-one port of the Java driver. The basic mechanics and 
architecture of both drivers are the same.

### Download & Install
**NuGet Package [RethinkDb.Driver](https://www.nuget.org/packages/RethinkDb.Driver/)**

```
Install-Package RethinkDb.Driver
```

Usage
-----
```csharp
[Test]
public void can_connect()
{
    var c = r.connection()
        .hostname("192.168.0.11")
        .port(RethinkDBConstants.DEFAULT_PORT)
        .timeout(60)
        .connect();
    
    var result = r.random(1, 9).add(r.random(1, 9)).run<JValue>(c).ToObject<int>();
    Console.WriteLine(result);
    result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
}
```

Building
--------

#### Prerequisites
* [Visual Studio 2015 Community](https://www.visualstudio.com/vs-2015-product-editions) or higher
* NuGet Package Command Line installed in PATH [(via NuGet.org)](http://docs.nuget.org/consume/installing-nuget) or [(via Chocolatey)](https://chocolatey.org/packages/NuGet.CommandLine)

#### Build Commands
* `git clone https://github.com/bchavez/RethinkDb.Driver.git`
* `cd RethinkDb.Driver`
* `build`

If you want to build the nuget package, run:
* `build pack`

The following folders at the root level be generated:
* `__compile` - Contains the result of the build process.
* `__package` - Contains the result of the packaging process.

#### Project Structure
* `Source\Builder` - Primary location where build tasks are defined. See `BauBuild.cs`.
* `Source\RethinkDb.Driver` - The RethinkDB C# driver.
* `Source\RethinkDb.Driver.Tests` - Driver unit tests.
* `Source\Templates` - Code generation templates.

#### Build Process

The build process is similar to the Java driver, except this C# driver
starts off from **JSON** files created from `ql2.proto` by `metajava.py` script.
The **JSON** files are required for building C# AST classes from JSON. 
The required JSON files are:

* `proto_basic.json`
* `term_info.json`
* `global_info.json`


These files reside inside [Source/Templates/Metadata](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/Templates/Metadata) 
@ [8e701ed158e649c25984e568431e96d5c675b24a](https://github.com/rethinkdb/rethinkdb/tree/8e701ed158e649c25984e568431e96d5c675b24a)

If you wish to update / refresh the AST classes (and enums) from `ql2.proto` you'll first
need to generate the updated `*.json` files from `metajava.py` over in the Java driver. Then
copy/update/overwrite the `*.json` files into `Source/Templates/Metadata`.

Next, run the unit test `Generate_All` inside [Source/Templates/Generator.cs](https://github.com/bchavez/RethinkDb.Driver/blob/master/Source/Templates/Generator.cs)
and the AST classes will be re-generated.

#### CodeGen Templates

The code generator templates are located in [`Source/Templates/CodeGen/`](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/Templates/CodeGen).
The templates are [RazorGenerator](https://github.com/RazorGenerator/RazorGenerator) templates. If you wish to update any of the `*.cshtml` code generation
templates be sure to install [RazorGenerator's Visual Studio Extension](https://visualstudiogallery.msdn.microsoft.com/1f6ec6ff-e89b-4c47-8e79-d2d68df894ec)
or use a RazorGenerator's MSBuild task to transform the Razor `*.cshtml` templates to `*.generated.cs` code-behind files.
