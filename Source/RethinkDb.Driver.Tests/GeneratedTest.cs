using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class GeneratedTest
    {
        protected const string DbName = "CSharpDriverTests";
        protected const string Hostname = "192.168.0.11";
        protected const int Port = 31157;

        protected static RethinkDB r = RethinkDB.r;
        protected Connection conn;

        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {

        }

        [TestFixtureTearDown]
        public void AfterRunningTestSession()
        {

        }


        [SetUp]
        public void BeforeEachTest()
        {
            conn = r.connection()
                .hostname(Hostname)
                .port(Port)
                .connect();
        }

        [TearDown]
        public void AfterEachTest()
        {

        }


        [Test]
        public void Test()
        {
            
        }

        public class Arrays
        {
            public static ArrayList asList(params object[] p)
            {
                var list = new ArrayList();
                foreach( var o in p )
                {
                    list.Add(o);
                }
                return list;
            }
        }

        public class Err
        {
            private string errorMessage;
            private string errorType;
            private object obj;

            public Err(string errorType, string errorMessage, object obj)
            {
                this.errorType = errorType;
                this.errorMessage = errorMessage;
                this.obj = obj;
            }
        }

        protected Err err(string errorType, string errorMessage)
        {
            return new Err(errorType, errorMessage, null);
        }
        protected Err err(string errorType, string errorMessage, object obj)
        {
            return new Err(errorType, errorMessage, obj);
        }

        protected void assertEquals(object expected, object obtained)
        {
            
        }

        protected object runOrCatch(object query, OptArgs runopts)
        {
            try
            {
                return ((ReqlAst)query).run(conn, runopts);
            }
            catch( Exception e)
            {
                return e;
            }
        }

        public int len(ArrayList array)
        {
            return array.Count;
        }

        public class Lst
        {
            ArrayList lst;
            public Lst(ArrayList lst)
            {
                this.lst = lst;
            }

            public override bool Equals(Object other)
            {
                return lst.Equals(other);
            }
        }

        public class Bag
        {
            IList lst;

            public Bag(ArrayList lst)
            {
                lst.Sort();
                this.lst = lst;
            }

            public override bool Equals(object other)
            {
                if (!(other is List)) {
                    return false;
                }
                ArrayList otherList = (ArrayList)other;
                otherList.Sort();
                return lst.Equals(otherList);
            }
        }

        protected Bag bag(ArrayList lst)
        {
            return new Bag(lst);
        }

        public class Partial
        {
        }

        public class PartialLst : Partial
        {
            IList lst;
            public PartialLst(IList lst){
                this.lst = lst;
            }

            public override bool Equals(Object other) {
                if (!(other is ArrayList)) {
                    return false;
                }
                ArrayList otherList = (ArrayList)other;
                if (lst.Count > otherList.Count)
                {
                    return false;
                }
                foreach (var item in lst)
                {
                    if (otherList.IndexOf(item) == -1)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        protected PartialLst partial(ArrayList lst)
        {
            return new PartialLst(lst);
        }

        public class PartialDct : Partial
        {
            IDictionary dct;
            public PartialDct(IDictionary dct)
            {
                this.dct = dct;
            }

            public override bool Equals(Object other)
            {
                if (!(other is IDictionary)) {
                    return false;
                }
                var otherDict = ((IDictionary)other);

                return otherDict.Keys.OfType<object>()
                    .Except(this.dct.Keys.OfType<object>())
                    .Any()
                       ||
                       otherDict.Values.OfType<object>()
                           .Except(this.dct.Values.OfType<object>())
                           .Any();
            }
        }
        protected PartialDct partial(MapObject dct)
        {
            return new PartialDct(dct);
        }



        public class ArrLen
        {
            int length;
            Object thing;
            public ArrLen(int length, Object thing)
            {
                this.length = length;
                this.thing = thing;
            }

            public override bool Equals(Object other)
            {
                if (!(other is IList)){
                    return false;
                }
                var otherList = (IList)other;
                if (length != otherList.Count)
                {
                    return false;
                }
                if (thing == null)
                {
                    return true;
                }
                foreach (var item in otherList)
                {
                    if (!item.Equals(thing))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        protected ArrLen arrlen(int length, Object thing)
        {
            return new ArrLen(length, thing);
        }



        public class Uuid
        {
            public override bool Equals(Object other)
            {
                if (!(other is String)) {
                    return false;
                }

                Guid val;
                return Guid.TryParse(other as string, out val);
            }
        }

        protected Uuid uuid()
        {
            return new Uuid();
        }


        public class IntCmp
        {
            private int nbr;

            public IntCmp(int nbr)
            {
                this.nbr = nbr;
            }

            public override bool Equals(Object other)
            {
                return nbr.Equals(other);
            }
        }

        protected IntCmp int_cmp(int nbr)
        {
            return new IntCmp(nbr);
        }

        public class FloatCmp
        {
            private Double nbr;

            public FloatCmp(Double nbr)
            {
                this.nbr = nbr;
            }

            public override bool Equals(Object other)
            {
                return nbr.Equals(other);
            }
        }

        protected FloatCmp float_cmp(Double nbr)
        {
            return new FloatCmp(nbr);
        }

       
    }
}