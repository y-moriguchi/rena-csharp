/**
 * This source code is under the Unlicense. 
 */
using NUnit.Framework;
using System;

namespace Morilib
{
    public class RenaTest
    {
        private void Match(Func<string, int, int, Rena<int>.Result> exp, string toMatch, string matched, int index)
        {
            Rena<int>.Result result = exp(toMatch, 0, default(int));
            Assert.AreEqual(matched, result.Match);
            Assert.AreEqual(index, result.LastIndex);
        }

        private void Match(Func<string, int, int, Rena<int>.Result> exp, string toMatch, int startIndex, string matched, int index)
        {
            Rena<int>.Result result = exp(toMatch, startIndex, default(int));
            Assert.AreEqual(matched, result.Match);
            Assert.AreEqual(index, result.LastIndex);
        }

        private void MatchAttr(Func<string, int, int, Rena<int>.Result> exp, string toMatch, string matched, int index, int initAttr, int attr)
        {
            Rena<int>.Result result = exp(toMatch, 0, initAttr);
            Assert.AreEqual(matched, result.Match);
            Assert.AreEqual(index, result.LastIndex);
            Assert.AreEqual(attr, result.Attr);
        }

        private void NoMatch(Func<string, int, int, Rena<int>.Result> exp, string toMatch)
        {
            Rena<int>.Result result = exp(toMatch, 0, default(int));
            Assert.IsNull(result);
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestStr()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Str("abcd");

            Match(matcher, "abcde", "abcd", 4);
            NoMatch(matcher, "abcx");
            NoMatch(matcher, "abc");
            NoMatch(matcher, "");
        }

        [Test]
        public void TestRE()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.RE("[a-z]+");

            Match(matcher, "abcde", "abcde", 5);
            Match(matcher, "abcx1", "abcx", 4);
            NoMatch(matcher, "123");
            NoMatch(matcher, "");
        }

        [Test]
        public void TestIsEnd()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.IsEnd();

