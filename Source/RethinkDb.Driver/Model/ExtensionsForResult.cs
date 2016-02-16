namespace RethinkDb.Driver.Model
{
    /// <summary>
    /// Extension method Result helpers.
    /// </summary>
    public static class ExtensionsForResult
    {
        /// <summary>
        /// Ensures no errors occurs during the processing of the result.
        /// </summary>
        /// <exception cref="ReqlAssertFailure">Thrown when the assertion applies</exception>
        public static Result AssertNoErrors(this Result result)
        {
            if( result.Errors > 0 )
            {
                throw new ReqlAssertFailure(result.FirstError);
            }
            return result;
        }

        /// <summary>
        /// Ensures a number of deleted documents.
        /// </summary>
        /// <exception cref="ReqlAssertFailure">Thrown when the assertion applies</exception>
        public static Result AssertDeleted(this Result result, ulong deleted)
        {
            if( result.Deleted != deleted )
            {
                throw AssertFail(nameof(AssertDeleted), result.Deleted, deleted);
            }

            return result;
        }

        /// <summary>
        /// Ensures a number of inserted documents.
        /// </summary>
        /// <exception cref="ReqlAssertFailure">Thrown when the assertion applies</exception>
        public static Result AssertInserted(this Result result, ulong inserted)
        {
            if( result.Inserted != inserted )
            {
                throw AssertFail(nameof(AssertInserted), result.Inserted, inserted);
            }

            return result;
        }

        /// <summary>
        /// Ensures a number of replaced documents.
        /// </summary>
        /// <exception cref="ReqlAssertFailure">Thrown when the assertion applies</exception>
        public static Result AssertReplaced(this Result result, ulong replaced)
        {
            if( result.Replaced != replaced )
            {
                throw AssertFail(nameof(AssertReplaced), result.Replaced, replaced);
            }

            return result;
        }

        /// <summary>
        /// Ensures a number of skipped documents.
        /// </summary>
        /// <exception cref="ReqlAssertFailure">Thrown when the assertion applies</exception>
        public static Result AssertSkipped(this Result result, ulong skipped)
        {
            if( result.Skipped != skipped )
            {
                throw AssertFail(nameof(AssertSkipped), result.Skipped, skipped);
            }

            return result;
        }

        /// <summary>
        /// Ensures a number of unchanged documents.
        /// </summary>
        /// <exception cref="ReqlAssertFailure">Thrown when the assertion applies</exception>
        public static Result AssertUnchanged(this Result result, ulong unchanged)
        {
            if( result.Unchanged != unchanged )
            {
                throw AssertFail(nameof(AssertUnchanged), result.Unchanged, unchanged);
            }

            return result;
        }

        /// <summary>
        /// Ensures a number of tables created.
        /// </summary>
        /// <exception cref="ReqlAssertFailure">Thrown when the assertion applies</exception>
        public static Result AssertTablesCreated(this Result result, ulong tablesCreated)
        {
            if( result.TablesCreated != tablesCreated )
            {
                throw AssertFail(nameof(AssertTablesCreated), result.TablesCreated, tablesCreated);
            }

            return result;
        }

        /// <summary>
        /// Ensures a number of tables deleted.
        /// </summary>
        /// <exception cref="ReqlAssertFailure">Thrown when the assertion applies</exception>
        public static Result AssertTablesDropped(this Result result, ulong tablesDropped)
        {
            if( result.TablesDropped != tablesDropped )
            {
                throw AssertFail(nameof(AssertTablesDropped), result.TablesDropped, tablesDropped);
            }

            return result;
        }

        /// <summary>
        /// Ensures a number of databases created.
        /// </summary>
        /// <exception cref="ReqlAssertFailure">Thrown when the assertion applies</exception>
        public static Result AssertDatabasesCreated(this Result result, ulong databasesCreated)
        {
            if( result.DatabasesCreated != databasesCreated )
            {
                throw AssertFail(nameof(AssertDatabasesCreated), result.DatabasesCreated, databasesCreated);
            }

            return result;
        }


        /// <summary>
        /// Ensures a number of databases deleted.
        /// </summary>
        /// <exception cref="ReqlAssertFailure">Thrown when the assertion applies</exception>
        public static Result AssertDatabasesDropped(this Result result, ulong databasesDropped)
        {
            if( result.DatabasesDropped != databasesDropped )
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