using System;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Proto;
using Z.ExtensionMethods;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class TestDebugger
    {
        //crazy i know right... :)
        private static Regex replacer = new Regex(@"(?<=\[)\d+(?!\d*])");
        
        [Test]
        public void Test()
        {
            //C#
            var q1 = @"[1,[64,[[69,[[2,[21]],[67,[[20,[[10,[21]],-1.7976931348623157E+308]],[21,[[10,[21]],-1.7976931348623157E+308]]]]]],[151,[-1.7976931348623157E+308,-1.7976931348623157E+308],{""float"":true}]]]]";
            //Java
            var q2 = @"[1,[56,[[15,[[14,[""CSharpDriverTests""]],""TableA""]],""{'Bar':1,'Jam':2}""]]]";



            q1 = FixUp(q1);
            q2 = FixUp(q2);

            q1 = replacer.Replace(q1, m =>
                {
                    TermType termType;
                    if( Enum.TryParse(m.Value, out termType) )
                    {
                        return m.Result(termType.ToString());
                    }
                    return m.Value;
                });

            Console.WriteLine("Q1:");
            Console.WriteLine(q1);

            q2 = replacer.Replace(q2, m =>
            {
                TermType termType;
                if (Enum.TryParse(m.Value, out termType))
                {
                    return m.Result(termType.ToString());
                }
                return m.Value;
            });

            Console.WriteLine("Q2:");
            Console.WriteLine(q2);

            q1.Should().Be(q2);
        }

        private string FixUp(string q)
        {
            if( q.StartsWith("[1,") )
                return q.Substring(3, q.Length - 4 );
            return q;
        }
    }
}
