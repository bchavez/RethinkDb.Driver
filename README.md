<img src="https://raw.githubusercontent.com/bchavez/RethinkDb.Driver/master/Docs/logo.png" align='right' />
[![Build status](https://ci.appveyor.com/api/projects/status/8o06bhlnjss2n7k8?svg=true)](https://ci.appveyor.com/project/bchavez/rethinkdb-driver) ![Nuget](https://img.shields.io/nuget/v/RethinkDb.Driver.svg) ![Users](https://img.shields.io/nuget/dt/RethinkDb.Driver.svg) ![Twitter](https://img.shields.io/twitter/url/https/github.com/bchavez/RethinkDb.Driver.svg?style=social)

RethinkDb.Driver
================

Project Description
-------------------
A RethinkDB database driver written in C# striving for 100% API compatibility and completeness.

This driver is based on the *official* [Java Driver](https://github.com/rethinkdb/rethinkdb/tree/josh/java-driver). This driver and the official Java Driver are *still under active development*.

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
public static RethinkDB r = RethinkDB.r;

[Test]
public void can_connect()
{
    var c = r.connection()
        .hostname("192.168.0.11")
        .port(RethinkDBConstants.DEFAULT_PORT)
        .timeout(60)
        .connect();
    
    var result = r.random(1, 9).add(r.random(1, 9)).run<int>(c);
    Console.WriteLine(result);
    result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
}
// Output: 8
```
You should be able to follow any examples found in the [ReQL documentation](http://www.rethinkdb.com/api/javascript/) with this driver.


Building
--------

#### Prerequisites
* [Visual Studio 2015 Community](https://www.visualstudio.com/vs-2015-product-editions) or higher
* NuGet Package Command Line installed in your PATH [via NuGet.org](http://docs.nuget.org/consume/installing-nuget) or [via Chocolatey](https://chocolatey.org/packages/NuGet.CommandLine).
* (Optional) [RazorGenerator](https://github.com/RazorGenerator/RazorGenerator) to modify CodeGen templates.

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
* `build.cmd` - Ensures build enviornment and fowards build commands to `Builder`.
* `Source\Builder` - Primary location where build tasks are defined. See [`BauBuild.cs`](https://github.com/bchavez/RethinkDb.Driver/blob/master/Source/Builder/BauBuild.cs).
* `Source\RethinkDb.Driver` - The RethinkDB C# driver.
* `Source\RethinkDb.Driver.Tests` - Driver unit tests.
* `Source\Templates` - Code generation templates.

#### Build Process

The build process is similar to the Java driver, except this C# driver
requires **JSON** metadata files derived from `ql2.proto` by the Java Driver's
`metajava.py` script. The **JSON** metadata files are:

* `proto_basic.json`
* `global_info.json`
* `java_term_info.json`


These files reside inside [Source/Templates/Metadata](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/Templates/Metadata).

`java_term_info.json` is a special file (not to be confused with `term_info.json`).
`java_term_info.json` is a more refined output of `term_info.json` that includes extra metadata to support Java language semantics when producing RethinkDB's AST. `java_term_info.json` generated 
by running the following command in the Java driver's directory:

`python metajava.py --term-info term_info.json --output-file java_term_info.json generate-java-terminfo`

If you wish to update the C# AST classes (and enums) you first
need to re-generate `*.json` files from `metajava.py` script that resides the Java driver. Then
copy/overwrite the `*.json` files in `Source/Templates/Metadata`.

The `build codegen` task will use the `*.json` files to regenerate all the AST C# classes; which, in effect, runs `Templates\GeneratorForAst.cs:Generate_All()`.

#### CodeGen Templates

The code generator templates are located in [`Source/Templates/CodeGen/`](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/Templates/CodeGen).
The templates are [RazorGenerator](https://github.com/RazorGenerator/RazorGenerator) templates. Updating any of the `*.cshtml` code generation
templates requires installing [RazorGenerator's Visual Studio Extension](https://visualstudiogallery.msdn.microsoft.com/1f6ec6ff-e89b-4c47-8e79-d2d68df894ec)
or using RazorGenerator's MSBuild task to transform the Razor `*.cshtml` templates to `*.generated.cs` razor code-behind files.
