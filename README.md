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

**Minimum Supported Runtimes**

<table style='border-collapse: collapse;'>
<tr>
	<th></th>

	<th><img src='https://github.com/Turbo87/Font-Awesome/raw/platform-icons/svg/windows.png'/> Windows</th>
	
	<th><img src='https://github.com/Turbo87/Font-Awesome/raw/platform-icons/svg/linux.png'> Linux</th>

	<th><img src='https://github.com/Turbo87/Font-Awesome/raw/platform-icons/svg/apple.png'/> Mac OS X</th>
</tr>
<tr>
 <td><strong>.NET Framework</strong</td>
 <td align='center'><strong>v4.5</strong></td>
 <td align='center'>n/a</td>
 <td align='center'>n/a</td>
</tr>
<tr>
 <td><strong>CoreCLR</strong></td>
 <td colspan='3' align='center'>All platforms <strong>1.0.0-beta7</strong> or higher</td>
</tr>
<tr>
 <td><strong>Mono</strong></td>
 <td colspan='3' align='center'>All platforms <strong>4.02 SR2</strong> or higher</td>
</tr>
<tr style='border-top: medium solid;'>
 <td><strong>RethinkDB Server</strong></td>
 <td align='center'>n/a</td>
 <td colspan='2' align='center'>Server <strong>2.1.5</strong> or higher</td>
</tr>
</table>



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

#### Checkout
* `git clone https://github.com/bchavez/RethinkDb.Driver.git`
* `cd RethinkDb.Driver`

#### Build Commands
The following build tasks are defined in [`BauBuild.cs`](https://github.com/bchavez/RethinkDb.Driver/blob/master/Source/Builder/BauBuild.cs). Execute any of the following build commands in the root project folder. 
* `build` - By default, triggers `build msb`.
* `build msb` - Builds binaries for **.NET Framework v4.5** using **msbuild**.
* `build dnx` - Builds **CoreCLR** binaries using **dnu build**.
* `build mono` - Builds **Mono** binaries using **xbuild**.
* `build clean` - Cleans up build.
* `build astgen` - Regenerates C# AST classes from `*.json` files.
* `build pack` - Builds local NuGet packages.
* `build yamlimport` - Imports and cleans up freshly copied YAML tests from the Java driver. See **Unit Tests** section below.
* `build testgen` - Generates C# unit tests from refined YAML tests.

The following folders at the root checkout level be generated:
* `__compile` - Contains the result of the build process.
* `__package` - Contains the result of the packaging process.

#### Project Structure
* `build.cmd` - Ensures sane build environment and forwards build commands to `Builder`.
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

The result of the command above will produce `java_term_info.json`. The **JSON** files inside the Java driver's folder can be copied to & overwritten inside the C# driver's folder `Source/Templates/Metadata`. The `build astgen` task will use the `*.json` files mentioned above to regenerate all AST C# classes, protocol enums, and various models.

`build astgen` build task essentially runs `Templates\GeneratorForAst.cs:Generate_All()`.

#### Updating Code Generation Templates

The code generator templates are located in [`Source/Templates/CodeGen/`](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/Templates/CodeGen).
The templates are [RazorGenerator](https://github.com/RazorGenerator/RazorGenerator) templates. Updating any of the `*.cshtml` code generation
templates requires installing [RazorGenerator's Visual Studio Extension](https://visualstudiogallery.msdn.microsoft.com/1f6ec6ff-e89b-4c47-8e79-d2d68df894ec)
or using RazorGenerator's MSBuild task to transform the Razor `*.cshtml` templates to `*.generated.cs` razor code-behind files.

Unit Tests
--------
Like the official Java driver, the C# driver also derives its **Query Language Tests** from the official [`rethinkdb\test\rql_test`](https://github.com/rethinkdb/rethinkdb/tree/next/test/rql_test) YAML tests.

The C# unit tests have been automatically converted from YAML to C# and reside inside:

* `Source\RethinkDb.Driver.Tests\Generated`

You can simply run all the unit tests in `RethinkDb.Driver.Tests` to test the driver's correctness.

#### Updating Generated Tests
The generated unit tests can be updated. The process for updating the auto generated unit tests requires `convert_tests.py` from the Java driver source. The following process updates the C# generated tests from YAML files:

1. Checkout the Java driver.
2. Ensure you have `java_term_info.json` (if not read above on how to generate it).
3. Copy our special YAML [**`Test.yaml`**](https://github.com/bchavez/RethinkDb.Driver/blob/master/Source/Templates/CodeGen/Test.yaml) template to the Java driver's `java\template\` directory. The `template\Test.yaml`  file (from our repo) should be along side `template\Test.java` file in the Java driver folder.
4. Make two edits to `convert_test.py` to ensure the `Test.yaml` file is used when generating test outputs.

	```
    class TestFile(object): def render(self)
        ....
        self.renderer.render(
            'Test.yaml', //EDIT 1: from Test.java 
            output_dir=self.test_output_dir,
            output_name=self.module_name + '.yaml', //EDIT 2: from .java
            ....
        )
	```

    All we're doing here is using our template instead of
    the Test.java template to output the expected Java lines in ReQL.
5. Next, run `python3 convert_tests.py` in the Java driver's folder. The result of the command will result in a batch of YAML files in `/src/test/java/gen/*.yaml`.
6. Copy `/src/test/java/gen/*.yaml` and overwrite all the YAML files in the C# driver's folder:
 
   * `Source\Templates\UnitTests` 
  
   The fresh YAML files from the Java folder will be base64 encoded to keep non-ascii characters intact and avoid complex character escape sequences when making the transition.
7. Next, clean up and decode the imported YAML tests by running `build yamlimport` task. The YAML tasks should now be valid YAML tests with correct escape sequences and character encodings.
8. Lastly, run `build testgen` to regenerate the C# tests in `Source\RethinkDb.Driver.Tests\Generated` from the newly imported YAML files.


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

Since we're using `Common.Logging` you can customize the log level and use log adapters for popular logging libraries like **NLog** or **log4net**.

#### Log Levels
 * `TRACE` - Logs the AST JSON sent to the server and the JSON response.
 * `DEBUG` - Logs only JSON responses received from the server.

If you're using **CoreCLR**, logging is handled by **[`Microsoft.Extensions.Logging`](https://github.com/aspnet/Logging)**.

#### Query Debugging
If you're concerned the a C# driver is sending an invalid AST query, you can compare C# AST query sent to the server with the official JavaScript driver in the RethinkDB web-admin console.

There's two ways to find the JSON AST from the web-admin console:

##### Option A: Using `r.expr` and `.build`
Thanks to [@neumino](https://github.com/rethinkdb/rethinkdb/issues/4812#issuecomment-147144128) the following will output the AST in the web-admin console:

```
r.expr(
  r.table("foo").map(function(x) { return x.merge({foo: "bar"})}).build()
  // Or <your_query>.build()
)
```

##### Option B: Using Chrome Developer Console
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

Using the debugging technique above, you can compare the C# driver's AST output with the JavaScript AST to help you locate any AST discrepancies.
