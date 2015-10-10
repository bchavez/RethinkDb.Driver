using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Common;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using Z.ExtensionMethods;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class GeneratedTest
    {
        protected static int TestCounter = 0;
        protected const string DbName = "CSharpDriverTests";
        protected const string Hostname = "192.168.0.11";
        protected const int Port = RethinkDb.Driver.RethinkDBConstants.DEFAULT_PORT;

        protected static RethinkDB r = RethinkDB.r;
        protected Connection conn;

        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {

        }

        [TestFixtureTearDown]
        public void AfterRunningTestSession()
        {
            Console.WriteLine($"TOTAL TESTS: {TestCounter}");
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

        // Python test conversion compatibility definitions

        public class Arrays
        {
            public static IList asList(params object[] p)
            {
                return p.ToList();
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
            //expected.Equals(obtained).Should().BeTrue();


        }

        protected object runOrCatch(object query, OptArgs runopts)
        {
            if( query == null )
                return null;

            try
            {
                return ((ReqlAst)query).run(conn, runopts);
            }
            catch( Exception e)
            {
                return e;
            }
        }

        protected object wait_(int length)
        {
            Thread.Sleep(length * 1000);
            return null;
        }

        public int len(IList array)
        {
            return array.Count;
        }

        public class Lst
        {
            IList lst;
            public Lst(IList lst)
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

            public Bag(IList lst)
            {
                var newlist = lst.OfType<object>().ToList();
                newlist.Sort();
                this.lst = newlist;
            }

            public override bool Equals(object other)
            {
                if (!(other is IList)) {
                    return false;
                }
                var otherList = ((IList)other).OfType<object>().ToList();
                otherList.Sort();
                return lst.Equals(otherList);
            }
        }

        protected Bag bag(IList lst)
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
                if (!(other is IList)) {
                    return false;
                }
                var otherList = (IList)other;
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
        protected PartialLst partial(IList lst)
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

        protected ArrLen arrlen(double length, object thing)
        {
            return arrlen((int)length, thing);
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

            public IntCmp(double nbr)
            {
                this.nbr = (int)nbr;
            }

            public override bool Equals(Object other)
            {
                return nbr.Equals(other);
            }
        }

        protected IntCmp int_cmp(double nbr)
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

        protected double float_(double nbr)
        {
            return nbr;
        }

        protected FloatCmp float_cmp(Double nbr)
        {
            return new FloatCmp(nbr);
        }


        public class ErrRegex
        {
            public string clazz;
            public String message_rgx;

            public ErrRegex(String classname, String message_rgx)
            {
                this.clazz = classname;
                this.message_rgx = message_rgx;
            }

            public override bool Equals(Object other)
            {
                if (!(other is ErrRegex))
                {
                    return false;
                }
                var errRegex = other as ErrRegex;
                if( errRegex.clazz != this.clazz )
                    return false;

                return Regex.Match(message_rgx, errRegex.message_rgx).Success;
            }
        }

        protected ErrRegex err_regex(String classname, String message_rgx, object extra)
        {
            return new ErrRegex(classname, message_rgx);
        }

        protected ArrayList fetch(ReqlAst query, double values)
        {
            throw new NotImplementedException("Not implemented!");
        }

        public IEnumerable<int> range(int start, int stop)
        {
            return Enumerable.Range(start, stop);
        }
        public IEnumerable<int> range(double start, double stop)
        {
            return range((int)start, (int)stop);
        }

        protected ArrayList list(object str)
        {
            return null;
        }
        
    }
    
}