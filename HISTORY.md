## v2.3.3
* Roll-up Release for Full .NET Framework since last non-beta release.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.3.3-beta-1
* Issue 62 - Fixed "An address incompatible with the requested protocol was used" on Azure

## v2.3.2
* Roll-up Release for Full .NET Framework since last non-beta release.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.3.2-beta-2
* Connection now exposes ClientLocalEndpoint.
* Better .NET Standard 1.3 and .NET Core RC2 compatibility.

## v2.3.2-beta-1
* Compatibility with .NET Standard 1.3 and .NET Core RC2.

## v2.3.1-beta-3
* Experimental LINQ to ReQL provider support.

## v2.3.1-beta-2
* BREAKING: Issue 39 - Pseudo types are now converted by default in JToken types (JObject, JArray).
*   You'll need to specify .Run*(conn, new { time_format: `raw` }) to keep raw types
*   from being converted. Other raw types: binary_format and group_format.
* BREAKING: Issue 49 - Handle DateTime and DateTimeOffset with ReqlDateTimeConverter
*   instead of Iso8601 AST term.

## v2.3.1-beta-1
* Compatibility with RethinkDB 2.3 and new user/pass authentication system.
* New `Grant` AST term added.
* New permission exception types.
* Issue 41 - Synchronous Run Helpers now throw expected exceptions (unwrapped AggregateException).

## v2.2.10
* Roll-up Release for Full .NET Framework since last non-beta release.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.10-beta-1
* Fixed Issue 36: Inconsistency between AND and OR method signatures.
* Added Fold Term (Note: Not usable until RethinkDB Server 2.3 is released).
* Added support for Union interleave OptArg.
* Added Proxy field to Server:conn.Server().
* BREAKING: .optArg now named .OptArg to follow .NET conventions (My apologies, I missed this one).

## v2.2.9
* Roll-up Release for Full .NET Framework since v2.2.8.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.9-beta-2
* Improved JObject and POCO serialization.

## v2.2.9-beta-1
* Added helper overloads for GetAll, HasFields, WithFields, Pluck, Without, IndexStatus, IndexWait.

