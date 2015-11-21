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
                throw new ReqlAssertFailure($"Deleted {result.Deleted} but expected {deleted}");
            }

            return result;
        }

        public static Result AssertInserted(this Result result, ulong inserted)
        {
            if( result.Inserted != inserted )
            {
                throw new ReqlAssertFailure($"Deleted {result.Inserted} but expected {inserted}");
            }

            return result;
        }

        public static Result AssertReplaced(this Result result, ulong replaced)
        {
            if( result.Replaced != replaced )
            {
                throw new ReqlAssertFailure($"Replaced {result.Replaced} but expected {replaced}");
            }

            return result;
        }

        public static Result AssertSkipped(this Result result, ulong skipped)
        {
            if (result.Skipped != skipped)
            {
                throw new ReqlAssertFailure($"Replaced {result.Skipped} but expected {skipped}");
            }

            return result;
        }

        public static Result AssertUnchanged(this Result result, ulong unchanged)
        {
            if (result.Unchanged != unchanged)
            {
                throw new ReqlAssertFailure($"Replaced {result.Unchanged} but expected {unchanged}");
            }

            return result;
        }
    }
}