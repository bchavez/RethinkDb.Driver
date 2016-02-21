<img src="https://raw.githubusercontent.com/wiki/bchavez/RethinkDb.Driver/GitHubBanner.png" style="max-width: 100%" />

[![Build status](https://ci.appveyor.com/api/projects/status/8o06bhlnjss2n7k8/branch/master?svg=true)](https://ci.appveyor.com/project/bchavez/rethinkdb-driver/branch/master) [![Twitter](https://img.shields.io/twitter/url/https/github.com/bchavez/RethinkDb.Driver.svg?style=social)](https://twitter.com/intent/tweet?text=%23RethinkDB %23reql driver for C%23 and .NET:&amp;amp;url=https%3A%2F%2Fgithub.com%2Fbchavez%2FRethinkDb.Driver) [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/bchavez/RethinkDb.Driver?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

RethinkDb.Driver
================

Project Description
-------------------
A [**RethinkDB**](http://rethinkdb.com/) database driver written in C# striving for 100% API compatibility and completeness.

This driver is based on the *official* [Java Driver](https://github.com/rethinkdb/rethinkdb/tree/next/drivers/java). The basic mechanics and architecture of both drivers are the same.

#### Commercial Support
Independent commercial support and consulting is available for this ***community driver***. To ensure best practices in .NET, proper driver usage, and critical bug fixes for the **C#** ***community driver*** contact [**Brian Chavez**](https://github.com/bchavez) for more information. Commercial support for **RethinkDB** ***Server*** out-of-scope of the **C#** ***community driver*** can be found [here](http://rethinkdb.com/services/). 



### Download & Install
**NuGet Package [RethinkDb.Driver](https://www.nuget.org/packages/RethinkDb.Driver/)**

```
Install-Package RethinkDb.Driver -Pre
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
* [Logging](https://github.com/bchavez/RethinkDb.Driver/wiki/Protocol-Debugging)
* [Connections & Pooling](https://github.com/bchavez/RethinkDb.Driver/wiki/Connections-&-Pooling)
  * [Single Connection](https://github.com/bchavez/RethinkDb.Driver/wiki/Connections-&-Pooling#single-connection-no-pooling)
  * [Round Robin Pooling](https://github.com/bchavez/RethinkDb.Driver/wiki/Connections-&-Pooling#connection-pool-round-robin)
  * [Epsilon Greedy Pooling](https://github.com/bchavez/RethinkDb.Driver/wiki/Connections-&-Pooling#connection-pool-epsilon-greedy)
* [Extra C# Features](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features)
  * [Optional Arguments](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#optional-arguments)
  * [Bracket](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#bracket)
  * [DLR Integration](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#dynamic-language-runtime-dlr-integration)
  * [Cursor[T]](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#cursort-support)
  * [Run Helpers](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#run-helpers)
     * [`.RunAtom`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#runatom)
     * [`.RunCursor`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#runcursor)
     * [`.RunResult`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#runresult)
     * [`.RunChanges`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#runchangest)
     * [`.RunGrouping`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#rungroupingtkeytitem)
  * [Async/Await Support](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#asyncawait-support)
  * [POCO Support](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#poco-support)
  * [JObject Support](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#jobject-support)
  * [Anon Type Insert](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#anonymous-type-insert-support)
  * [Anon Type Projection](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#anonymous-type-map-projection)
  * [Consuming Changefeeds](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#consuming-changefeeds)
  * [Reactive Extensions](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#reactive-extensions-rx-support)
  * [Implicit Operators](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#implicit-conversion-operator-overload)
* [Differences](https://github.com/bchavez/RethinkDb.Driver/wiki/Differences-Between-C%23-and-Java-driver)
* [Java ReQL API Documentation](http://rethinkdb.com/api/java/)

### ReGrid File Storage
* [What is ReGrid?](https://github.com/bchavez/RethinkDb.Driver/wiki/ReGrid-File-Storage)
* [Getting Started](https://github.com/bchavez/RethinkDb.Driver/wiki/ReGrid-File-Storage#getting-started)
* [Buckets](https://github.com/bchavez/RethinkDb.Driver/wiki/ReGrid-File-Storage#buckets)
* [Files](https://github.com/bchavez/RethinkDb.Driver/wiki/ReGrid-File-Storage#files)
  * [Revision Numbers](https://github.com/bchavez/RethinkDb.Driver/wiki/ReGrid-File-Storage#revision-numbers) 
  * [Upload](https://github.com/bchavez/RethinkDb.Driver/wiki/ReGrid-File-Storage#upload)
  * [Download](https://github.com/bchavez/RethinkDb.Driver/wiki/ReGrid-File-Storage#download)
  * [Seekable Streams](https://github.com/bchavez/RethinkDb.Driver/wiki/ReGrid-File-Storage#seekable-download-streams)
  * [Delete](https://github.com/bchavez/RethinkDb.Driver/wiki/ReGrid-File-Storage#delete)



### Driver Development
* [Contributing](https://github.com/bchavez/RethinkDb.Driver/blob/master/CONTRIBUTING.md)
* [Getting Started](https://github.com/bchavez/RethinkDb.Driver/wiki/Getting-Started)
* [Unit Tests](https://github.com/bchavez/RethinkDb.Driver/wiki/Unit-Tests)
* [Protocol Debugging](https://github.com/bchavez/RethinkDb.Driver/wiki/Protocol-Debugging)
* [Threading Architecture](https://github.com/bchavez/RethinkDb.Driver/issues/15)
* [Connection Pooling Architecture](https://github.com/bchavez/RethinkDb.Driver/issues/17)

[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/bchavez/RethinkDb.Driver?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

Quick Examples
-----
```csharp
public static RethinkDB R = RethinkDB.R;

[Test]
public void can_connect()
{
    var c = R.Connection()
             .Hostname("192.168.0.11")
             .Port(RethinkDBConstants.DefaultPort)
             .Timeout(60)
             .Connect();

    int result = R.Random(1, 9).Add(R.Random(1, 9)).Run<int>(c);
    Console.WriteLine(result);
    result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
}
// Output: 8

[Test]
public void insert_poco_without_id()
{
    var obj = new Foo { Bar = 1, Baz = 2};
    var result = R.Db("mydb").Table("mytable").Insert(obj).Run(conn);
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
    var result = R.Db("mydb").Table("mytable").Insert(list).Run(conn);
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
    Foo foo = R.Db("mydb").Table("mytable").Get("abc").Run<Foo>(conn);
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
Created by [Brian Chavez](http://bchavez.bitarmory.com) ([twitter](https://twitter.com/bchavez)). Originally ported from the Java Driver by [Josh Kuhn](https://github.com/deontologician). Special thanks to the rest of the RethinkDB team ([Josh](https://github.com/deontologician), [AtnNn](https://github.com/AtnNn), [danielmewes](https://github.com/danielmewes), [neumino](https://github.com/neumino)) for answering ReQL protocol questions. Also, special thanks to [Annie Ruygt](https://github.com/ahruygt) for the wonderful GitHub banner!

A big thanks to GitHub and all contributors:

* [fiLLLip](https://github.com/fiLLLip) (Filip Andre Larsen Tomren)
* [cadabloom](https://github.com/cadabloom)

