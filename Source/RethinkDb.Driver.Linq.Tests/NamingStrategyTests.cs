using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Serialization;
using System;
using RethinkDb.Driver.Linq.Attributes;
using RethinkDb.Driver.Net;
using Newtonsoft.Json;

namespace RethinkDb.Driver.Linq.Tests
{
    [TestFixture]
    public class NamingStrategyTests : BaseLinqTest
    {
        [DatapointSource]
        public DefaultContractResolver[] ContractResolvers =
        {
            new DefaultContractResolver(),
            new CamelCasePropertyNamesContractResolver(),
            new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        readonly List<TestObject> data = new List<TestObject>
        {
            new TestObject
            {
                SimpleProperty = "Property value 1",
                IndexedProperty = "Indexed property value 1",
                SimpleField = 1,
            },
            new TestObject
            {
                SimpleProperty = "Property value 2",
                IndexedProperty = "Indexed property value 2",
                SimpleField = 2,
            }
        };
        
        [Theory]
        public void Where_RespectsConfiguredNamingStrategy(DefaultContractResolver contractResolver)
        {
            using (WithContractResolver(contractResolver))
            {
                SpawnData( data );
                
                var namingStrategy = contractResolver.NamingStrategy ?? new DefaultNamingStrategy();
    
                var expected = RethinkDB.R.Table( TableName )
                    .Filter( x => x[namingStrategy.GetPropertyName( nameof(TestObject.SimpleProperty), false )].Eq( data[0].SimpleProperty ) )
                    .Filter( x => x[namingStrategy.GetPropertyName( nameof(TestObject.SimpleField), false )].Gt( data[0].SimpleField ) );
    
                var queryable = GetQueryable<TestObject>( TableName, expected );
           
                var result =
                (
                    from testObject in queryable
                    where testObject.SimpleProperty == data[0].SimpleProperty
                    where testObject.SimpleField > data[0].SimpleField
                    select testObject
                ).ToList();

                Assert.NotNull(result);
            }
        }
        
        [Theory]
        public void WhereWithIndex_RespectsConfiguredNamingStrategy(DefaultContractResolver contractResolver)
        {
            using (WithContractResolver(contractResolver))
            {
                SpawnData( data );
                
                var namingStrategy = contractResolver.NamingStrategy ?? new DefaultNamingStrategy();
    
                var expected = RethinkDB.R.Table( TableName )
                    .GetAll( data[0].IndexedProperty )
                    .OptArg( "index", namingStrategy.GetPropertyName( nameof(TestObject.IndexedProperty), false ) );
    
                var queryable = GetQueryable<TestObject>( TableName, expected );
            
                var result =
                (
                    from testObject in queryable
                    where testObject.IndexedProperty == data[0].IndexedProperty
                    select testObject
                ).ToList();

                Assert.NotNull(result);
            }
        }
        
        [Theory]
        public void Orderby_RespectsConfiguredNamingStrategy(DefaultContractResolver contractResolver)
        {
            using (WithContractResolver(contractResolver))
            {
                SpawnData( data );
                
                var namingStrategy = contractResolver.NamingStrategy ?? new DefaultNamingStrategy();
    
                var expected = RethinkDB.R.Table( TableName )
                    .OrderBy( namingStrategy.GetPropertyName( nameof(TestObject.SimpleProperty), false ) );
    
                var queryable = GetQueryable<TestObject>( TableName, expected );
            
                var result =
                (
                    from testObject in queryable
                    orderby testObject.SimpleProperty
                    select testObject
                ).ToList();

                Assert.NotNull(result);
            }
        }
        
        [Theory]
        public void OrderbyField_RespectsConfiguredNamingStrategy(DefaultContractResolver contractResolver)
        {
            using (WithContractResolver(contractResolver))
            {
                SpawnData( data );
                
                var namingStrategy = contractResolver.NamingStrategy ?? new DefaultNamingStrategy();
    
                var expected = RethinkDB.R.Table( TableName )
                    .OrderBy( namingStrategy.GetPropertyName( nameof(TestObject.SimpleField), false ) );
    
                var queryable = GetQueryable<TestObject>( TableName, expected );
            
                var result =
                (
                    from testObject in queryable
                    orderby testObject.SimpleField
                    select testObject
                ).ToList();

                Assert.NotNull(result);
            }
        }
        
        [Theory]
        public void OrderbyWithIndex_RespectsConfiguredNamingStrategy(DefaultContractResolver contractResolver)
        {
            using (WithContractResolver(contractResolver))
            {
                SpawnData( data );
                
                var namingStrategy = contractResolver.NamingStrategy ?? new DefaultNamingStrategy();
    
                var expected = RethinkDB.R.Table( TableName )
                    .OrderBy( namingStrategy.GetPropertyName( nameof(TestObject.IndexedProperty), false ) )
                    .OptArg( "index", namingStrategy.GetPropertyName( nameof(TestObject.IndexedProperty), false ) );
    
                var queryable = GetQueryable<TestObject>( TableName, expected );
            
                var result =
                (
                    from testObject in queryable
                    orderby testObject.IndexedProperty
                    select testObject
                ).ToList();

                Assert.NotNull(result);
            }
        }
        
        [Theory]
        public void Groupby_RespectsConfiguredNamingStrategy(DefaultContractResolver contractResolver)
        {
            using (WithContractResolver(contractResolver))
            {
                SpawnData( data );
                
                var namingStrategy = contractResolver.NamingStrategy ?? new DefaultNamingStrategy();
    
                var expected = RethinkDB.R.Table( TableName )
                    .Group(namingStrategy.GetPropertyName( nameof(TestObject.SimpleProperty), false ) )
                    .Ungroup();
    
                var queryable = GetQueryable<TestObject>( TableName, expected );
            
                var result = queryable
                    .GroupBy(testObject => testObject.SimpleProperty)
                    .ToList();

                Assert.NotNull(result);
            }
        }
        
        [Theory]
        public void GroupbyField_RespectsConfiguredNamingStrategy(DefaultContractResolver contractResolver)
        {
            using (WithContractResolver(contractResolver))
            {
                SpawnData( data );
                
                var namingStrategy = contractResolver.NamingStrategy ?? new DefaultNamingStrategy();
    
                var expected = RethinkDB.R.Table( TableName )
                    .Group(namingStrategy.GetPropertyName( nameof(TestObject.SimpleField), false ) )
                    .Ungroup();
    
                var queryable = GetQueryable<TestObject>( TableName, expected );
            
                var result = queryable
                    .GroupBy(testObject => testObject.SimpleField)
                    .ToList();

                Assert.NotNull(result);
            }
        }
        
        [Theory]
        public void GroupbyWithSelector_RespectsConfiguredNamingStrategy(DefaultContractResolver contractResolver)
        {
            using (WithContractResolver(contractResolver))
            {
                SpawnData( data );
                
                var namingStrategy = contractResolver.NamingStrategy ?? new DefaultNamingStrategy();
    
                var expected = RethinkDB.R.Table( TableName )
                    .Group( namingStrategy.GetPropertyName( nameof(TestObject.SimpleProperty), false ) )
                    .GetField( namingStrategy.GetPropertyName( nameof(TestObject.SimpleField), false ) )
                    .Ungroup();
    
                var queryable = GetQueryable<TestObject>( TableName, expected );
            
                var result = 
                (
                    from testObject in queryable
                    group testObject.SimpleField by testObject.SimpleProperty into g
                    select g
                ).ToList();

                Assert.NotNull(result);
            }
        }
        
        static IDisposable WithContractResolver(IContractResolver contractResolver)
        {
            var original = Converter.Serializer;
            
            Converter.Serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                Converters = Converter.Converters,
                ContractResolver = contractResolver
            });
            
            return new Disposable(() => Converter.Serializer = original); 
        }

        class TestObject
        {
            public string SimpleProperty { get; set; }
            
            [SecondaryIndex]
            public string IndexedProperty { get; set; }

            public int SimpleField;
        }

        class Disposable : IDisposable
        {
            readonly Action action;

            public Disposable(Action action)
            {
                this.action = action;
            }

            public void Dispose()
            {
                action();
            }
        }
    }
}