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

            var result = R.Db(DbName).Table(TableName)
                .Insert(new
                    {
                        students = new
                            {
                                locations = new[]
                                    {
                                        "Los Angeles", "Huston", "Orlando"
                                    }
                            }
                    })
                    .RunWrite(conn);

            var insertedId = result.GeneratedKeys[0];

            var obj = R.Db(DbName).Table(TableName)
                .Get(insertedId)
                .RunAtom<JObject>(conn);

            obj.Dump();

            R.Db(DbName).Table(TableName)
                .Get(insertedId)
                .Update(new
                    {
                        students = new
                            {
                                locations = new[] { "Seattle", "San Francisco" }
                            }
                    })
                .RunWrite(conn);


            var newObj = R.Db(DbName).Table(TableName)
                .Get(insertedId)
                .RunAtom<JObject>(conn);

            newObj.Dump();
        }
         
    }
}
