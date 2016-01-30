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
