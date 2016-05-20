using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Remotion.Linq;
using Remotion.Linq.Clauses.ResultOperators;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Linq
{
    public class RethinkQueryExecutor : IQueryExecutor
    {
        private readonly Table table;
        private readonly IConnection connection;

        public RethinkQueryExecutor( Table table, IConnection connection )
        {
            this.table = table;
            this.connection = connection;
        }

        protected virtual void ProcessQuery( ReqlAst query )
        {
            
        }

        public T ExecuteScalar<T>( QueryModel queryModel )
        {
            var visitor = new RethinkDbQueryModelVisitor( table );

            visitor.VisitQueryModel( queryModel );

            var query = visitor.Query;
            ProcessQuery( query );

            var result = query.Run( connection );

            if( queryModel.ResultOperators.FirstOrDefault() is AnyResultOperator )
                return result > 0;
            if( queryModel.ResultOperators.FirstOrDefault() is AllResultOperator )
                return result == 0;

            return (T)result;
        }



        public T ExecuteSingle<T>( QueryModel queryModel, bool returnDefaultWhenEmpty )
        {
            var visitor = new RethinkDbQueryModelVisitor( table );

            visitor.VisitQueryModel( queryModel );

            var query = visitor.Query;
            ProcessQuery( query );

            try
            {
                return query.RunResult<T>( connection );
            }
            catch( ReqlNonExistenceError ex )
            {
                if( ShouldReturnDefault( queryModel ) )
                    return default( T );
                throw new InvalidOperationException( ex.Message );
            }
        }

        private static bool ShouldReturnDefault( QueryModel queryModel )
        {
            var firstResultOperator = queryModel.ResultOperators.FirstOrDefault();
            return ( firstResultOperator as FirstResultOperator )?.ReturnDefaultWhenEmpty
                   ?? ( firstResultOperator as LastResultOperator )?.ReturnDefaultWhenEmpty
                   ?? false;
        }

        public IEnumerable<T> ExecuteCollection<T>( QueryModel queryModel )
        {
            var visitor = new RethinkDbQueryModelVisitor( table );

            visitor.VisitQueryModel( queryModel );

            var query = visitor.Query;
            ProcessQuery( query );

            if( typeof(T).GetTypeInfo().IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IGrouping<,>) )
            {

                return typeof(RethinkQueryExecutor)
                    .GetMethod(nameof(DeserializeGrouping),
                        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .MakeGenericMethod(
                        typeof(T).GetGenericArguments()[0],
                        typeof(T).GetGenericArguments()[1])
                    .Invoke(null, new object[]
                                  {
                                      query.Run(connection) as JArray
                                  }) as IEnumerable<T>;
            }

            if( query is Get )
            {
                return new List<T>
                       {
                           query.RunResult<T>(connection)
                       };
            }

            return query.RunResult<List<T>>( connection );
        }

        private static List<IGrouping<T, TVal>> DeserializeGrouping<T, TVal>( JArray groups )
        {
            var result = new List<IGrouping<T, TVal>>();

            foreach( var group in groups )
            {
                var groupArray = (JObject)group;
                result.Add( new RethinkDbGroup<T, TVal>
                {
                    Key = groupArray["group"].ToObject<T>(),
                    Reduction = JsonConvert.DeserializeObject<List<TVal>>( groupArray["reduction"].ToString() )
                } );
            }

            return result;
        }
    }
}