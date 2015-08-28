# RethinkDb.Driver
A RethinkDB database driver written in C# striving for 100% API compatibility and completeness.

This driver is based on https://github.com/rethinkdb/rethinkdb/tree/josh/java-driver.

The code here is a one-to-one port of the Java driver. The basic mechanics and 
architecture are the same.

#### Build Process

The build process is pretty much the same as the Java driver, except that this C# driver
starts off from the `ql2.proto` **JSON** files created by the `metajava.py` script.
The **JSON** files are required for building C# AST classes from JSON. 
The required JSON files are:

* `proto_basic.json`
* `term_info.json`
* `global_info.json`


These files reside inside [Source/Templates/Metadata](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/Templates/Metadata) 
@ (8e701ed158e649c25984e568431e96d5c675b24a)[https://github.com/rethinkdb/rethinkdb/tree/8e701ed158e649c25984e568431e96d5c675b24a]

If you wish to update / refresh the AST classes (and enums) from `ql2.proto` you'll first
need to generate the updated `*.json` files from `metajava.py` over in the Java driver. Then
copy/update/overwrite the `*.json` files into `Source/Templates/Metadata`.

Next, run the unit test `Generate_All` inside [Source/Templates/Generator.cs](https://github.com/bchavez/RethinkDb.Driver/blob/master/Source/Templates/Generator.cs)
and the AST classes will be re-generated.
