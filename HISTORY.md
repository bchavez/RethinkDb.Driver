## v0.0.7-alpha1
* Test

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