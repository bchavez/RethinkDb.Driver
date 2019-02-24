using System;
using System.Threading;
using Bogus;
using NUnit.Framework;

namespace RethinkDb.Driver.Tests.Network
{
    public class TestDoc
    {
        public int Counter { get; set; }
        public string SomeString { get; set; }
        public DateTime SomeDate { get; set; }
        public DateTimeOffset SomeDateOffset { get; set; }
        public string SomeOtherString { get; set; }
    }

    [TestFixture]
    [Explicit]
    public class Benchmark : QueryTestFixture
    {
        private Faker<TestDoc> docs;

        public Benchmark()
        {
            Bogus.Randomizer.Seed = new Random(1337);
        }

        [SetUp]
        public void BeforeEachBenchmark()
        {
            var counter = 1;

            this.docs = new Faker<TestDoc>()
                .RuleFor(x => x.Counter, f => Interlocked.Increment(ref counter))
                .RuleFor(x => x.SomeString, f => f.Lorem.Sentence())
                .RuleFor(x => x.SomeDate, f => f.Date.Recent())
                .RuleFor(x => x.SomeOtherString, f => f.Lorem.Sentence())
                .RuleFor(x => x.SomeDateOffset, f => f.Date.Future());
        }

        [Test]
        public void many_write()
        {
            var inserts = 10000;

            for( var i = 0; i < inserts; i++ )
            {
                R.db(DbName).table(TableName)
                    .insert(docs.Generate())
                    .RunWrite(conn);
            }
            

        }

        [Test]
        public void read_all()
        {
            var cursor = R.db(DbName).table(TableName)
                .RunCursor<TestDoc>(conn);

            foreach( var doc in cursor )
            {
                
            }
        }
    }
}