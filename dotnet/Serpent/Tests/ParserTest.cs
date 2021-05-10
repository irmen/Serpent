using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

// ReSharper disable CheckNamespace
// ReSharper disable SpecifyACultureInStringConversionExplicitly
// ReSharper disable RedundantAssignment
// ReSharper disable PossibleNullReferenceException
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeMadeStatic.Local

namespace Razorvine.Serpent.Test
{
    public class ParserTest
    {
        [Fact]
        public void TestBasic()
        {
            var p = new Parser();
            Assert.Null(p.Parse((string) null).Root);
            Assert.Null(p.Parse("").Root);
            Assert.NotNull(p.Parse("# comment\n42\n").Root);
        }

        [Fact]
        public void TestComments()
        {
            var p = new Parser();

            var ast = p.Parse("[ 1, 2 ]"); // no header whatsoever
            var visitor = new ObjectifyVisitor();
            ast.Accept(visitor);
            var obj = visitor.GetObject();
            Assert.Equal(new[] {1, 2}, obj);

            ast = p.Parse(@"# serpent utf-8 python2.7
[ 1, 2,
   # some comments here
   3, 4]    # more here
# and here.
");
            visitor = new ObjectifyVisitor();
            ast.Accept(visitor);
            obj = visitor.GetObject();
            Assert.Equal(new[] {1, 2, 3, 4}, obj);
        }

        [Fact]
        public void TestPrimitives()
        {
            var p = new Parser();
            Assert.Equal(new Ast.IntegerNode(42), p.Parse("42").Root);
            Assert.Equal(new Ast.IntegerNode(-42), p.Parse("-42").Root);
            Assert.Equal(new Ast.DoubleNode(42.331), p.Parse("42.331").Root);
            Assert.Equal(new Ast.DoubleNode(-42.331), p.Parse("-42.331").Root);
            Assert.Equal(new Ast.DoubleNode(-1.2e19), p.Parse("-1.2e+19").Root);
            Assert.Equal(new Ast.DoubleNode(-1.2e19), p.Parse("-1.2e19").Root);
            Assert.Equal(new Ast.DoubleNode(0.0004), p.Parse("4e-4").Root);
            Assert.Equal(new Ast.DoubleNode(40000), p.Parse("4e4").Root);
            Assert.Equal(new Ast.BooleanNode(true), p.Parse("True").Root);
            Assert.Equal(new Ast.BooleanNode(false), p.Parse("False").Root);
            Assert.Equal(Ast.NoneNode.Instance, p.Parse("None").Root);

            // long ints
            Assert.Equal(new Ast.DecimalNode(123456789123456789123456789M),
                p.Parse("123456789123456789123456789").Root);
            Assert.NotEqual(new Ast.LongNode(52), p.Parse("52").Root);
            Assert.Equal(new Ast.LongNode(123456789123456789L), p.Parse("123456789123456789").Root);
            Assert.Throws<ParseException>(() => p.Parse("123456789123456789123456789123456789")); // overflow
        }

        [Fact]
        public void TestWeirdFloats()
        {
            var p = new Parser();
            var d = (Ast.DoubleNode) p.Parse("1e30000").Root;
            Assert.True(double.IsPositiveInfinity(d.Value));
            d = (Ast.DoubleNode) p.Parse("-1e30000").Root;
            Assert.True(double.IsNegativeInfinity(d.Value));

            var tuple = (Ast.TupleNode) p.Parse("(1e30000,-1e30000,{'__class__':'float','value':'nan'})").Root;
            Assert.Equal(3, tuple.Elements.Count);
            d = (Ast.DoubleNode) tuple.Elements[0];
            Assert.True(double.IsPositiveInfinity(d.Value));
            d = (Ast.DoubleNode) tuple.Elements[1];
            Assert.True(double.IsNegativeInfinity(d.Value));
            d = (Ast.DoubleNode) tuple.Elements[2];
            Assert.True(double.IsNaN(d.Value));

            var c = (Ast.ComplexNumberNode) p.Parse("(1e30000-1e30000j)").Root;
            Assert.True(double.IsPositiveInfinity(c.Real));
            Assert.True(double.IsNegativeInfinity(c.Imaginary));
        }

        [Fact]
        public void TestFloatPrecision()
        {
            var p = new Parser();
            var serpent = new Serializer();
            var ser = serpent.Serialize(1.2345678987654321);
            var dv = (Ast.DoubleNode) p.Parse(ser).Root;
            Assert.Equal(1.2345678987654321.ToString(), dv.Value.ToString());

            ser = serpent.Serialize(5555.12345678987656);
            dv = (Ast.DoubleNode) p.Parse(ser).Root;
            Assert.Equal(5555.12345678987656.ToString(), dv.Value.ToString());

            ser = serpent.Serialize(98765432123456.12345678987656);
            dv = (Ast.DoubleNode) p.Parse(ser).Root;
            Assert.Equal(98765432123456.12345678987656.ToString(), dv.Value.ToString());

            ser = serpent.Serialize(98765432123456.12345678987656e+44);
            dv = (Ast.DoubleNode) p.Parse(ser).Root;
            Assert.Equal(98765432123456.12345678987656e+44.ToString(), dv.Value.ToString());

            ser = serpent.Serialize(-98765432123456.12345678987656e-44);
            dv = (Ast.DoubleNode) p.Parse(ser).Root;
            Assert.Equal((-98765432123456.12345678987656e-44).ToString(), dv.Value.ToString());
        }

        [Fact]
        public void TestEquality()
        {
            Ast.INode n1 = new Ast.IntegerNode(42);
            Ast.INode n2 = new Ast.IntegerNode(42);
            Assert.Equal(n1, n2);
            n2 = new Ast.IntegerNode(43);
            Assert.NotEqual(n1, n2);

            n1 = new Ast.StringNode("foo");
            n2 = new Ast.StringNode("foo");
            Assert.Equal(n1, n2);
            n2 = new Ast.StringNode("bar");
            Assert.NotEqual(n1, n2);

            n1 = new Ast.ComplexNumberNode
            {
                Real = 1.1,
                Imaginary = 2.2
            };
            n2 = new Ast.ComplexNumberNode
            {
                Real = 1.1,
                Imaginary = 2.2
            };
            Assert.Equal(n1, n2);
            n2 = new Ast.ComplexNumberNode
            {
                Real = 1.1,
                Imaginary = 3.3
            };
            Assert.NotEqual(n1, n2);

            n1 = new Ast.KeyValueNode
            {
                Key = new Ast.IntegerNode(42),
                Value = new Ast.IntegerNode(42)
            };
            n2 = new Ast.KeyValueNode
            {
                Key = new Ast.IntegerNode(42),
                Value = new Ast.IntegerNode(42)
            };
            Assert.Equal(n1, n2);
            n1 = new Ast.KeyValueNode
            {
                Key = new Ast.IntegerNode(43),
                Value = new Ast.IntegerNode(43)
            };
            Assert.NotEqual(n1, n2);

            n1 = Ast.NoneNode.Instance;
            n2 = Ast.NoneNode.Instance;
            Assert.Equal(n1, n2);
            n2 = new Ast.IntegerNode(42);
            Assert.NotEqual(n1, n2);

            n1 = new Ast.DictNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.KeyValueNode
                    {
                        Key = new Ast.IntegerNode(42),
                        Value = new Ast.IntegerNode(42)
                    }
                }
            };
            n2 = new Ast.DictNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.KeyValueNode
                    {
                        Key = new Ast.IntegerNode(42),
                        Value = new Ast.IntegerNode(42)
                    }
                }
            };
            Assert.Equal(n1, n2);
            n2 = new Ast.DictNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.KeyValueNode
                    {
                        Key = new Ast.IntegerNode(42),
                        Value = new Ast.IntegerNode(43)
                    }
                }
            };
            Assert.NotEqual(n1, n2);

            n1 = new Ast.ListNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.IntegerNode(42)
                }
            };
            n2 = new Ast.ListNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.IntegerNode(42)
                }
            };
            Assert.Equal(n1, n2);
            n2 = new Ast.ListNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.IntegerNode(43)
                }
            };
            Assert.NotEqual(n1, n2);

            n1 = new Ast.SetNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.IntegerNode(42)
                }
            };
            n2 = new Ast.SetNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.IntegerNode(42)
                }
            };
            Assert.Equal(n1, n2);
            n2 = new Ast.SetNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.IntegerNode(43)
                }
            };
            Assert.NotEqual(n1, n2);

            n1 = new Ast.TupleNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.IntegerNode(42)
                }
            };
            n2 = new Ast.TupleNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.IntegerNode(42)
                }
            };
            Assert.Equal(n1, n2);
            n2 = new Ast.TupleNode
            {
                Elements = new List<Ast.INode>
                {
                    new Ast.IntegerNode(43)
                }
            };
            Assert.NotEqual(n1, n2);
        }

        [Fact]
        public void TestDictEquality()
        {
            var dict1 = new Ast.DictNode();
            var kv = new Ast.KeyValueNode
            {
                Key = new Ast.StringNode("key1"),
                Value = new Ast.IntegerNode(42)
            };
            dict1.Elements.Add(kv);
            kv = new Ast.KeyValueNode
            {
                Key = new Ast.StringNode("key2"),
                Value = new Ast.IntegerNode(43)
            };
            dict1.Elements.Add(kv);
            kv = new Ast.KeyValueNode
            {
                Key = new Ast.StringNode("key3"),
                Value = new Ast.IntegerNode(44)
            };
            dict1.Elements.Add(kv);

            var dict2 = new Ast.DictNode();
            kv = new Ast.KeyValueNode
            {
                Key = new Ast.StringNode("key2"),
                Value = new Ast.IntegerNode(43)
            };
            dict2.Elements.Add(kv);
            kv = new Ast.KeyValueNode
            {
                Key = new Ast.StringNode("key3"),
                Value = new Ast.IntegerNode(44)
            };
            dict2.Elements.Add(kv);
            kv = new Ast.KeyValueNode
            {
                Key = new Ast.StringNode("key1"),
                Value = new Ast.IntegerNode(42)
            };
            dict2.Elements.Add(kv);

            Assert.Equal(dict1, dict2);
            kv = new Ast.KeyValueNode
            {
                Key = new Ast.StringNode("key4"),
                Value = new Ast.IntegerNode(45)
            };
            dict2.Elements.Add(kv);
            Assert.NotEqual(dict1, dict2);
        }

        [Fact]
        public void TestSetEquality()
        {
            var set1 = new Ast.SetNode();
            set1.Elements.Add(new Ast.IntegerNode(1));
            set1.Elements.Add(new Ast.IntegerNode(2));
            set1.Elements.Add(new Ast.IntegerNode(3));

            var set2 = new Ast.SetNode();
            set2.Elements.Add(new Ast.IntegerNode(2));
            set2.Elements.Add(new Ast.IntegerNode(3));
            set2.Elements.Add(new Ast.IntegerNode(1));

            Assert.Equal(set1, set2);
            set2.Elements.Add(new Ast.IntegerNode(0));
            Assert.NotEqual(set1, set2);
        }

        [Fact]
        public void TestPrintSingle()
        {
            var p = new Parser();

            // primitives
            Assert.Equal("42", p.Parse("42").Root.ToString());
            Assert.Equal("-42.331", p.Parse("-42.331").Root.ToString());
            Assert.Equal("-42.0", p.Parse("-42.0").Root.ToString());
            Assert.Equal("-2E+20", p.Parse("-2E20").Root.ToString());
            Assert.Equal("2.0", p.Parse("2.0").Root.ToString());
            Assert.Equal("1.2E+19", p.Parse("1.2e19").Root.ToString());
            Assert.Equal("True", p.Parse("True").Root.ToString());
            Assert.Equal("'hello'", p.Parse("'hello'").Root.ToString());
            Assert.Equal("'\\n'", p.Parse("'\n'").Root.ToString());
            Assert.Equal("'\\''", p.Parse("'\\''").Root.ToString());
            Assert.Equal("'\"'", p.Parse("'\\\"'").Root.ToString());
            Assert.Equal("'\"'", p.Parse("'\"'").Root.ToString());
            Assert.Equal("'\\\\'", p.Parse("'\\\\'").Root.ToString());
            Assert.Equal("None", p.Parse("None").Root.ToString());
            const string ustr = "'\u20ac\u2603'";
            Assert.Equal(ustr, p.Parse(ustr).Root.ToString());

            // complex
            Assert.Equal("(0+2j)", p.Parse("2j").Root.ToString());
            Assert.Equal("(-1.1-2.2j)", p.Parse("(-1.1-2.2j)").Root.ToString());
            Assert.Equal("(1.1+2.2j)", p.Parse("(1.1+2.2j)").Root.ToString());

            // long int
            Assert.Equal("123456789123456789123456789", p.Parse("123456789123456789123456789").Root.ToString());
        }

        [Fact]
        public void TestPrintSeq()
        {
            var p = new Parser();

            //tuple
            Assert.Equal("()", p.Parse("()").Root.ToString());
            Assert.Equal("(42,)", p.Parse("(42,)").Root.ToString());
            Assert.Equal("(42,43)", p.Parse("(42,43)").Root.ToString());

            // list			
            Assert.Equal("[]", p.Parse("[]").Root.ToString());
            Assert.Equal("[42]", p.Parse("[42]").Root.ToString());
            Assert.Equal("[42,43]", p.Parse("[42,43]").Root.ToString());

            // set			
            Assert.Equal("{42}", p.Parse("{42}").Root.ToString());
            Assert.Equal("{42,43}", p.Parse("{42,43,43,43}").Root.ToString());

            // dict			
            Assert.Equal("{}", p.Parse("{}").Root.ToString());
            Assert.Equal("{'a':42}", p.Parse("{'a': 42}").Root.ToString());
            Assert.Equal("{'a':42,'b':43}", p.Parse("{'a': 42, 'b': 43}").Root.ToString());
            Assert.Equal("{'a':42,'b':45}", p.Parse("{'a': 42, 'b': 43, 'b': 44, 'b': 45}").Root.ToString());
        }

        [Fact]
        public void TestInvalidPrimitives()
        {
            var p = new Parser();
            Assert.Throws<ParseException>(() => p.Parse("1+2"));
            Assert.Throws<ParseException>(() => p.Parse("1-2"));
            Assert.Throws<ParseException>(() => p.Parse("1.1+2.2"));
            Assert.Throws<ParseException>(() => p.Parse("1.1-2.2"));
            Assert.Throws<ParseException>(() => p.Parse("True+2"));
            Assert.Throws<ParseException>(() => p.Parse("False-2"));
            Assert.Throws<ParseException>(() => p.Parse("3j+2"));
            Assert.Throws<ParseException>(() => p.Parse("3j-2"));
            Assert.Throws<ParseException>(() => p.Parse("None+2"));
            Assert.Throws<ParseException>(() => p.Parse("None-2"));
        }

        [Fact]
        public void TestComplex()
        {
            var p = new Parser();
            var cplx = new Ast.ComplexNumberNode
            {
                Real = 4.2,
                Imaginary = 3.2
            };
            var cplx2 = new Ast.ComplexNumberNode
            {
                Real = 4.2,
                Imaginary = 99
            };
            Assert.NotEqual(cplx, cplx2);
            cplx2.Imaginary = 3.2;
            Assert.Equal(cplx, cplx2);

            Assert.Equal(cplx, p.Parse("(4.2+3.2j)").Root);
            cplx.Real = 0;
            Assert.Equal(cplx, p.Parse("(0+3.2j)").Root);
            Assert.Equal(cplx, p.Parse("3.2j").Root);
            Assert.Equal(cplx, p.Parse("+3.2j").Root);
            cplx.Imaginary = -3.2;
            Assert.Equal(cplx, p.Parse("-3.2j").Root);
            cplx.Real = -9.9;
            Assert.Equal(cplx, p.Parse("(-9.9-3.2j)").Root);

            cplx.Real = 2;
            cplx.Imaginary = 3;
            Assert.Equal(cplx, p.Parse("(2+3j)").Root);
            cplx.Imaginary = -3;
            Assert.Equal(cplx, p.Parse("(2-3j)").Root);
            cplx.Real = 0;
            Assert.Equal(cplx, p.Parse("-3j").Root);

            cplx.Real = -3.2e32;
            cplx.Imaginary = -9.9e44;
            Assert.Equal(cplx, p.Parse("(-3.2e32 -9.9e44j)").Root);
            Assert.Equal(cplx, p.Parse("(-3.2e+32 -9.9e+44j)").Root);
            Assert.Equal(cplx, p.Parse("(-3.2e32-9.9e44j)").Root);
            Assert.Equal(cplx, p.Parse("(-3.2e+32-9.9e+44j)").Root);
            cplx.Imaginary = 9.9e44;
            Assert.Equal(cplx, p.Parse("(-3.2e32+9.9e44j)").Root);
            Assert.Equal(cplx, p.Parse("(-3.2e+32+9.9e+44j)").Root);
            cplx.Real = -3.2e-32;
            cplx.Imaginary = -9.9e-44;
            Assert.Equal(cplx, p.Parse("(-3.2e-32-9.9e-44j)").Root);
        }

        [Fact]
        public void TestComplexPrecision()
        {
            var p = new Parser();
            var cv = (Ast.ComplexNumberNode) p.Parse("(98765432123456.12345678987656+665544332211.9998877665544j)")
                .Root;
            Assert.Equal(98765432123456.12345678987656, cv.Real);
            Assert.Equal(665544332211.9998877665544, cv.Imaginary);
            cv = (Ast.ComplexNumberNode) p.Parse("(98765432123456.12345678987656-665544332211.9998877665544j)").Root;
            Assert.Equal(98765432123456.12345678987656, cv.Real);
            Assert.Equal(-665544332211.9998877665544, cv.Imaginary);
            cv = (Ast.ComplexNumberNode) p.Parse("(98765432123456.12345678987656e+33+665544332211.9998877665544e+44j)")
                .Root;
            Assert.Equal(98765432123456.12345678987656e+33, cv.Real);
            Assert.Equal(665544332211.9998877665544e+44, cv.Imaginary);
            cv = (Ast.ComplexNumberNode) p.Parse("(-98765432123456.12345678987656e+33-665544332211.9998877665544e+44j)")
                .Root;
            Assert.Equal(-98765432123456.12345678987656e+33, cv.Real);
            Assert.Equal(-665544332211.9998877665544e+44, cv.Imaginary);
        }

        [Fact]
        public void TestPrimitivesStuffAtEnd()
        {
            var p = new Parser();
            Assert.Equal(new Ast.IntegerNode(42), p.ParseSingle(new SeekableStringReader("42@")));
            Assert.Equal(new Ast.DoubleNode(42.331), p.ParseSingle(new SeekableStringReader("42.331@")));
            Assert.Equal(new Ast.BooleanNode(true), p.ParseSingle(new SeekableStringReader("True@")));
            Assert.Equal(Ast.NoneNode.Instance, p.ParseSingle(new SeekableStringReader("None@")));
            var cplx = new Ast.ComplexNumberNode
            {
                Real = 4,
                Imaginary = 3
            };
            Assert.Equal(cplx, p.ParseSingle(new SeekableStringReader("(4+3j)@")));
            cplx.Real = 0;
            Assert.Equal(cplx, p.ParseSingle(new SeekableStringReader("3j@")));
        }

        [Fact]
        public void TestStrings()
        {
            var p = new Parser();
            Assert.Equal(new Ast.StringNode("hello"), p.Parse("'hello'").Root);
            Assert.Equal(new Ast.StringNode("hello"), p.Parse("\"hello\"").Root);
            Assert.Equal(new Ast.StringNode("\\"), p.Parse("'\\\\'").Root);
            Assert.Equal(new Ast.StringNode("\\"), p.Parse("\"\\\\\"").Root);
            Assert.Equal(new Ast.StringNode("'"), p.Parse("\"'\"").Root);
            Assert.Equal(new Ast.StringNode("\""), p.Parse("'\"'").Root);
            Assert.Equal(new Ast.StringNode("tab\tnewline\n."), p.Parse("'tab\\tnewline\\n.'").Root);
        }

        [Fact]
        public void TestUnicode()
        {
            var p = new Parser();
            const string str = "'\u20ac\u2603'";
            Assert.Equal(0x20ac, str[1]);
            Assert.Equal(0x2603, str[2]);
            var bytes = Encoding.UTF8.GetBytes(str);

            const string value = "\u20ac\u2603";
            Assert.Equal(new Ast.StringNode(value), p.Parse(str).Root);
            Assert.Equal(new Ast.StringNode(value), p.Parse(bytes).Root);
        }

        [Fact]
        public void TestLongUnicodeRoundtrip()
        {
            var chars64k = new char[65536];
            for (var i = 0; i <= 65535; ++i)
                chars64k[i] = (char) i;

            var str64k = new string(chars64k);

            var ser = new Serializer();
            var data = ser.Serialize(str64k);
            Assert.True(data.Length > chars64k.Length);

            var p = new Parser();
            var result = (string) p.Parse(data).GetData();
            Assert.Equal(str64k, result);
        }

        [Fact]
        public void TestWhitespace()
        {
            var p = new Parser();
            Assert.Equal(new Ast.IntegerNode(42), p.Parse(" 42 ").Root);
            Assert.Equal(new Ast.IntegerNode(42), p.Parse("  42  ").Root);
            Assert.Equal(new Ast.IntegerNode(42), p.Parse("\t42\r\n").Root);
            Assert.Equal(new Ast.IntegerNode(42), p.Parse(" \t 42 \r \n ").Root);
            Assert.Equal(new Ast.StringNode("   string value    "), p.Parse("  '   string value    '   ").Root);
            Assert.Throws<ParseException>(() => p.Parse("     (  42  ,  ( 'x',   'y'  )   ")); // missing tuple close )
            var ast = p.Parse("     (  42  ,  ( 'x',   'y'  )  )  ");
            var tuple = (Ast.TupleNode) ast.Root;
            Assert.Equal(new Ast.IntegerNode(42), tuple.Elements[0]);
            tuple = (Ast.TupleNode) tuple.Elements[1];
            Assert.Equal(new Ast.StringNode("x"), tuple.Elements[0]);
            Assert.Equal(new Ast.StringNode("y"), tuple.Elements[1]);

            p.Parse(" ( 52 , ) ");
            p.Parse(" [ 52 ] ");
            p.Parse(" { 'a' : 42 } ");
            p.Parse(" { 52 } ");
        }

        [Fact]
        public void TestTuple()
        {
            var p = new Parser();
            var tuple = new Ast.TupleNode();
            var tuple2 = new Ast.TupleNode();
            Assert.Equal(tuple, tuple2);

            tuple.Elements.Add(new Ast.IntegerNode(42));
            tuple2.Elements.Add(new Ast.IntegerNode(99));
            Assert.NotEqual(tuple, tuple2);
            tuple2.Elements.Clear();
            tuple2.Elements.Add(new Ast.IntegerNode(42));
            Assert.Equal(tuple, tuple2);
            tuple2.Elements.Add(new Ast.IntegerNode(43));
            tuple2.Elements.Add(new Ast.IntegerNode(44));
            Assert.NotEqual(tuple, tuple2);

            Assert.Equal(new Ast.TupleNode(), p.Parse("()").Root);
            Assert.Equal(tuple, p.Parse("(42,)").Root);
            Assert.Equal(tuple2, p.Parse("( 42,43, 44 )").Root);

            Assert.Throws<ParseException>(() => p.Parse("(42,43]"));
            Assert.Throws<ParseException>(() => p.Parse("()@"));
            Assert.Throws<ParseException>(() => p.Parse("(42,43)@"));
        }

        [Fact]
        public void TestList()
        {
            var p = new Parser();
            var list = new Ast.ListNode();
            var list2 = new Ast.ListNode();
            Assert.Equal(list, list2);

            list.Elements.Add(new Ast.IntegerNode(42));
            list2.Elements.Add(new Ast.IntegerNode(99));
            Assert.NotEqual(list, list2);
            list2.Elements.Clear();
            list2.Elements.Add(new Ast.IntegerNode(42));
            Assert.Equal(list, list2);
            list2.Elements.Add(new Ast.IntegerNode(43));
            list2.Elements.Add(new Ast.IntegerNode(44));
            Assert.NotEqual(list, list2);

            Assert.Equal(new Ast.ListNode(), p.Parse("[]").Root);
            Assert.Equal(list, p.Parse("[42]").Root);
            Assert.Equal(list2, p.Parse("[ 42,43, 44 ]").Root);

            Assert.Throws<ParseException>(() => p.Parse("[42,43}"));
            Assert.Throws<ParseException>(() => p.Parse("[]@"));
            Assert.Throws<ParseException>(() => p.Parse("[42,43]@"));
        }

        [Fact]
        public void TestSet()
        {
            var p = new Parser();
            var set1 = new Ast.SetNode();
            var set2 = new Ast.SetNode();
            Assert.Equal(set1, set2);

            set1.Elements.Add(new Ast.IntegerNode(42));
            set2.Elements.Add(new Ast.IntegerNode(99));
            Assert.NotEqual(set1, set2);
            set2.Elements.Clear();
            set2.Elements.Add(new Ast.IntegerNode(42));
            Assert.Equal(set1, set2);

            set2.Elements.Add(new Ast.IntegerNode(43));
            set2.Elements.Add(new Ast.IntegerNode(44));
            Assert.NotEqual(set1, set2);

            Assert.Equal(set1, p.Parse("{42}").Root);
            Assert.Equal(set2, p.Parse("{ 42,43, 44 }").Root);

            Assert.Throws<ParseException>(() => p.Parse("{42,43]"));
            Assert.Throws<ParseException>(() => p.Parse("{42,43}@"));

            set1 =
                p.Parse("{'first','second','third','fourth','fifth','second', 'first', 'third', 'third' }")
                    .Root as Ast.SetNode;
            Assert.Equal("'first'", set1.Elements[0].ToString());
            Assert.Equal("'second'", set1.Elements[1].ToString());
            Assert.Equal("'third'", set1.Elements[2].ToString());
            Assert.Equal("'fourth'", set1.Elements[3].ToString());
            Assert.Equal("'fifth'", set1.Elements[4].ToString());
            Assert.Equal(5, set1.Elements.Count);
        }

        [Fact]
        public void TestDict()
        {
            var p = new Parser();
            var dict1 = new Ast.DictNode();
            var dict2 = new Ast.DictNode();
            Assert.Equal(dict1, dict2);

            var kv1 = new Ast.KeyValueNode {Key = new Ast.StringNode("key"), Value = new Ast.IntegerNode(42)};
            var kv2 = new Ast.KeyValueNode {Key = new Ast.StringNode("key"), Value = new Ast.IntegerNode(99)};
            Assert.NotEqual(kv1, kv2);
            kv2.Value = new Ast.IntegerNode(42);
            Assert.Equal(kv1, kv2);

            dict1.Elements.Add(new Ast.KeyValueNode
                {Key = new Ast.StringNode("key1"), Value = new Ast.IntegerNode(42)});
            dict2.Elements.Add(new Ast.KeyValueNode
                {Key = new Ast.StringNode("key1"), Value = new Ast.IntegerNode(99)});
            Assert.NotEqual(dict1, dict2);
            dict2.Elements.Clear();
            dict2.Elements.Add(new Ast.KeyValueNode
                {Key = new Ast.StringNode("key1"), Value = new Ast.IntegerNode(42)});
            Assert.Equal(dict1, dict2);

            dict2.Elements.Add(new Ast.KeyValueNode
                {Key = new Ast.StringNode("key2"), Value = new Ast.IntegerNode(43)});
            dict2.Elements.Add(new Ast.KeyValueNode
                {Key = new Ast.StringNode("key3"), Value = new Ast.IntegerNode(44)});
            Assert.NotEqual(dict1, dict2);

            Assert.Equal(new Ast.DictNode(), p.Parse("{}").Root);
            Assert.Equal(dict1, p.Parse("{'key1': 42}").Root);
            Assert.Equal(dict2, p.Parse("{'key1': 42, 'key2': 43, 'key3':44}").Root);

            Assert.Throws<ParseException>(() => p.Parse("{'key': 42]"));
            Assert.Throws<ParseException>(() => p.Parse("{}@"));
            Assert.Throws<ParseException>(() => p.Parse("{'key': 42}@"));

            dict1 = p.Parse("{'a': 1, 'b': 2, 'c': 3, 'c': 4, 'c': 5, 'c': 6}").Root as Ast.DictNode;
            Assert.Equal("'a':1", dict1.Elements[0].ToString());
            Assert.Equal("'b':2", dict1.Elements[1].ToString());
            Assert.Equal("'c':6", dict1.Elements[2].ToString());
            Assert.Equal(3, dict1.Elements.Count);
        }

        [Fact]
        public void TestFile()
        {
            var p = new Parser();
            var ser = File.ReadAllBytes("testserpent.utf8.bin");
            var ast = p.Parse(ser);

            var expr = ast.ToString();
            var ast2 = p.Parse(expr);
            var expr2 = ast2.ToString();
            Assert.Equal(expr, expr2);

            var sb = new StringBuilder();
            Walk(ast.Root, sb);
            var walk1 = sb.ToString();
            sb = new StringBuilder();
            Walk(ast2.Root, sb);
            var walk2 = sb.ToString();
            Assert.Equal(walk1, walk2);

            Assert.Equal(ast.Root, ast2.Root);
            ast = p.Parse(expr2);
            Assert.Equal(ast.Root, ast2.Root);
        }

        [Fact]
        public void TestAstEquals()
        {
            var p = new Parser();
            var ser = File.ReadAllBytes("testserpent.utf8.bin");
            var ast = p.Parse(ser);
            var ast2 = p.Parse(ser);
            Assert.Equal(ast.Root, ast2.Root);
        }

        private void Walk(Ast.INode node, StringBuilder sb)
        {
            var seq = node as Ast.SequenceNode;
            if (seq != null)
            {
                sb.AppendLine(string.Format("{0} (seq)", node.GetType()));
                foreach (var child in seq.Elements) Walk(child, sb);
            }
            else
            {
                sb.AppendLine(string.Format("{0} = {1}", node.GetType(), node.ToString()));
            }
        }

        [Fact]
        public void TestTrailingCommas()
        {
            var p = new Parser();
            var result = p.Parse("[1,2,3,  ]").Root;
            result = p.Parse("[1,2,3  ,  ]").Root;
            result = p.Parse("[1,2,3,]").Root;
            Assert.Equal("[1,2,3]", result.ToString());
            result = p.Parse("(1,2,3,  )").Root;
            result = p.Parse("(1,2,3  ,  )").Root;
            result = p.Parse("(1,2,3,)").Root;
            Assert.Equal("(1,2,3)", result.ToString());

            // for dict and set the asserts are a bit more complex
            // we cannot simply convert to string because the order of elts is undefined.

            result = p.Parse("{'a':1, 'b':2, 'c':3,  }").Root;
            result = p.Parse("{'a':1, 'b':2, 'c':3  ,  }").Root;
            result = p.Parse("{'a':1, 'b':2, 'c':3,}").Root;
            var dict = (Ast.DictNode) result;
            var items = dict.ElementsAsSet();
            Assert.Contains(new Ast.KeyValueNode(new Ast.StringNode("a"), new Ast.IntegerNode(1)), items);
            Assert.Contains(new Ast.KeyValueNode(new Ast.StringNode("b"), new Ast.IntegerNode(2)), items);
            Assert.Contains(new Ast.KeyValueNode(new Ast.StringNode("c"), new Ast.IntegerNode(3)), items);
            result = p.Parse("{1,2,3,  }").Root;
            result = p.Parse("{1,2,3  ,  }").Root;
            result = p.Parse("{1,2,3,}").Root;
            var set = (Ast.SetNode) result;
            items = set.ElementsAsSet();
            Assert.Contains(new Ast.IntegerNode(1), items);
            Assert.Contains(new Ast.IntegerNode(2), items);
            Assert.Contains(new Ast.IntegerNode(3), items);
            Assert.DoesNotContain(new Ast.IntegerNode(4), items);
        }
    }

    public class VisitorTest
    {
        [Fact]
        public void TestObjectify()
        {
            var p = new Parser();
            var ser = File.ReadAllBytes("testserpent.utf8.bin");
            var ast = p.Parse(ser);
            var visitor = new ObjectifyVisitor();
            ast.Accept(visitor);
            var thing = visitor.GetObject();

            var dict = (IDictionary) thing;
            Assert.Equal(11, dict.Count);
            var list = dict["numbers"] as IList<object>;
            Assert.Equal(4, list.Count);
            Assert.Equal(999.1234, list[1]);
            Assert.Equal(new ComplexNumber(-3, 8), list[3]);
            var euro = dict["unicode"] as string;
            Assert.Equal("\u20ac", euro);
            var exc = (IDictionary) dict["exc"];
            var args = (object[]) exc["args"];
            Assert.Equal("fault", args[0]);
            Assert.Equal("ZeroDivisionError", exc["__class__"]);
        }

        private object ZerodivisionFromDict(IDictionary dict)
        {
            var classname = (string) dict["__class__"];
            if (classname != "ZeroDivisionError") return null;
            var args = (object[]) dict["args"];
            return new DivideByZeroException((string) args[0]);
        }

        [Fact]
        public void TestObjectifyDictToClass()
        {
            var p = new Parser();
            var ser = File.ReadAllBytes("testserpent.utf8.bin");
            var ast = p.Parse(ser);

            var visitor = new ObjectifyVisitor(ZerodivisionFromDict);
            ast.Accept(visitor);
            var thing = visitor.GetObject();

            var dict = (IDictionary) thing;
            Assert.Equal(11, dict.Count);
            var ex = (DivideByZeroException) dict["exc"];
            Assert.Equal("fault", ex.Message);

            thing = ast.GetData(ZerodivisionFromDict);
            dict = (IDictionary) thing;
            Assert.Equal(11, dict.Count);
            ex = (DivideByZeroException) dict["exc"];
            Assert.Equal("fault", ex.Message);
        }
    }
}