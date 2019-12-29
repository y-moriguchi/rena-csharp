# Rena C#
Rena C# is a library of parsing texts. Rena C# makes parsing text easily.  
Rena C# can treat recursion of pattern, hence Rena C# can parse languages which described top down parsing
like arithmetic expressions and so on.  
Rena C# can parse class of Parsing Expression Grammar (PEG) language.  
Rena C# can also treat synthesized and inherited attributes.  
'Rena' is an acronym of REpetation (or REcursion) Notation API.  

## Expression

### Construct Expression Generation Object
```csharp
Rena<Type>.GetInstance();                           // ignore no characters and not specify any keys
Rena<Type>.Ignore("regex");                         // ignore characters which matches the given regex
Rena<Type>.Keys("specify", "keys", "...");          // specify the given keys
Rena<Type>.IgnoreAndKeys("regex", "keys", "...");   // specify the pattern to ignore and keys
```

An example which generates object show as follows.
```js
Rena<int> r = Rena<int>.IgnoreAndKeys("\\s+", "+", "-", "++");
```

### Elements of Expression

#### String
Expression to match a string is an element of expression.
```
r.Str("aString");
```

#### Regular expression
Expression to match regular expression is an element of expression.
```
r.RE("regex");
```

#### Attrinbute Setting Expression
Attribute setting expression is an element of expression.
```
r.Attr(attribute to set);
```

#### Key Matching Expression
Key matching expression is an element of expression.  
If keys "+", "++", "-" are specified by option, below expression matches "+" but does not match "+" after "+".
```
r.Key("+");
```

#### Not Key Matching Expression
Not key matching expression is an element of expression.
If keys "+", "++", "-" are specified by option, "+", "++", "-" will not match.
```
r.NotKey();
```

#### Keyword Matching Expression
Keyword matching expression is an element of expression.
```
r.EqualsId(keyword);
```

The table shows how to match expression r.equalsId("keyword") by option.

|option|keyword|keyword1|keyword-1|keyword+|
|:-----|:------|:-------|:--------|:-------|
|no options|match|match|match|match|
|ignore: /-/|match|no match|match|no match|
|keys: ["+"]|match|no match|no match|match|
|ignore: /-/ and keys: ["+"]|match|no match|match|match|

#### Real Number
Real number expression is an element of expression and matches any real number.
```
r.Real();
```

#### End of string
End of string is an element of expression and matches the end of string.
```
r.IsEnd();
```

#### Function
Function which fulfilled condition shown as follow is an element of expression.  
* the function has 3 arguments
* first argument is a string to match
* second argument is last index of last match
* third argument is an attribute
* return value of the function is an instance of class Rena.Result which has 3 properties
  * "Match": matched string
  * "LastIndex": last index of matched string
  * "Attr": result attribute

Every instance of expression is a function fulfilled above condition.

### Synthesized Expression

#### Sequence
Sequence expression matches if all specified expression are matched sequentially.  
Below expression matches "abc".
```
r.Concat("a", "b", "c");
```

#### Choice
Choice expression matches if one of specified expression are matched.  
Specified expression will be tried sequentially.  
Below expression matches "a", "b" or "c".
```
r.Choice("a", "b", "c");
```

#### Repetation
Repetation expression matches repetation of specified expression.  
The family of repetation expression are shown as follows.  
```
r.OneOrMore(expression);
r.ZeroOrMore(expression);
```

Repetation expression is already greedy and does not backtrack.

#### Optional
Optional expression matches the expression if it is matched, or matches empty string.
```
r.Opt(expression);
```

#### Lookahead (AND predicate)
Lookahead (AND predicate) matches the specify expression but does not consume input string.
Below example matches "ab" but matched string is "a", and does not match "ad".
```
r.Concat("a", r.Lookahead("b"));
```

#### Nogative Lookahead (NOT predicate)
Negative lookahead (NOT predicate) matches if the specify expression does not match.
Below example matches "ab" but matched string is "a", and does not match "ad".
```
r.Concat("a", r.LookaheadNot("d"));
```

#### Action
Action expression matches the specified expression.  
```
r.Action(expression, action);
```

The second argument must be a function with 3 arguments and return result attribute.  
First argument of the function will pass a matched string,
second argument will pass an attribute of repetation expression ("synthesized attribtue"),
and third argument will pass an inherited attribute.  

Below example, argument of action will be passed ("2", "2", "").
```csharp
r.Action(r.RE("[0-9]"), (match, synthesized, inherited) => match)("2", 0, "")
```

### Matching Expression
To apply string to match to an expression, call the expression with 3 arguments shown as follows.
1. a string to match
2. an index to begin to match
3. an initial attribute

```csharp
var match = r.OneOrMore(r.Action(r.RE("[0-9]"), (match, synthesized, inherited) => inherited + ":" + synthesized));
match("27", 0, "");
```

### Description of Recursion
The family of r.LetrecX function is available to recurse an expression.  
The argument of r.LetrecX function are functions, and return value is the return value of first function.

The family of r.LetrecX are shown as follows.
```
r.Letrec1((x) => pattern);
r.Letrec2((x, y) => pattern, (x, y) => pattern);
r.Letrec3((x, y, z) => pattern, (x, y, z) => pattern, (x, y, z) => pattern);
r.Letrecn(array of (array of x) => pattern);   // length of argument of r.Letrecn must be equal to argument of lambda
```

Below example matches balanced parenthesis.
```csharp
var paren = r.Letrec1((a) => r.Concat(r.Str("("), r.Opt(a), r.Str(")")));
```

