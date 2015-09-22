<img src="https://raw.githubusercontent.com/bchavez/RethinkDb.Driver/master/Docs/logo.png" align='right' />
[![Build status](https://ci.appveyor.com/api/projects/status/8o06bhlnjss2n7k8?svg=true)](https://ci.appveyor.com/project/bchavez/rethinkdb-driver) [![Nuget](https://img.shields.io/nuget/v/RethinkDb.Driver.svg)](https://www.nuget.org/packages/RethinkDb.Driver/) [![Users](https://img.shields.io/nuget/dt/RethinkDb.Driver.svg)](https://www.nuget.org/packages/RethinkDb.Driver/) [![Twitter](https://img.shields.io/twitter/url/https/github.com/bchavez/RethinkDb.Driver.svg?style=social)](https://twitter.com/intent/tweet?text=Wow:&amp;amp;url=https%3A%2F%2Fgithub.com%2Fbchavez%2FRethinkDb.Driver)

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
You should be able to follow any examples found in the official [ReQL documentation](http://www.rethinkdb.com/api/javascript/) with this driver.



Contributing
------------
Here are some helpful guidelines to keep in mind when contributing.  While following them isn't absolutely required, it does help everyone to accept your pull-requests with maximum awesomeness.

* :heavy_check_mark: **CONSIDER** adding a unit test if your PR resolves an issue.
* :heavy_check_mark: **DO** keep pull requests small so they can be easily digested. 
* :heavy_check_mark: **DO** make sure unit tests pass.
* :x: **AVOID** break the continuous integration build. 
* :x: **AVOID** making significant changes to the driver's overall architecture. We'd like to keep this driver in-sync with the overall architecture of the Java driver so both projects benefit from bug fixes and new features. 

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
* `build.cmd` - Ensures build environment and forwards build commands to `Builder`.
* `Source\Builder` - Primary location where build tasks are defined. See [`BauBuild.cs`](https://github.com/bchavez/RethinkDb.Driver/blob/master/Source/Builder/BauBuild.cs).
* `Source\RethinkDb.Driver` - The RethinkDB C# driver.
* `Source\RethinkDb.Driver.Tests` - Driver unit tests.
* `Source\Templates` - Code generation templates.

#### Driver Architecture

There are two main components of this C# driver. The **ReQL Abstract Syntax Tree (AST)** and **the infrastructure** to handle serialization/deserialization of the AST. The infrastructure also handles communication with a RethinkDB server.

#### AST & Code Generation

The ReQL AST is located in [`Source\RethinkDb.Driver\Generated\Ast`](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/RethinkDb.Driver/Generated). The AST C# classes are generated using code generation templates in `Source\Templates`. The code generation process is similar to the Java driver, except this C# driver
requires **JSON** metadata files derived the Java driver's
python scripts (namely, `metajava.py`). The **JSON** metadata files required to rebuild the AST (and other objects) are:

* `proto_basic.json`
* `global_info.json`
* `java_term_info.json`


These files reside inside [`Source/Templates/Metadata`](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/Templates/Metadata).

`java_term_info.json` is a special file (not to be confused with `term_info.json`).
`java_term_info.json` is a more refined output of `term_info.json` that includes extra metadata to support OOP language semantics when generating RethinkDB's AST. `java_term_info.json` generated 
by running the following command in the Java driver's folder:

`python metajava.py --term-info term_info.json --output-file java_term_info.json generate-java-terminfo`

The result of the command above will produce `java_term_info.json`. The **JSON** files inside the Java driver's folder can be copied to & overwritten inside the C# driver's folder `Source/Templates/Metadata`. The `build codegen` task will use the `*.json` files mentioned above to regenerate all AST C# classes, protocol enums, and various models.

`build codegen` build task essentially runs `Templates\GeneratorForAst.cs:Generate_All()`.

#### Changing Code Generation Templates

The code generator templates are located in [`Source/Templates/CodeGen/`](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/Templates/CodeGen).
The templates are [RazorGenerator](https://github.com/RazorGenerator/RazorGenerator) templates. Updating any of the `*.cshtml` code generation
templates requires installing [RazorGenerator's Visual Studio Extension](https://visualstudiogallery.msdn.microsoft.com/1f6ec6ff-e89b-4c47-8e79-d2d68df894ec)
or using RazorGenerator's MSBuild task to transform the Razor `*.cshtml` templates to `*.generated.cs` razor code-behind files.
