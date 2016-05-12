using System;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Linq.Tests
{
    public class TestRethinkQueryExecutor : RethinkQueryExecutor
    {
        private readonly Action<ReqlAst> _validate;

        public TestRethinkQueryExecutor( Table table, IConnection connection, Action<ReqlAst> validate ) : base( table, connection )
        {
            _validate = validate;
        }

        protected override void ProcessQuery( ReqlAst query )
        {
            _validate( query );
        }
    }
}