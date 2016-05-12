using System.Linq;
using Remotion.Linq;
using Remotion.Linq.Clauses.ResultOperators;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers.SubQueryVisitor
{
    public class FirstAndLastSubQueryVisitor : BaseFilterableSubQueryVisitor<ChoiceResultOperatorBase>
    {
        public override bool CanVisit( QueryModel queryModel )
        {
            return queryModel.ResultOperators.Any( x => x is FirstResultOperator || x is LastResultOperator );
        }

        protected override ReqlExpr BuildReql( ReqlExpr reqlExpr, QueryModel queryModel )
        {
            reqlExpr = reqlExpr.Nth( queryModel.ResultOperators[0] is FirstResultOperator ? 0 : -1 );
            var resultOperator = (ChoiceResultOperatorBase)queryModel.ResultOperators[0];
            if( resultOperator.ReturnDefaultWhenEmpty )
                reqlExpr = reqlExpr.Default_( (object)null );
            return reqlExpr;
        }
    }
}