using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;

namespace Benchmark
{
    [RPlotExporter]
    public class BenchReqlAst
    {
        RethinkDB R = RethinkDB.R;
        
        [Benchmark]
        public void CSharp7()
        {
            R.Db("Test")
                .Table("Foo")
                .Insert(new
                    {
                        someValue = "fff",
                        someObject = new
                            {
                                someNested = 25,
                                someNested2 = false
                            },
                        someOtherProp = Guid.Parse("E111E216-EBD6-47CE-B2DE-CE5F60E2FA8F")
                    })
                    .Filter( x => x["someRow"] == 25)
                    .Filter( x => x["someOtherRow"].Gt(44))
                    .Delete()
                    .Build();
        }

        [Benchmark]
        public void CSharp6()
        {
            R.Db("Test")
                .Table("Foo")
                .Insert(new
                    {
                        someValue = "fff",
                        someObject = new
                            {
                                someNested = 25,
                                someNested2 = false
                            },
                        someOtherProp = Guid.Parse("E111E216-EBD6-47CE-B2DE-CE5F60E2FA8F")
                    })
                .Filter(x => x["someRow"] == 25)
                .Filter(x => x["someOtherRow"].Gt(44))
                .Delete()
                .Build();
        }
    }
}