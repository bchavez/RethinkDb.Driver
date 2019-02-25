<img src="https://raw.githubusercontent.com/wiki/bchavez/RethinkDb.Driver/GitHubBanner.png" style="max-width: 100%" />

[![Build status](https://ci.appveyor.com/api/projects/status/8o06bhlnjss2n7k8/branch/master?svg=true)](https://ci.appveyor.com/project/bchavez/rethinkdb-driver/branch/master)

RethinkDb.Driver
================

Project Description
-------------------
A [**RethinkDB**](http://rethinkdb.com/) database driver written in C# with 100% **ReQL** API compatibility and completeness.

This driver is based on the *official* [Java Driver](https://github.com/rethinkdb/rethinkdb/tree/next/drivers/java). The basic mechanics and architecture of both drivers are the same. Except this **C#** driver is a bit more [*cool*, *stylish* and *kick-ass*](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features). :sunglasses:

###### Standout Features
* 1,200+ Unit Tests - This driver passes the same ***ReQL*** test harness as the [official drivers](http://rethinkdb.com/docs/install-drivers/).
* [Awesome documentation](https://github.com/bchavez/RethinkDb.Driver/wiki).
* [ReGrid Support](https://github.com/bchavez/RethinkDb.Driver/wiki/ReGrid-File-Storage) - Distributed Large Binary Storage

###### The Crypto Tip Jar!
<a href="https://commerce.coinbase.com/checkout/a765bbf2-c5fa-44fb-a801-84249dd465a8"><img src="https://raw.githubusercontent.com/wiki/bchavez/RethinkDb.Driver/tipjar.png" /></a>

## Open-Source and Commercial Licensing
This open-source driver is dual-licensed. Please read below about SSL/TLS restrictions.

###### Commercial License
A commercial license is required if any part of this driver is used to communicate with a **RethinkDB (server or proxy)** using SSL/TLS encryption. A commercial license is also required if any part of this driver is used to interact with [**Compose.IO**](https://www.compose.com). Commercial licenses are sold by [**Bit Armory Inc**](https://www.bitarmory.com/) and are available for [**purchase here**](https://www.bitarmory.com/payments/rethinkdb). More information about commercial licensing can be [found here](https://github.com/bchavez/RethinkDb.Driver/wiki/Using-SSL-TLS-Encryption-(and-connecting-to-Compose.IO)).

###### Open-Source License
As long as SSL/TLS is not used when communicating with a **RethinkDB (server or proxy)**, the driver is free to use for **commercial** and **non-commercial** applications and is covered under a [*modified Apache License 2.0*](https://github.com/bchavez/RethinkDb.Driver/blob/master/LICENSE).

## Getting Help
###### Commercial Support
Independent commercial support and consulting are available for this driver. To ensure best practices in .NET, proper driver usage, training, and critical bug fixes for the **C#** driver contact [**Brian Chavez**](https://github.com/bchavez) ([twitter](https://twitter.com/bchavez), [email](https://github.com/bchavez)) for more information.

###### Community Support
**Discord** ([**Server 1**](https://discord.gg/ZeEq5TE), [**2**](https://discord.gg/UbQyNNs)) and [**Gitter.im**](https://gitter.im/bchavez/RethinkDb.Driver) are the primary means of getting help for free. **Discord** is recommended since voice chat can be helpful when debugging support issues. If your **C#** related question can't be answered by anyone tag **@bchavez** in your question.


## Download & Install
**NuGet Package [RethinkDb.Driver](https://www.nuget.org/packages/RethinkDb.Driver/)**

```powershell
Install-Package RethinkDb.Driver
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
 <td><strong>.NET Framework</strong</td>
 <td align='center'><strong>v4.5</strong></td>
 <td align='center'>n/a</td>
 <td align='center'>n/a</td>
</tr>
<tr>
 <td><strong>.NET Standard</strong></td>
 <td colspan='3' align='center'>All platforms supporting <strong>.NET Standard 2.0</strong> or higher.</br>Eg: <a href="https://docs.microsoft.com/en-us/dotnet/articles/standard/library#net-platforms-support"><strong>.NET Core 2.0</strong></a>.</td>
</tr>
<tr>
 <td><strong>Mono</strong></td>
 <td colspan='3' align='center'>All platforms <strong>4.0.2 SR2</strong> or higher</td>
</tr>
<tr>
	<td colspan='4'></td>
</tr>
<tr>
     <td colspan='4' align='center'><strong>RethinkDB</strong> server <strong>2.3.0</strong> or higher</td>
</tr>
</table>

Documentation
-----
* [Home](https://github.com/bchavez/RethinkDb.Driver/wiki)
* [Query Examples](https://github.com/bchavez/RethinkDb.Driver/tree/master/Source/RethinkDb.Driver.Tests/ReQL)
* [Logging](https://github.com/bchavez/RethinkDb.Driver/wiki/Protocol-Debugging)
* [Connections & Pooling](https://github.com/bchavez/RethinkDb.Driver/wiki/Connections-&-Pooling)
  * [Single Connection](https://github.com/bchavez/RethinkDb.Driver/wiki/Connections-&-Pooling#single-connection-no-pooling)
  * [Round Robin Pooling](https://github.com/bchavez/RethinkDb.Driver/wiki/Connections-&-Pooling#round-robin-strategy)
  * [Epsilon Greedy Pooling](https://github.com/bchavez/RethinkDb.Driver/wiki/Connections-&-Pooling#epsilon-greedy-strategy)
  * [Using SSL / TLS Encryption](https://github.com/bchavez/RethinkDb.Driver/wiki/Using-SSL-TLS-Encryption-(and-connecting-to-Compose.IO))
      * [Connecting to Compose.IO](https://github.com/bchavez/RethinkDb.Driver/wiki/Using-SSL-TLS-Encryption-(and-connecting-to-Compose.IO)#connecting-to-composeio)
* [Extra C# Features](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features)
  * [Optional Arguments](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#optional-arguments)
  * [Bracket](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#bracket)
  * [DLR Integration](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#dynamic-language-runtime-dlr-integration)
  * [Cursor[T]](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#cursort-support)
  * [Run Helpers](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#run-helpers)
     * [`.RunAtom`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#runatom)
     * [`.RunWrite`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#runwrite-for-inserts-updates-and-deletes)
     * [`.RunCursor`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#runcursor)
     * [`.RunResult`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#runresult)
     * [`.RunChanges`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#runchangest)
     * [`.RunGrouping`](https://github.com/bchavez/RethinkDb.Driver/wiki/Run-Helpers#rungroupingtkeytitem)
  * [Async/Await Support](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#asyncawait-support)
  * [POCO Support](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#poco-support)
  * [DAO Support](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#document-access-object-support)
  * [JObject Support](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#jobject-support)
  * [Anon Type Insert](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#anonymous-type-insert-support)
  * [Anon Type Projection](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#anonymous-type-map-projection)
  * [Consuming Changefeeds](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#consuming-changefeeds)
  * [Reactive Extensions](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#reactive-extensions-rx-support)
  * [Implicit Operators](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#implicit-conversion-operator-overload)
  * [Serializing Expressions](https://github.com/bchavez/RethinkDb.Driver/wiki/Extra-C%23-Driver-Features#serializing-reql-expressions)
* [GOTCHA Goblins!](https://github.com/bchavez/RethinkDb.Driver/wiki/GOTCHA!)
* [LINQ to ReQL Provider](https://github.com/bchavez/RethinkDb.Driver/wiki/LINQ-to-ReQL-Provider)
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

[**RethinkDB** Discord Help](https://discord.gg/wnkWBVh)
[**RethinkDB** Gitter Help](https://gitter.im/bchavez/RethinkDb.Driver)

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
------------
Created by [Brian Chavez](http://bchavez.bitarmory.com) ([twitter](https://twitter.com/bchavez)). Originally ported from the Java Driver by [Josh Kuhn](https://github.com/deontologician). Special thanks to the rest of the RethinkDB team ([Josh](https://github.com/deontologician), [AtnNn](https://github.com/AtnNn), [danielmewes](https://github.com/danielmewes), [neumino](https://github.com/neumino), [VeXocide](https://github.com/VeXocide)) for answering ReQL protocol questions. Also, special thanks to [Annie Ruygt](https://github.com/ahruygt) for the wonderful GitHub banner!

A big thanks to GitHub and all contributors:

* [`jrote1`](https://github.com/jrote1) (Jake Rote) ~ LINQ Provider
* [fiLLLip](https://github.com/fiLLLip) (Filip Andre Larsen Tomren)
* [cadabloom](https://github.com/cadabloom)

Music Wall
----------
:musical_score: A small collection of videos that helped in the creation of this driver. =)

<table>
<tr>
<td>
    <a href=
    "http://www.youtube.com/watch?feature=player_embedded&v=1lyu1KKwC74"
    target="_blank"><img alt="Summer Time" border="10" height="180"
    src="http://img.youtube.com/vi/1lyu1KKwC74/0.jpg" width=
    "240"></a>
</td>
<td>
    <a href=
    "http://www.youtube.com/watch?feature=player_embedded&v=XmSdTa9kaiQ"
    target="_blank"><img alt="U2" border="10" height="180" src=
    "http://img.youtube.com/vi/XmSdTa9kaiQ/0.jpg" width="240"></a>
</td>
<td>
    <a href=
    "http://www.youtube.com/watch?feature=player_embedded&v=7xzU9Qqdqww"
    target="_blank"><img alt="Cudi" border="10" height="180" src=
    "http://img.youtube.com/vi/7xzU9Qqdqww/0.jpg" width="240"></a>
</td>
</tr>



<tr>
<td>
    <a href="http://www.youtube.com/watch?feature=player_embedded&v=nXri9t8yrr4" target="_blank">
        <img src="http://img.youtube.com/vi/nXri9t8yrr4/0.jpg" alt="Kuffdam & Plant" width="240" height="180" border="10" />
    </a>
</td>
<td>
    <a href=
    "http://www.youtube.com/watch?feature=player_embedded&v=FyASdjZE0R0"
    target="_blank"><img alt="NIN" border="10" height="180" src=
    "http://img.youtube.com/vi/FyASdjZE0R0/0.jpg" width="240"></a>
</td>
<td>
    <a href=
    "http://www.youtube.com/watch?feature=player_embedded&v=dd00Qh9yZUg"
    target="_blank"><img alt="Glitch Mob" border="10" height="180" src=
    "http://img.youtube.com/vi/dd00Qh9yZUg/0.jpg" width="240"></a>
</td>
</tr>

</table>


