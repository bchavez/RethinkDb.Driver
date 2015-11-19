<img src="https://raw.githubusercontent.com/bchavez/RethinkDb.Driver/master/Docs/logo.png" align='right' />
[![Build status](https://ci.appveyor.com/api/projects/status/8o06bhlnjss2n7k8?svg=true)](https://ci.appveyor.com/project/bchavez/rethinkdb-driver) [![Nuget](https://img.shields.io/nuget/v/RethinkDb.Driver.svg)](https://www.nuget.org/packages/RethinkDb.Driver/) [![Users](https://img.shields.io/nuget/dt/RethinkDb.Driver.svg)](https://www.nuget.org/packages/RethinkDb.Driver/) [![Twitter](https://img.shields.io/twitter/url/https/github.com/bchavez/RethinkDb.Driver.svg?style=social)](https://twitter.com/intent/tweet?text=Wow:&amp;amp;url=https%3A%2F%2Fgithub.com%2Fbchavez%2FRethinkDb.Driver)

RethinkDb.Driver
================

[![Join the chat at https://gitter.im/bchavez/RethinkDb.Driver](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/bchavez/RethinkDb.Driver?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Project Description
-------------------
A RethinkDB database driver written in C# striving for 100% API compatibility and completeness.

This driver is based on the *official* [Java Driver](https://github.com/rethinkdb/rethinkdb/tree/josh/java-driver). This driver and the official Java Driver are *still under active development*.

The basic mechanics and architecture of both drivers are the same.

### Download & Install
**NuGet Package [RethinkDb.Driver](https://www.nuget.org/packages/RethinkDb.Driver/)**

```
Install-Package RethinkDb.Driver -Pre
```
While **CoreCLR** is still in beta, an additional restore fallback source is needed to restore `Microsoft.Extensions.Logging.Abstractions` reference:
```
dnu restore --fallbacksource https://www.myget.org/F/aspnetvnext/api/v2/
```

**Supported Runtimes**

<table>
<tr>
	<th></th>

	<th><img src='https://github.com/Turbo87/Font-Awesome/raw/platform-icons/svg/windows.png'/> Windows</th>
	
	<th><img src='https://github.com/Turbo87/Font-Awesome/raw/platform-icons/svg/linux.png'> Linux</th>

	<th><img src='https://github.com/Turbo87/Font-Awesome/raw/platform-icons/svg/apple.png'/> Mac OS X</th>
</tr>
<tr>
 <td><strong>Mono</strong></td>
 <td colspan='3' align='center'>All platforms <strong>4.0.2 SR2</strong> or higher</td>
</tr>
<tr>
 <td><strong>CoreCLR</strong></td>
 <td colspan='3' align='center'>All platforms <strong>1.0.0-rc1-final</strong> or higher</td>
</tr>
<tr>
 <td><strong>.NET Framework</strong</td>
 <td align='center'><strong>v4.5</strong></td>
 <td align='center'>n/a</td>
 <td align='center'>n/a</td>
</tr>
<tr>
	<td colspan='4'></td>
</tr>
<tr>
     <td colspan='4' align='center'><strong>RethinkDB</strong> server <strong>2.2.0</strong> or higher</td>
</tr>
</table>

Documentation
-----
* [Home](https://github.com/bchavez/RethinkDb.Driver/wiki)
* [Query Examples](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/RethinkDb.Driver.Tests/ReQL)
* [Extra C# Features](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features)
  * [Optional Arguments](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#optional-arguments)
  * [Bracket](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#bracket)
  * [DLR Integration](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#dynamic-language-runtime-dlr-integration)
  * [POCO Support](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#poco-support)
  * [Anon Type Projection](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#anonymous-type-map-projection)
  * [Cursor[T]](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#cursort-support)
  * [Implicit Operators](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#implicit-conversion-operator-overload)
* [Differences](https://github.com/bchavez/RethinkDb.Driver/wiki/Differences-Between-C%23-and-Java-driver)

Quick Examples
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

int result = r.random(1, 9).add(r.random(1, 9)).run<int>(c);
    Console.WriteLine(result);
    result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
}
// Output: 8

[Test]
public void insert_poco_without_id()
{
    var obj = new Foo { Bar = 1, Baz = 2};
    var result = r.db(DbName).table(TableName).insert(obj).run(conn);
    result.Dump();
}
/*
    //JObject: Insert Response
	{
	  "deleted": 0,
	  "errors": 0,
	  "generated_keys": [
	    "6931c97f-de3d-46d2-b0f9-956af9517a57"
	  ],
	  "inserted": 1,
	  "replaced": 0,
	  "skipped": 0,
	  "unchanged": 0
	}
*/

[Test]
public void insert_an_array_of_pocos()
{
    var list = new[]
        {
            new Foo {id = "a", Baz = 1, Bar = 1},
            new Foo {id = "b", Baz = 2, Bar = 2},
            new Foo {id = "c", Baz = 3, Bar = 3}
        };
    var result = r.db(DbName).table(TableName).insert(list).run(conn);
    result.Dump();
}
/*
    //JObject Insert Response
    {
      "deleted": 0,
      "errors": 0,
      "inserted": 3,
      "replaced": 0,
      "skipped": 0,
      "unchanged": 0
    }
*/


[Test]
public void get_a_poco()
{
    Foo foo = r.db(DbName).table(TableName).get("abc").run<Foo>(conn);
    foo.Dump();
}
//Foo Object
/*
    {
      "id": "abc",
      "Bar": 1,
      "Baz": 2
    }
*/
```

Contributing
------------
If you'd like to contribute, please consider reading some [helpful tips before making changes](https://github.com/bchavez/RethinkDb.Driver/blob/master/CONTRIBUTING.md).
 
Contributors
---------
Created by [Brian Chavez](http://bchavez.bitarmory.com). Originally ported from the Java Driver by [Josh Kuhn](https://github.com/deontologician).

A big thanks to GitHub and all contributors:

* [fiLLLip](https://github.com/fiLLLip) (Filip Andre Larsen Tomren)
* [cadabloom](https://github.com/cadabloom)