            Match(matcher, "a", 1, "", 1);
            NoMatch(matcher, "123");
        }

        [Test]
        public void TestAction()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Action(r.Action(r.RE("[0-9]+"), (match, syn, inh) => int.Parse(match)), (match, syn, inh) => inh - syn);

            MatchAttr(matcher, "27", "27", 2, 28, 1);
        }

        [Test]
        public void TestConcat()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Concat(r.Str("a"), r.Str("c"), r.Str("e"));

            Match(matcher, "ace", "ace", 3);
            NoMatch(matcher, "bce");
            NoMatch(matcher, "ade");
            NoMatch(matcher, "acf");
            NoMatch(matcher, "cea");
            NoMatch(matcher, "ac");
            NoMatch(matcher, "");
        }

        [Test]
        public void TestChoice()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Choice(r.Str("a"), r.Str("c"), r.Str("e"));

            Match(matcher, "ace", "a", 1);
            Match(matcher, "ce", "c", 1);
            Match(matcher, "e", "e", 1);
            NoMatch(matcher, "b");
            NoMatch(matcher, "");
        }

        [Test]
        public void TestLookaheadNot()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Concat(r.LookaheadNot(r.Str("ab")), r.RE("[a-z]+"));

            Match(matcher, "accde", "accde", 5);
            NoMatch(matcher, "abcde");
        }

        [Test]
        public void TestLetrec1()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Letrec1(x => r.Choice(r.Concat(r.Str("("), x, r.Str(")")), r.Str("")));

            Match(matcher, "((()))", "((()))", 6);
            Match(matcher, "((())))", "((()))", 6);
            Match(matcher, "((())", "", 0);
        }

        [Test]
        public void TestLetrec2()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Letrec2(
                (x, y) => r.Choice(r.Concat(r.Str("("), y, r.Str(")")), r.Str("")),
                (x, y) => r.Concat(r.Str("["), x, r.Str("]")));

            Match(matcher, "([([])])", "([([])])", 8);
            Match(matcher, "([([])]", "", 0);
        }

        [Test]
        public void TestLetrec3()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Letrec3(
                (x, y, z) => r.Choice(r.Concat(r.Str("("), y, r.Str(")")), r.Str("")),
                (x, y, z) => r.Concat(r.Str("["), z, r.Str("]")),
                (x, y, z) => r.Concat(r.Str("{"), x, r.Str("}")));

            Match(matcher, "([{([{}])}])", "([{([{}])}])", 12);
            Match(matcher, "([{([{}])}]", "", 0);
        }

        [Test]
        public void TestZeroOrMore()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.ZeroOrMore(r.Str("a"));

            Match(matcher, "aaaa", "aaaa", 4);
            Match(matcher, "a", "a", 1);
            Match(matcher, "", "", 0);
        }

        [Test]
        public void TestOneOrMore()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.OneOrMore(r.Str("a"));

            Match(matcher, "aaaa", "aaaa", 4);
            Match(matcher, "a", "a", 1);
            NoMatch(matcher, "");
        }

        [Test]
        public void TestOpt()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Opt(r.Str("a"));

            Match(matcher, "aaaa", "a", 1);
            Match(matcher, "a", "a", 1);
            Match(matcher, "", "", 0);
        }

        [Test]
        public void TestLookahead()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Concat(r.Lookahead(r.Str("ab")), r.RE("[a-z]+"));

            Match(matcher, "abcde", "abcde", 5);
            NoMatch(matcher, "accde");
        }

        [Test]
        public void TestAttr()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.Attr(27);

            MatchAttr(matcher, "27", "", 0, 0, 27);
        }

        [Test]
        public void TestKey()
        {
            Rena<int> r = Rena<int>.Keys("+", "++", "--");
            var matcher = r.Key("+");

            Match(matcher, "+", "+", 1);
            Match(matcher, "+1", "+", 1);
            NoMatch(matcher, "++");
        }

        [Test]
        public void TestNotKey()
        {
            Rena<int> r = Rena<int>.Keys("+", "++", "--");
            var matcher = r.NotKey();

            Match(matcher, "-", "", 0);
            NoMatch(matcher, "++");
            NoMatch(matcher, "--");
            NoMatch(matcher, "+");
        }

        [Test]
        public void TestEqualsId1()
        {
            Rena<int> r = Rena<int>.GetInstance();
            var matcher = r.EqualsId("key");

            Match(matcher, "key", "key", 3);
            Match(matcher, "key+", "key", 3);
            Match(matcher, "key ", "key", 3);
            Match(matcher, "keys", "key", 3);
            NoMatch(matcher, "not");
        }

        [Test]
        public void TestEqualsId2()
        {
            Rena<int> r = Rena<int>.Keys("+", "++", "--");
            var matcher = r.EqualsId("key");

            Match(matcher, "key", "key", 3);
            Match(matcher, "key+", "key", 3);
            NoMatch(matcher, "key ");
            NoMatch(matcher, "keys");
            NoMatch(matcher, "not");
        }

        [Test]
        public void TestEqualsId3()
        {
            Rena<int> r = Rena<int>.Ignore("\\s+");
            var matcher = r.EqualsId("key");

            Match(matcher, "key", "key", 3);
            NoMatch(matcher, "key+");
            Match(matcher, "key ", "key", 3);
            NoMatch(matcher, "keys");
            NoMatch(matcher, "not");
        }

        [Test]
        public void TestEqualsId4()
        {
            Rena<int> r = Rena<int>.IgnoreAndKeys("\\s+", "+", "++", "--");
            var matcher = r.EqualsId("key");

            Match(matcher, "key", "key", 3);
            Match(matcher, "key+", "key", 3);
            Match(matcher, "key ", "key", 3);
            NoMatch(matcher, "keys");
            NoMatch(matcher, "not");
        }

        [Test]
        public void TestSkipSpace()
        {
            Rena<int> r = Rena<int>.Ignore("\\s+");
            var matcher = r.ZeroOrMore(r.RE("[a-z]+"));

            Match(matcher, "aa aa  aa", "aa aa  aa", 9);
            Match(matcher, "a", "a", 1);
        }
    }
}