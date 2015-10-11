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
* :heavy_check_mark: **DO** keep pull requests small so they can be easily reviewed. 
* :heavy_check_mark: **DO** make sure unit tests pass.
* :x: **AVOID** breaking the continuous integration build. 
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

`python3 metajava.py --term-info term_info.json --output-file java_term_info.json generate-java-terminfo`

The result of the command above will produce `java_term_info.json`. The **JSON** files inside the Java driver's folder can be copied to & overwritten inside the C# driver's folder `Source/Templates/Metadata`. The `build codegen` task will use the `*.json` files mentioned above to regenerate all AST C# classes, protocol enums, and various models.

`build codegen` build task essentially runs `Templates\GeneratorForAst.cs:Generate_All()`.

#### Changing Code Generation Templates

The code generator templates are located in [`Source/Templates/CodeGen/`](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/Templates/CodeGen).
The templates are [RazorGenerator](https://github.com/RazorGenerator/RazorGenerator) templates. Updating any of the `*.cshtml` code generation
templates requires installing [RazorGenerator's Visual Studio Extension](https://visualstudiogallery.msdn.microsoft.com/1f6ec6ff-e89b-4c47-8e79-d2d68df894ec)
or using RazorGenerator's MSBuild task to transform the Razor `*.cshtml` templates to `*.generated.cs` razor code-behind files.

Protocol Debugging 
--------
Debugging the JSON protocol can be useful when debugging driver issues. This driver uses `Common.Logging` for the logging infrastructure. To enable driver protocol logging add the following to your **App.config** (or **Web.config**):

```
  <configSections>
    <sectionGroup name="common">
      <section name="logging" type="Common.Logging.ConfigurationSectionHandler, Common.Logging" />
    </sectionGroup>
  </configSections>

  <common>
    <logging>
      <factoryAdapter type="Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter, Common.Logging">
        <arg key="level" value="TRACE" />
        <arg key="showLogName" value="false" />
        <arg key="showDateTime" value="false" />
      </factoryAdapter>
    </logging>
  </common>
```

Since we're using `Common.Logging` you can customize the log level and various log adapters for popular logging libraries like **NLog** or **log4net**.

#### Log Levels
 * `TRACE` - Logs the AST JSON sent to the server and the JSON response.
 * `DEBUG` - Logs only JSON responses received from the server.

#### Query Debugging
You can compare your query sent to the server with a query written in the RethinkDB web-admin console.

1. Browse to the RethinkDb web-admin console.
2. Select the RethinkDB **Data Explorer**. 
3. Open up the Chrome developer console.
4. Select the *Sources* tab, and press F8 to pause script execution.
5. After the script pauses, type the following in the JavaScript console: `n.timers`. You'll get output similar to:
	* `Object {1: Object, 2: Object}`
6. Next, disable the timers by typing:
	* `driver.stop_timer(1)`
	* `driver.stop_timer(2)`
7. Press F8 to resume script execution. 
8. Select the *Network* tab, and clear the requests.
9. Now finally, type your query in the **Data Explorer** and press *Run*.
10. Your query's AST is in the HTTP request ***payload*** for network traffic named `?conn_id=hash`. For example, running `r.now()` translates to:
	* `[1,[103,[]], {global args}]`

Using the debugging technique above, you can compare the C# driver's AST output with the RethinkDB web-admin's AST to help you locate any AST discrepancies.
