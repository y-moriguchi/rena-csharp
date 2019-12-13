using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Morilib
{
    public class Rena<T>
    {
        private static readonly Rena<T> instanceRena = new Rena<T>();

        private readonly Func<string, int, T, Result> ignore;
        private readonly List<string> keys;

        /// <summary>
        /// Create expression generator.
        /// </summary>
        /// <param name="ignore">expression to ignore</param>
        /// <param name="keys">keywords (operators)</param>
        public Rena(Func<string, int, T, Result> ignore, string[] keys)
        {
            this.ignore = ignore;
            this.keys = keys == null ? null : new List<string>(keys);
        }

        private Rena(string ignore, string[] keys)
        {
            this.ignore = RE(ignore);
            this.keys = keys == null ? null : new List<string>(keys);
        }

        private Rena()
        {
            ignore = null;
            keys = null;
        }

        /// <summary>
        /// Create exression generator.
        /// </summary>
        /// <param name="regex">regex pattern to ignore</param>
        /// <param name="keys">keywords (operators)</param>
        /// <returns>exression generator</returns>
        public static Rena<T> IgnoreAndKeys(string regex, params string[] keys)
        {
            return new Rena<T>(regex, keys);
        }

        /// <summary>
        /// Create exression generator.
        /// </summary>
        /// <param name="regex">regex pattern to ignore</param>
        /// <returns>exression generator</returns>
        public static Rena<T> Ignore(string regex)
        {
            return new Rena<T>(regex, null);
        }

        /// <summary>
        /// Create exression generator.
        /// </summary>
        /// <param name="keys">keywords (operators)</param>
        /// <returns>exression generator</returns>
        public static Rena<T> Keys(params string[] keys)
        {
            return new Rena<T>((Func<string, int, T, Result>)null, keys);
        }

        /// <summary>
        /// Get expression generator without ignoring and keywords.
        /// </summary>
        /// <returns>exression generator</returns>
        public static Rena<T> GetInstance()
        {
            return instanceRena;
        }

        private int SkipSpace(string match, int index)
        {
            Result result;

            if(ignore == null || (result = ignore(match, index, default(T))) == null)
            {
                return index;
            }
            else
            {
                return result.LastIndex;
            }
        }

        /// <summary>
        /// A class which has a matching result.
        /// </summary>
        public class Result
        {
            internal Result(string match, int lastIndex, T attr)
            {
                Match = match;
                LastIndex = lastIndex;
                Attr = attr;
            }

            /// <summary>
            /// matching string
            /// </summary>
            public string Match { get; private set; }

            /// <summary>
            /// last index of matching
            /// </summary>
            public int LastIndex { get; private set; }

            /// <summary>
            /// result attribute
            /// </summary>
            public T Attr { get; private set; }
        }

        /// <summary>
        /// generates expression to match a string
        /// </summary>
        /// <param name="toMatch">string to match</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> Str(string toMatch)
        {
            return (match, index, attr) =>
            {
                if(index + toMatch.Length <= match.Length && toMatch == match.Substring(index, toMatch.Length))
                {
                    return new Result(toMatch, index + toMatch.Length, attr);
                }
                else
                {
                    return null;
                }
            };
        }

        /// <summary>
        /// generates expression to match to a pattern
        /// </summary>
        /// <param name="toMatch">pattern to match</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> RE(string regex)
        {
            Regex pattern = new Regex(regex);

            return (match, index, attr) =>
            {
                string toMatch = match.Substring(index);
                Match matched = pattern.Match(toMatch);

                if (matched.Success && matched.Index == 0)
                {
                    return new Result(match.Substring(index, matched.Length), index + matched.Length, attr);
                }
                else
                {
                    return null;
                }
            };
        }

        /// <summary>
        /// generates expression to match end of input
        /// </summary>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> IsEnd()
        {
            return (match, index, attr) => index >= match.Length ? new Result("", index, attr) : null;
        }

        /// <summary>
        /// generates expression to affect to attribute
        /// </summary>
        /// <param name="exp">enclosed expression</param>
        /// <param name="action">action</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> Action(Func<string, int, T, Result> exp, Func<string, T, T, T> action)
        {
            return (match, index, attr) =>
            {
                Result result = exp(match, index, attr);

                if(result == null)
                {
                    return null;
                }
                else
                {
                    return new Result(result.Match, result.LastIndex, action(result.Match, result.Attr, attr));
                }
            };
        }

        private Func<string, int, T, Result> Concat(Func<string, int, int> skipSpace, params Func<string, int, T, Result>[] exps)
        {
            return (match, index, attr) =>
            {
                int indexNew = index;
                T attrNew = attr;

                foreach (Func<string, int, T, Result> exp in exps)
                {
                    Result result = exp(match, indexNew, attrNew);

                    if (result == null)
                    {
                        return null;
                    }
                    else
                    {
                        indexNew = skipSpace(match, result.LastIndex);
                        attrNew = result.Attr;
                    }
                }
                return new Result(match.Substring(index, indexNew - index), indexNew, attrNew);
            };
        }

        /// <summary>
        /// generates expression to match a sequence of expression
        /// </summary>
        /// <param name="exps">sequence of expression</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> Concat(params Func<string, int, T, Result>[] exps)
        {
            return Concat(SkipSpace, exps);
        }

        /// <summary>
        /// generates expression to choice a expression from the arguments.
        /// </summary>
        /// <param name="exps">expressions to choice</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> Choice(params Func<string, int, T, Result>[] exps)
        {
            return (match, index, attr) =>
            {
                foreach (Func<string, int, T, Result> exp in exps)
                {
                    Result result = exp(match, index, attr);

                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            };
        }

        /// <summary>
        /// generates expression which matches if the given expression is not matched
        /// </summary>
        /// <param name="exp">expression not to match</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> LookaheadNot(Func<string, int, T, Result> exp)
        {
            return (match, index, attr) =>
            {
                Result result = exp(match, index, attr);

                if(result == null)
                {
                    return new Result("", index, attr);
                }
                else
                {
                    return null;
                }
            };
        }

        private delegate Func<string, int, T, Result> ILetrec(ILetrec letrec);
        private delegate Func<string, int, T, Result>[] ILetrecn(ILetrecn letrec);
        private readonly Func<int, ILetrecn, Func<string, int, T, Result>> FLetrecn =
            (i, g) => (match, index, attr) => g(g)[i](match, index, attr);

        /// <summary>
        /// A method which can refer a return values of the function itself.<br>
        /// This method will be used for defining a expression with recursion.
        /// </summary>
        /// <param name="funcs">functions which are return values itself</param>
        /// <returns>matcher function</returns>
        public Func<string, int, T, Result> Letrecn(
            params Func<Func<string, int, T, Result>[], Func<string, int, T, Result>>[] funcs)
        {
            ILetrecn f = g => g(g);
            ILetrecn h = g => {
                var prms = new List<Func<string, int, T, Result>>();
                for(int i = 0; i < funcs.Length; i++)
                {
                    prms.Add(FLetrecn(i, g));
                }

                Func<string, int, T, Result>[] args = prms.ToArray();
                var result = new List<Func<string, int, T, Result>>();
                foreach(Func<Func<string, int, T, Result>[], Func<string, int, T, Result>> func in funcs)
                {
                    result.Add(func(args));
                }
                return result.ToArray();
            };
            return f(h)[0];
        }

        /// <summary>
        /// A method which can refer a return values of the function itself.<br>
        /// This method will be used for defining a expression with recursion.
        /// </summary>
        /// <param name="func">a function which is a return value itself</param>
        /// <returns>matcher function</returns>
        public Func<string, int, T, Result> Letrec1(
            Func<Func<string, int, T, Result>, Func<string, int, T, Result>> func)
        {
            ILetrec f = g => g(g);
            ILetrec h = g => func((match, index, attr) => g(g)(match, index, attr));
            return f(h);
        }

        /// <summary>
        /// A method which can refer a return values of the function itself.<br>
        /// This method will be used for defining a expression with recursion.
        /// </summary>
        /// <param name="func1">a function whose first argument is a return value itself</param>
        /// <param name="func2">a function whose second argument is a return value itself</param>
        /// <returns>matcher function</returns>
        public Func<string, int, T, Result> Letrec2(
            Func<Func<string, int, T, Result>, Func<string, int, T, Result>, Func<string, int, T, Result>> func1,
            Func<Func<string, int, T, Result>, Func<string, int, T, Result>, Func<string, int, T, Result>> func2)
        {
            ILetrecn f = g => g(g);
            ILetrecn h = g => {
                var prm1 = FLetrecn(0, g);
                var prm2 = FLetrecn(1, g);

                var result = new Func<string, int, T, Result>[2];
                result[0] = func1(prm1, prm2);
                result[1] = func2(prm1, prm2);
                return result;
            };
            return f(h)[0];
        }

        /// <summary>
        /// A method which can refer a return values of the function itself.<br>
        /// This method will be used for defining a expression with recursion.
        /// </summary>
        /// <param name="func1">a function whose first argument is a return value itself</param>
        /// <param name="func2">a function whose second argument is a return value itself</param>
        /// <param name="func3">a function whose third argument is a return value itself</param>
        /// <returns>matcher function</returns>
        public Func<string, int, T, Result> Letrec3(
            Func<Func<string, int, T, Result>, Func<string, int, T, Result>, Func<string, int, T, Result>, Func<string, int, T, Result>> func1,
            Func<Func<string, int, T, Result>, Func<string, int, T, Result>, Func<string, int, T, Result>, Func<string, int, T, Result>> func2,
            Func<Func<string, int, T, Result>, Func<string, int, T, Result>, Func<string, int, T, Result>, Func<string, int, T, Result>> func3)
        {
            ILetrecn f = g => g(g);
            ILetrecn h = g => {
                var prm1 = FLetrecn(0, g);
                var prm2 = FLetrecn(1, g);
                var prm3 = FLetrecn(2, g);

                var result = new Func<string, int, T, Result>[3];
                result[0] = func1(prm1, prm2, prm3);
                result[1] = func2(prm1, prm2, prm3);
                result[2] = func3(prm1, prm2, prm3);
                return result;
            };
            return f(h)[0];
        }

        /// <summary>
        /// generate expression to match zero or more occurrence
        /// </summary>
        /// <param name="exp">expression to repeat</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> ZeroOrMore(Func<string, int, T, Result> exp)
        {
            return Letrec1(x => Choice(Concat(exp, x), Str("")));
        }

        /// <summary>
        /// generate expression to match one or more occurrence
        /// </summary>
        /// <param name="exp">expression to repeat</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> OneOrMore(Func<string, int, T, Result> exp)
        {
            return Concat(exp, Letrec1(x => Choice(Concat(exp, x), Str(""))));
        }

        /// <summary>
        /// generate expression to match zero or one occurrence
        /// </summary>
        /// <param name="exp">expression to match optionally</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> Opt(Func<string, int, T, Result> exp)
        {
            return Choice(exp, Str(""));
        }

        /// <summary>
        /// generates expression which matches if the given expression is matched but input is not consumed
        /// </summary>
        /// <param name="exp">expression to match</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> Lookahead(Func<string, int, T, Result> exp)
        {
            return LookaheadNot(LookaheadNot(exp));
        }

        /// <summary>
        /// generates expression which matches keyword (operator)
        /// </summary>
        /// <param name="key">keyword to match</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> Key(string key)
        {
            var skipKeys = new List<Func<string, int, T, Result>>();
            
            if(keys == null)
            {
                throw new ArgumentException("keys are not set");
            }
            foreach(var skipKey in keys)
            {
                if(skipKey.Length > key.Length)
                {
                    skipKeys.Add(Str(skipKey));
                }
            }
            return Concat(LookaheadNot(Choice(skipKeys.ToArray())), Str(key));
        }

        /// <summary>
        /// generates expression which does not match any keywords (operators)
        /// </summary>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> NotKey()
        {
            var skipKeys = new List<Func<string, int, T, Result>>();

            if (keys == null)
            {
                throw new ArgumentException("keys are not set");
            }
            foreach (var skipKey in keys)
            {
                skipKeys.Add(Str(skipKey));
            }
            return LookaheadNot(Choice(skipKeys.ToArray()));
        }

        /// <summary>
        /// generates expression which matches identifier with succeeding the pattern to ignore
        /// or keywords (operators)
        /// </summary>
        /// <param name="key">identifier to match</param>
        /// <returns>expression</returns>
        public Func<string, int, T, Result> EqualsId(string key)
        {
            if(ignore == null && keys == null)
            {
                return Str(key);
            }
            else if(ignore == null)
            {
                return Concat((match, index) => index, Str(key), Choice(IsEnd(), LookaheadNot(NotKey())));
            }
            else if(keys == null)
            {
                return Concat((match, index) => index, Str(key), Choice(IsEnd(), Lookahead(ignore)));
            }
            else
            {
                return Concat((match, index) => index, Str(key), Choice(IsEnd(), Lookahead(ignore), LookaheadNot(NotKey())));
            }
        }
    }
}