## v2.2.8
* Roll-up Release for Full .NET Framework since v2.2.7.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.8-beta-4
* Improved [] operator overloading in AST. Term[`bracket`]. Sometimes wouldn`t get called.

## v2.2.8-beta-3
* Promoted anonymous types to expressions. R.Expr(new {keya="vala"}).Keys()
* Fixed null reference exception in ReGrid.OpenDownloadStreamAsync()

## v2.2.8-beta-2
* Issue 32: Adding back `dnx451`, `dnxcore50`.

## v2.2.8-beta-1
* Issue 32: Switch to new `dotnet` target framework monikers for CoreCLR.

## v2.2.7
* Roll-up Release for Full .NET Framework since v2.2.5.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.7-beta-1
* All public API are squeaky clean.
* Added more more convenience CancellationToken Run*().
* Fixed Cursor.IsFeed bug always false.
* Added more XML docs.
* Inverted the AST generation for faster query composition.
* Fixed some async bugs.

## v2.2.5
* Roll-up Release for Full .NET Framework since v2.2.4.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.
* BREAKING CHANGES:
* -- **ReGrid** specification update: chunks using: file_id and num fields.
* -- **ReQL AST** now using .NET naming conventions. AST is now PascalCase.

## v2.2.5-beta-5
* Completely reimplemented Cursor from the ground up. *Better*, *faster*, *stronger*, *simpler*.
* Async APIs now accept CancellationTokens.

## v2.2.5-beta-4
* Issue 31: Handle null byte[] properly.

## v2.2.5-beta-3
* BREAKING CHANGES:
* -- **ReGrid** specification update: chunks using: file_id and num fields.
* -- **ReQL AST** now using .NET naming conventions. AST is now PascalCase.
* -- RethinkDBConstants using .NET naming conventions.
* Fixed **ReGrid** bug with large uploads.

## v2.2.5-beta-2
* Fixed possible memory leak in Cursor.close()
* Some minor Cursor changes to make MoveNextAsync work better
* Connection and ConnectionPool are now IDisposable.

## v2.2.5-beta-1
* Introducing ReGrid: The RethinkDB Large Object File Store. See docs for more info.
* Added Connection.ConnectionError event. Better for connection pooling.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.4
* Roll-up Release for Full .NET Framework since v2.2.3.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.4-beta-1
* Issue 24 - Aggregate / NullReference after .connect() and immediate .run().

## v2.2.3
* Roll-up Release for Full .NET Framework since v2.2.2.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.3-beta-2
* runResult() now takes IConnection instead of strongly typed Connection.
* Added runResult[T] for SUCCESS_ATOM or SUCCESS_SEQUENCE responses.
* Fixed nullable DateTime? and DateTimeOffset? not converting to reql_type:TIME pesudo type.
* Make ConnectionPool more reliable. Unstable ConnectionPool arised when driver threw errors due to syntax.
* Fixed bug in ReqlBinaryConverter preventing ser/deserialization of 0xFF

## v2.2.2
* Roll-up Release for .NET Framework since v2.2.1.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.2-beta-2
* Issue #21: Fixed nested array types in JObject serialization.
* Notice: ReqlDateTimeConverter serialization implementation changed (non-breaking).
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.2-beta-1
* Issue #21: Allow driver usage of JObject in API. Example: r.table().insert(JObject).run().
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.1
* Roll-up Release for .NET Framework since v2.2.0.
* CoreCLR users please continue using latest beta release until CoreCLR is RTM.

## v2.2.1-beta-2
* Newtonsoft v8.0.2 compatibility.

## v2.2.1-beta-1
* Added remaining top-level aggregation terms.

## v2.2.0
* Release for .NET 4.5 Framework (CoreCLR users please continue using latest beta release until CoreCLR is RTM.).

## v2.2.0-beta-2
* ConnectAsync
* ReconnectAsync
* Connection Pooling: RoundRobin and EpsilonGreedy connection pools.

## v2.2.0-beta-1
* conn.server() and conn.serverAsync(): SERVER_INFO implemented.
* TopLevel AST adjustments for Table: rebalance, reconfigure, and wait_
* Slight adjustment to System.Dynamic.Runtime dependency so no fallback source is needed.

## v0.0.7-alpha7
* async/await run() implementations.
* Database connection thread-safety.
* EnsureSuccess() renamed to AssertNoErrors()
* Assert: Deleted(), Inserted(), skipped(), replaced, etc.. helpers.
* Better Reactive Extension (Rx) semantic compatibility.
* Added Cursor.MoveNext(Timeout) for manual cursor movement.
* Added new helper: runGrouping<TKey,TItem>()
* Added new helper: runAtom<T>()
* See project documentation wiki

## v0.0.7-alpha6
* Added run helpers: runResult(), runChanges<T>()
* Added EnsureSuccess() to help ensure query execution has no errors. Example: insert().runResult().EnsureSuccess(); throws if errors.
* Change[T] class helper to help with change feeds.
* Reactive Extensions .ToObservable() compatibility with .NET 4.5 framework and change feeds.

## v0.0.7-alpha5
* Issue 13: Fixed POCO:byte[] not serializing correctly

## v0.0.7-alpha4
* Better DNX compatibility with dnx451 and dnxcore50.
* Requires DNX RC1.

## v0.0.7-alpha2
* Allow logging in CoreCLR - In startup: loggerFactory.EnableRethinkDbLogging();

## v0.0.5-alpha9
* .map() projections with anonymous types. IE: r.filter().map( g => new {points = g["points"]} )
* Converter.Serializer main configuration point for Newtonsoft.

## v0.0.5-alpha7
* Fixed POCO serialization issues
* ReqlExpr[] uses r.bracket() instead of r.getField();

## v0.0.5-alpha6
* More work on Result helper.
* Feature: .getField() overload helper. Example: .get("id")["Name"] returns field Name.
* Feature: +,-,*,/,&gt;,&lt; etc.. expression operator overloading.
* Feature: Implicit operator overrides. Example: (r.expr(1) + 1).run().
* ChangeFeeds unit tests passing.
* MetaDbs unit tests passing.
* Better support for POCO byte[] binary.
* Fixed bug in Cursor[T].BufferedItems. Respects native reql_type.
* Better support for group()-ed results.
* Moved reql_type converters to Newtonsoft's JsonConverter engine.

## v0.0.5-alpha5
* Fixed #8: NullReference exception when querying non-existent DB.

## v0.0.5-alpha4
* AST: added uuid(expr)
* Feature: Anonymous typed args. Example: getAll(...)[new {index = "foo"}].run()
* Fixed bugs in r.binary() when building AST.
* Fixed bugs in geometry deserialization.
* Fixed Cursor bug in cursor continuation.
* Binary unit tests passing.
* Times Constructors unit tests passing.
* DatumNumber unit tests passing.
* Default unit tests passing.
* DatumObject unit tests passing.
* Json unit tests passing.
* Geo constructors unit tests passing.
* GeoGeo json unit tests passing.
* GeoOperations unit tests passing.
* GeoPrimitives unit tests passing.
* Match unit tests passing.

## v0.0.5-alpha3
* Cursor support for sequence / partial results (example getAll).
* Make .run dynamic.
* Added faster .runCursor for queries expecting a cursor.

## v0.0.5-alpha2
* Implemented driver prefetching.
* Support for inserting POCO objects in tables.
* Support for retrieving POCO objects from tables.
* More accurate DateTime conversions.

## v0.0.5-alpha1
* Support for .NET Core / DNX Runtime.
* Support for Mono / Linux Runtime.
* Convert Func0 in AST.

## v0.0.4-alpha9
* Fixed bug in Reql Function lambda FUNCALL AST ordering.
* Best practice - avoid using C# "is" operator in Util.ToReqlAst.

## v0.0.4-alpha8
* AST Update
* More signatures for table.indexCreate.
* Allow JavaScript in places for ReqlFunction1.
* r.desc and r.desc can accept functions

## v0.0.4-alpha7
* Ensure latest Common.Logging is used.

## v0.0.4-alpha6
* Removed unnecessary dependency on extension methods utility.

## v0.0.4-alpha5
* Added a toplevel r.array and r.hashMap utility
* AST now using proper C# lambdas.
* More refined AST signatures.

## v0.0.4-alpha4
* PR#2: Fix null reference exception: JObject.FromObject before objects to JArray list. -cadabloom

## v0.0.4-alpha3
* Fixed recursion in MakeObject
* optArg now explicitly included where needed.

## v0.0.4-alpha2
* Added IntelliSense XML documentation.

## v0.0.4-alpha1
* Refined serialization.
* DB methods fixed.
* Create/Delete database working.
* RethinkDB PesudoType conversion working.

## v0.0.0.3
* Connection to RethinkDB working.

## v0.0.0.0:
* Initial port from Josh's Java Driver.
