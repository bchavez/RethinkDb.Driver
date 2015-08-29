<img src="https://raw.githubusercontent.com/bchavez/RethinkDb.Driver/master/Docs/logo.png" align='right' />
![Build Status](https://bchavez.visualstudio.com/DefaultCollection/_apis/public/build/definitions/0e63b37e-487a-4bcd-83d7-c43e7feb96af/3/badge) ![Nuget](https://img.shields.io/nuget/v/RethinkDb.Driver.svg) ![Users](https://img.shields.io/nuget/dt/RethinkDb.Driver.svg) ![Twitter](https://img.shields.io/twitter/url/https/github.com/bchavez/RethinkDb.Driver.svg?style=social)

# RethinkDb.Driver
A RethinkDB database driver written in C# striving for 100% API compatibility and completeness.

This driver is based on https://github.com/rethinkdb/rethinkdb/tree/josh/java-driver.

The code here is a one-to-one port of the Java driver. The basic mechanics and 
architecture of both drivers are the same.

#### Build Process

The build process is similar to the Java driver, except that this C# driver
starts off from the **JSON** files created from `ql2.proto` by `metajava.py` script.
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

##### CodeGen Templates

The code generator templates are located in [`Source/Templates/CodeGen/`](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/Templates/CodeGen).
The templates are [RazorGenerator](https://github.com/RazorGenerator/RazorGenerator) templates. If you wish to update any of the `*.cshtml` code generation
templates be sure to install [RazorGenerator's Visual Studio Extension](https://visualstudiogallery.msdn.microsoft.com/1f6ec6ff-e89b-4c47-8e79-d2d68df894ec)
or use a RazorGenerator's MSBuild task to transform the Razor `*.cshtml` templates to `*.generated.cs` code-behind files.

