using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class StackOverflowQuestions : QueryTestFixture
    {
        //https://stackoverflow.com/questions/34818423/how-to-updated-nested-lists-in-rethinkdb-using-net
        [Test]
        public void question_34818423()
        {
            ClearTable(DbName, TableName);

            var result = r.db(DbName).table(TableName)
                .insert(new
                    {
                        students = new
                            {
                                locations = new[]
                                    {
                                        "Los Angeles", "Huston", "Orlando"
                                    }
                            }
                    })
                    .runResult(conn);

            var insertedId = result.GeneratedKeys[0];

            var obj = r.db(DbName).table(TableName)
                .get(insertedId)
                .runAtom<JObject>(conn);

            obj.Dump();

            r.db(DbName).table(TableName)
                .get(insertedId)
                .update(new
                    {
                        students = new
                            {
                                locations = new[] { "Seattle", "San Francisco" }
                            }
                    })
                .runResult(conn);


            var newObj = r.db(DbName).table(TableName)
                .get(insertedId)
                .runAtom<JObject>(conn);

            newObj.Dump();
        }
         
    }
}