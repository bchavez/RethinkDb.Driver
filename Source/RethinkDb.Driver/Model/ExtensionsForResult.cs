namespace RethinkDb.Driver.Model
{
    public static class ExtensionsForResult
    {
        public static Result AssertNoErrors(this Result result)
        {
            if( result.Errors > 0 )
            {
                throw new ReqlAssertFailure(result.FirstError);
            }
            return result;
        }

        public static Result AssertDeleted(this Result result, ulong deleted)
        {
            if( result.Deleted != deleted )
            {
                throw AssertFail(nameof(AssertDeleted), result.Deleted, deleted);
            }

            return result;
        }

        public static Result AssertInserted(this Result result, ulong inserted)
        {
            if( result.Inserted != inserted )
            {
                throw AssertFail(nameof(AssertInserted), result.Inserted, inserted);
            }

            return result;
        }

        public static Result AssertReplaced(this Result result, ulong replaced)
        {
            if( result.Replaced != replaced )
            {
                throw AssertFail(nameof(AssertReplaced), result.Replaced, replaced);
            }

            return result;
        }

        public static Result AssertSkipped(this Result result, ulong skipped)
        {
            if (result.Skipped != skipped)
            {
                throw AssertFail(nameof(AssertSkipped), result.Skipped, skipped);
            }

            return result;
        }

        public static Result AssertUnchanged(this Result result, ulong unchanged)
        {
            if (result.Unchanged != unchanged)
            {
                throw AssertFail(nameof(AssertUnchanged), result.Unchanged, unchanged);
            }

            return result;
        }

        public static Result AssertTablesCreated(this Result result, ulong tablesCreated)
        {
            if (result.TablesCreated != tablesCreated)
            {
                throw AssertFail(nameof(AssertTablesCreated), result.TablesCreated, tablesCreated);
            }

            return result;
        }

        public static Result AssertTablesDropped(this Result result, ulong tablesDropped)
        {
            if (result.TablesDropped != tablesDropped)
            {
                throw AssertFail(nameof(AssertTablesDropped), result.TablesDropped, tablesDropped);
            }

            return result;
        }

        public static Result AssertDatabasesCreated(this Result result, ulong databasesCreated)
        {
            if (result.DatabasesCreated != databasesCreated)
            {
                throw AssertFail(nameof(AssertDatabasesCreated), result.DatabasesCreated, databasesCreated);
            }

            return result;
        }

        public static Result AssertDatabasesDropped(this Result result, ulong databasesDropped)
        {
            if (result.DatabasesDropped != databasesDropped)
            {
                throw AssertFail(nameof(AssertDatabasesDropped), result.DatabasesDropped, databasesDropped);
            }

            return result;
        }

        private static ReqlAssertFailure AssertFail(string op, ulong got, ulong expected)
        {
            return new ReqlAssertFailure($"The result was {got}, but {op} expected {expected}");
        }

    }
}