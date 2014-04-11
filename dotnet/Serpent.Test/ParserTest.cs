/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright 2014, Irmen de Jong (irmen@razorvine.net)
/// Software license: "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

using NUnit.Framework;

namespace Razorvine.Serpent.Test
{
	[TestFixture]
	public class ParserTest
	{
		[Test]
		public void TestBasic()
		{
			Parser p = new Parser();
			Assert.IsNull(p.Parse((string)null).Root);
			Assert.IsNull(p.Parse("").Root);
			Assert.IsNotNull(p.Parse("# comment\n42\n").Root);
		}

		[Test]
		public void TestComments()
		{
			Parser p = new Parser();

			Ast ast = p.Parse("[ 1, 2 ]");  // no header whatsoever
			var visitor = new ObjectifyVisitor();
			ast.Accept(visitor);
			Object obj = visitor.GetObject();
			Assert.AreEqual(new int[] {1,2}, obj);

			ast = p.Parse(@"# serpent utf-8 python2.7
[ 1, 2,
   # some comments here
   3, 4]    # more here
# and here.
");			
			visitor = new ObjectifyVisitor();
			ast.Accept(visitor);
			obj = visitor.GetObject();
			Assert.AreEqual(new int[] {1,2,3,4}, obj);
		}

		[Test]
		public void TestPrimitives()
		{
			Parser p = new Parser();
			Assert.AreEqual(new Ast.IntegerNode(42), p.Parse("42").Root);
			Assert.AreEqual(new Ast.IntegerNode(-42), p.Parse("-42").Root);
			Assert.AreEqual(new Ast.DoubleNode(42.331), p.Parse("42.331").Root);
			Assert.AreEqual(new Ast.DoubleNode(-42.331), p.Parse("-42.331").Root);
			Assert.AreEqual(new Ast.DoubleNode(-1.2e19), p.Parse("-1.2e+19").Root);
			Assert.AreEqual(new Ast.DoubleNode(0.0004), p.Parse("4e-4").Root);
			Assert.AreEqual(new Ast.DoubleNode(40000), p.Parse("4e4").Root);
			Assert.AreEqual(new Ast.BooleanNode(true), p.Parse("True").Root);
			Assert.AreEqual(new Ast.BooleanNode(false), p.Parse("False").Root);
			Assert.AreEqual(Ast.NoneNode.Instance, p.Parse("None").Root);
			
			// long ints
			Assert.AreEqual(new Ast.DecimalNode(123456789123456789123456789M), p.Parse("123456789123456789123456789").Root);
			Assert.AreNotEqual(new Ast.LongNode(52), p.Parse("52").Root);
			Assert.AreEqual(new Ast.LongNode(123456789123456789L), p.Parse("123456789123456789").Root);
			Assert.Throws<ParseException>(()=>p.Parse("123456789123456789123456789123456789")); // overflow
		}
		
		[Test]
		public void TestEquality()
		{
			Ast.INode n1, n2;

			n1 = new Ast.IntegerNode(42);
			n2 = new Ast.IntegerNode(42);
			Assert.AreEqual(n1, n2);
			n2 = new Ast.IntegerNode(43);
			Assert.AreNotEqual(n1, n2);
			
			n1 = new Ast.StringNode("foo");
			n2 = new Ast.StringNode("foo");
			Assert.AreEqual(n1, n2);
			n2 = new Ast.StringNode("bar");
			Assert.AreNotEqual(n1, n2);
			
			n1 = new Ast.ComplexNumberNode() {
				Real=1.1,
				Imaginary=2.2
			};
			n2 = new Ast.ComplexNumberNode() {
				Real=1.1,
				Imaginary=2.2
			};
			Assert.AreEqual(n1, n2);
			n2 = new Ast.ComplexNumberNode() {
				Real=1.1,
				Imaginary=3.3
			};
			Assert.AreNotEqual(n1, n2);
			
			n1=new Ast.KeyValueNode() {
				Key=new Ast.IntegerNode(42),
				Value=new Ast.IntegerNode(42)
			};
			n2=new Ast.KeyValueNode() {
				Key=new Ast.IntegerNode(42),
				Value=new Ast.IntegerNode(42)
			};
			Assert.AreEqual(n1, n2);
			n1=new Ast.KeyValueNode() {
				Key=new Ast.IntegerNode(43),
				Value=new Ast.IntegerNode(43)
			};
			Assert.AreNotEqual(n1,n2);
			
			n1=Ast.NoneNode.Instance;
			n2=Ast.NoneNode.Instance;
			Assert.AreEqual(n1, n2);
			n2=new Ast.IntegerNode(42);
			Assert.AreNotEqual(n1, n2);
			
			n1=new Ast.DictNode() {
				Elements=new List<Ast.INode>() {
					new Ast.KeyValueNode() {
						Key=new Ast.IntegerNode(42),
						Value=new Ast.IntegerNode(42)
					}
				}
			};
			n2=new Ast.DictNode() {
				Elements=new List<Ast.INode>() {
					new Ast.KeyValueNode() {
						Key=new Ast.IntegerNode(42),
						Value=new Ast.IntegerNode(42)
					}
				}
			};
			Assert.AreEqual(n1, n2);
			n2=new Ast.DictNode() {
				Elements=new List<Ast.INode>() {
					new Ast.KeyValueNode() {
						Key=new Ast.IntegerNode(42),
						Value=new Ast.IntegerNode(43)
					}
				}
			};
			Assert.AreNotEqual(n1, n2);
			
			n1=new Ast.ListNode() {
				Elements=new List<Ast.INode>() {
					new Ast.IntegerNode(42)
				}
			};
			n2=new Ast.ListNode() {
				Elements=new List<Ast.INode>() {
					new Ast.IntegerNode(42)
				}
			};
			Assert.AreEqual(n1,n2);
			n2=new Ast.ListNode() {
				Elements=new List<Ast.INode>() {
					new Ast.IntegerNode(43)
				}
			};
			Assert.AreNotEqual(n1,n2);
			
			n1=new Ast.SetNode() {
				Elements=new List<Ast.INode>() {
					new Ast.IntegerNode(42)
				}
			};
			n2=new Ast.SetNode() {
				Elements=new List<Ast.INode>() {
					new Ast.IntegerNode(42)
				}
			};
			Assert.AreEqual(n1,n2);
			n2=new Ast.SetNode() {
				Elements=new List<Ast.INode>() {
					new Ast.IntegerNode(43)
				}
			};
			Assert.AreNotEqual(n1,n2);
			
			n1=new Ast.TupleNode() {
				Elements=new List<Ast.INode>() {
					new Ast.IntegerNode(42)
				}
			};
			n2=new Ast.TupleNode() {
				Elements=new List<Ast.INode>() {
					new Ast.IntegerNode(42)
				}
			};
			Assert.AreEqual(n1,n2);
			n2=new Ast.TupleNode() {
				Elements=new List<Ast.INode>() {
					new Ast.IntegerNode(43)
				}
			};
			Assert.AreNotEqual(n1,n2);
			
		}
		
		[Test]
		public void TestDictEquality()
		{
			Ast.DictNode dict1 = new Ast.DictNode();
			Ast.KeyValueNode kv=new Ast.KeyValueNode() {
				Key=new Ast.StringNode("key1"),
				Value=new Ast.IntegerNode(42)
			};
			dict1.Elements.Add(kv);
			kv=new Ast.KeyValueNode() {
				Key=new Ast.StringNode("key2"),
				Value=new Ast.IntegerNode(43)
			};
			dict1.Elements.Add(kv);
			kv=new Ast.KeyValueNode() {
				Key=new Ast.StringNode("key3"),
				Value=new Ast.IntegerNode(44)
			};
			dict1.Elements.Add(kv);
	
			Ast.DictNode dict2 = new Ast.DictNode();
			kv=new Ast.KeyValueNode(){
				Key=new Ast.StringNode("key2"),
				Value=new Ast.IntegerNode(43)
			};
			dict2.Elements.Add(kv);
			kv=new Ast.KeyValueNode() {
				Key=new Ast.StringNode("key3"),
				Value=new Ast.IntegerNode(44)
			};
			dict2.Elements.Add(kv);
			kv=new Ast.KeyValueNode() {
				Key=new Ast.StringNode("key1"),
				Value=new Ast.IntegerNode(42)
			};
			dict2.Elements.Add(kv);
			
			Assert.AreEqual(dict1, dict2);
			kv=new Ast.KeyValueNode() {
				Key=new Ast.StringNode("key4"),
				Value=new Ast.IntegerNode(45)
			};
			dict2.Elements.Add(kv);
			Assert.AreNotEqual(dict1, dict2);
		}
		
		[Test]
		public void TestSetEquality()
		{
			Ast.SetNode set1 = new Ast.SetNode();
			set1.Elements.Add(new Ast.IntegerNode(1));
			set1.Elements.Add(new Ast.IntegerNode(2));
			set1.Elements.Add(new Ast.IntegerNode(3));
			
			Ast.SetNode set2 = new Ast.SetNode();
			set2.Elements.Add(new Ast.IntegerNode(2));
			set2.Elements.Add(new Ast.IntegerNode(3));
			set2.Elements.Add(new Ast.IntegerNode(1));
	
			Assert.AreEqual(set1, set2);
			set2.Elements.Add(new Ast.IntegerNode(0));
			Assert.AreNotEqual(set1, set2);
		}
	
		[Test]
		public void TestPrintSingle()
		{
			Parser p = new Parser();
			
			// primitives
			Assert.AreEqual("42", p.Parse("42").Root.ToString());
			Assert.AreEqual("-42.331", p.Parse("-42.331").Root.ToString());
			Assert.AreEqual("-42.0", p.Parse("-42.0").Root.ToString());
			Assert.AreEqual("-2E+20", p.Parse("-2E20").Root.ToString());
			Assert.AreEqual("2.0", p.Parse("2.0").Root.ToString());
			Assert.AreEqual("1.2E+19", p.Parse("1.2e19").Root.ToString());
			Assert.AreEqual("True", p.Parse("True").Root.ToString());
			Assert.AreEqual("'hello'", p.Parse("'hello'").Root.ToString());
			Assert.AreEqual("'\\n'", p.Parse("'\n'").Root.ToString());
			Assert.AreEqual("'\\''", p.Parse("'\\''").Root.ToString());
			Assert.AreEqual("'\"'", p.Parse("'\\\"'").Root.ToString());
			Assert.AreEqual("'\"'", p.Parse("'\"'").Root.ToString());
			Assert.AreEqual("'\\\\'", p.Parse("'\\\\'").Root.ToString());
			Assert.AreEqual("None", p.Parse("None").Root.ToString());
			string ustr = "'\u20ac\u2603'";
			Assert.AreEqual(ustr, p.Parse(ustr).Root.ToString());
			
			// complex
			Assert.AreEqual("(0+2j)", p.Parse("2j").Root.ToString());
			Assert.AreEqual("(-1.1-2.2j)", p.Parse("(-1.1-2.2j)").Root.ToString());
			Assert.AreEqual("(1.1+2.2j)", p.Parse("(1.1+2.2j)").Root.ToString());
			
			// long int
			Assert.AreEqual("123456789123456789123456789", p.Parse("123456789123456789123456789").Root.ToString());
		}
		
		[Test]
		public void TestPrintSeq()
		{
			Parser p=new Parser();
			
			//tuple
			Assert.AreEqual("()", p.Parse("()").Root.ToString());
			Assert.AreEqual("(42,)", p.Parse("(42,)").Root.ToString());
			Assert.AreEqual("(42,43)", p.Parse("(42,43)").Root.ToString());

			// list			
			Assert.AreEqual("[]", p.Parse("[]").Root.ToString());
			Assert.AreEqual("[42]", p.Parse("[42]").Root.ToString());
			Assert.AreEqual("[42,43]", p.Parse("[42,43]").Root.ToString());

			// set			
			Assert.AreEqual("{42}", p.Parse("{42}").Root.ToString());
			Assert.AreEqual("{42,43}", p.Parse("{42,43,43,43}").Root.ToString());

			// dict			
			Assert.AreEqual("{}", p.Parse("{}").Root.ToString());
			Assert.AreEqual("{'a':42}", p.Parse("{'a': 42}").Root.ToString());
			Assert.AreEqual("{'a':42,'b':43}", p.Parse("{'a': 42, 'b': 43}").Root.ToString());
			Assert.AreEqual("{'a':42,'b':45}", p.Parse("{'a': 42, 'b': 43, 'b': 44, 'b': 45}").Root.ToString());
		}
		
		[Test]
		public void TestInvalidPrimitives()
		{
			Parser p = new Parser();
			Assert.Throws<ParseException>(()=>p.Parse("1+2"));
			Assert.Throws<ParseException>(()=>p.Parse("1-2"));
			Assert.Throws<ParseException>(()=>p.Parse("1.1+2.2"));
			Assert.Throws<ParseException>(()=>p.Parse("1.1-2.2"));
			Assert.Throws<ParseException>(()=>p.Parse("True+2"));
			Assert.Throws<ParseException>(()=>p.Parse("False-2"));
			Assert.Throws<ParseException>(()=>p.Parse("3j+2"));
			Assert.Throws<ParseException>(()=>p.Parse("3j-2"));
			Assert.Throws<ParseException>(()=>p.Parse("None+2"));
			Assert.Throws<ParseException>(()=>p.Parse("None-2"));
		}
		
		[Test]
		public void TestComplex()
		{
			Parser p = new Parser();
			var cplx = new Ast.ComplexNumberNode() {
				Real = 4.2,
				Imaginary = 3.2
			};
			var cplx2 = new Ast.ComplexNumberNode() {
				Real = 4.2,
				Imaginary = 99
			};
			Assert.AreNotEqual(cplx, cplx2);
			cplx2.Imaginary = 3.2;
			Assert.AreEqual(cplx, cplx2);

			Assert.AreEqual(cplx, p.Parse("(4.2+3.2j)").Root);
			cplx.Real = 0;
			Assert.AreEqual(cplx, p.Parse("(0+3.2j)").Root);
			Assert.AreEqual(cplx, p.Parse("3.2j").Root);
			Assert.AreEqual(cplx, p.Parse("+3.2j").Root);
			cplx.Imaginary = -3.2;
			Assert.AreEqual(cplx, p.Parse("-3.2j").Root);
			cplx.Real = -9.9;
			Assert.AreEqual(cplx, p.Parse("(-9.9-3.2j)").Root);
		}
		
		[Test]
		public void TestPrimitivesStuffAtEnd()
		{
			Parser p = new Parser();
			Assert.AreEqual(new Ast.IntegerNode(42), p.ParseSingle(new SeekableStringReader("42@")));
			Assert.AreEqual(new Ast.DoubleNode(42.331), p.ParseSingle(new SeekableStringReader("42.331@")));
			Assert.AreEqual(new Ast.BooleanNode(true), p.ParseSingle(new SeekableStringReader("True@")));
			Assert.AreEqual(Ast.NoneNode.Instance, p.ParseSingle(new SeekableStringReader("None@")));
			var cplx = new Ast.ComplexNumberNode() {
				Real = 4,
				Imaginary = 3
			};
			Assert.AreEqual(cplx, p.ParseSingle(new SeekableStringReader("(4+3j)@")));
			cplx.Real=0;
			Assert.AreEqual(cplx, p.ParseSingle(new SeekableStringReader("3j@")));
		}
		
		[Test]
		public void TestStrings()
		{
			Parser p = new Parser();
			Assert.AreEqual(new Ast.StringNode("hello"), p.Parse("'hello'").Root);
			Assert.AreEqual(new Ast.StringNode("hello"), p.Parse("\"hello\"").Root);
			Assert.AreEqual(new Ast.StringNode("\\"), p.Parse("'\\\\'").Root);
			Assert.AreEqual(new Ast.StringNode("\\"), p.Parse("\"\\\\\"").Root);
			Assert.AreEqual(new Ast.StringNode("'"), p.Parse("\"'\"").Root);
			Assert.AreEqual(new Ast.StringNode("\""), p.Parse("'\"'").Root);
			Assert.AreEqual(new Ast.StringNode("tab\tnewline\n."), p.Parse("'tab\\tnewline\\n.'").Root);
		}
		
		[Test]
		public void TestUnicode()
		{
			Parser p = new Parser();
			string str = "'\u20ac\u2603'";
			Assert.AreEqual(0x20ac, str[1]);
			Assert.AreEqual(0x2603, str[2]);
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			
			string value = "\u20ac\u2603";
			Assert.AreEqual(new Ast.StringNode(value), p.Parse(str).Root);
			Assert.AreEqual(new Ast.StringNode(value), p.Parse(bytes).Root);
		}
		
		[Test]
		public void TestWhitespace()
		{
			Parser p = new Parser();
			Assert.AreEqual(new Ast.IntegerNode(42), p.Parse(" 42 ").Root);
			Assert.AreEqual(new Ast.IntegerNode(42), p.Parse("  42  ").Root);
			Assert.AreEqual(new Ast.IntegerNode(42), p.Parse("\t42\r\n").Root);
			Assert.AreEqual(new Ast.IntegerNode(42), p.Parse(" \t 42 \r \n ").Root);
			Assert.AreEqual(new Ast.StringNode("   string value    "), p.Parse("  '   string value    '   ").Root);
			Assert.Throws<ParseException>(()=>p.Parse("     (  42  ,  ( 'x',   'y'  )   "));  // missing tuple close )
			Ast ast = p.Parse("     (  42  ,  ( 'x',   'y'  )  )  ");
			Ast.TupleNode tuple = (Ast.TupleNode) ast.Root;
			Assert.AreEqual(new Ast.IntegerNode(42), tuple.Elements[0]);
			tuple = (Ast.TupleNode) tuple.Elements[1];
			Assert.AreEqual(new Ast.StringNode("x"), tuple.Elements[0]);
			Assert.AreEqual(new Ast.StringNode("y"), tuple.Elements[1]);
			
			p.Parse(" ( 52 , ) ");
			p.Parse(" [ 52 ] ");
			p.Parse(" { 'a' : 42 } ");
			p.Parse(" { 52 } ");
		}
		
		[Test]
		public void TestTuple()
		{
			Parser p = new Parser();
			Ast.TupleNode tuple = new Ast.TupleNode();
			Ast.TupleNode tuple2 = new Ast.TupleNode();
			Assert.AreEqual(tuple, tuple2);
			
			tuple.Elements.Add(new Ast.IntegerNode(42));
			tuple2.Elements.Add(new Ast.IntegerNode(99));
			Assert.AreNotEqual(tuple, tuple2);
			tuple2.Elements.Clear();
			tuple2.Elements.Add(new Ast.IntegerNode(42));
			Assert.AreEqual(tuple, tuple2);
			tuple2.Elements.Add(new Ast.IntegerNode(43));
			tuple2.Elements.Add(new Ast.IntegerNode(44));
			Assert.AreNotEqual(tuple, tuple2);
			
			Assert.AreEqual(new Ast.TupleNode(), p.Parse("()").Root);
			Assert.AreEqual(tuple, p.Parse("(42,)").Root);
			Assert.AreEqual(tuple2, p.Parse("( 42,43, 44 )").Root);

			Assert.Throws<ParseException>(()=>p.Parse("(42,43]"));
			Assert.Throws<ParseException>(()=>p.Parse("()@"));
			Assert.Throws<ParseException>(()=>p.Parse("(42,43)@"));
		}
		
		[Test]
		public void TestList()
		{
			Parser p = new Parser();
			Ast.ListNode list = new Ast.ListNode();
			Ast.ListNode list2 = new Ast.ListNode();
			Assert.AreEqual(list, list2);
			
			list.Elements.Add(new Ast.IntegerNode(42));
			list2.Elements.Add(new Ast.IntegerNode(99));
			Assert.AreNotEqual(list, list2);
			list2.Elements.Clear();
			list2.Elements.Add(new Ast.IntegerNode(42));
			Assert.AreEqual(list, list2);
			list2.Elements.Add(new Ast.IntegerNode(43));
			list2.Elements.Add(new Ast.IntegerNode(44));
			Assert.AreNotEqual(list, list2);
			
			Assert.AreEqual(new Ast.ListNode(), p.Parse("[]").Root);
			Assert.AreEqual(list, p.Parse("[42]").Root);
			Assert.Throws<ParseException>(()=>p.Parse("[42,]"));
			Assert.AreEqual(list2, p.Parse("[ 42,43, 44 ]").Root);

			Assert.Throws<ParseException>(()=>p.Parse("[42,43}"));
			Assert.Throws<ParseException>(()=>p.Parse("[]@"));
			Assert.Throws<ParseException>(()=>p.Parse("[42,43]@"));
		}
		
		[Test]
		public void TestSet()
		{
			Parser p = new Parser();
			Ast.SetNode set1 = new Ast.SetNode();
			Ast.SetNode set2 = new Ast.SetNode();
			Assert.AreEqual(set1, set2);
			
			set1.Elements.Add(new Ast.IntegerNode(42));
			set2.Elements.Add(new Ast.IntegerNode(99));
			Assert.AreNotEqual(set1, set2);
			set2.Elements.Clear();
			set2.Elements.Add(new Ast.IntegerNode(42));
			Assert.AreEqual(set1, set2);
			
			set2.Elements.Add(new Ast.IntegerNode(43));
			set2.Elements.Add(new Ast.IntegerNode(44));
			Assert.AreNotEqual(set1, set2);
			
			Assert.AreEqual(set1, p.Parse("{42}").Root);
			Assert.Throws<ParseException>(()=>p.Parse("{42,}"));
			Assert.AreEqual(set2, p.Parse("{ 42,43, 44 }").Root);

			Assert.Throws<ParseException>(()=>p.Parse("{42,43]"));
			Assert.Throws<ParseException>(()=>p.Parse("{42,43}@"));
			
			set1 = p.Parse("{'first','second','third','fourth','fifth','second', 'first', 'third', 'third' }").Root as Ast.SetNode;
			Assert.AreEqual("'first'", set1.Elements[0].ToString());
			Assert.AreEqual("'second'", set1.Elements[1].ToString());
			Assert.AreEqual("'third'", set1.Elements[2].ToString());
			Assert.AreEqual("'fourth'", set1.Elements[3].ToString());
			Assert.AreEqual("'fifth'", set1.Elements[4].ToString());
			Assert.AreEqual(5, set1.Elements.Count);
		}
		
		[Test]
		public void TestDict()
		{
			Parser p = new Parser();
			Ast.DictNode dict1 = new Ast.DictNode();
			Ast.DictNode dict2 = new Ast.DictNode();
			Assert.AreEqual(dict1, dict2);
			
			Ast.KeyValueNode kv1 = new Ast.KeyValueNode { Key=new Ast.StringNode("key"), Value=new Ast.IntegerNode(42) };
			Ast.KeyValueNode kv2 = new Ast.KeyValueNode { Key=new Ast.StringNode("key"), Value=new Ast.IntegerNode(99) };
			Assert.AreNotEqual(kv1, kv2);
			kv2.Value = new Ast.IntegerNode(42);
			Assert.AreEqual(kv1, kv2);
			
			dict1.Elements.Add(new Ast.KeyValueNode { Key=new Ast.StringNode("key1"), Value=new Ast.IntegerNode(42) });
			dict2.Elements.Add(new Ast.KeyValueNode { Key=new Ast.StringNode("key1"), Value=new Ast.IntegerNode(99) });
			Assert.AreNotEqual(dict1, dict2);
			dict2.Elements.Clear();
			dict2.Elements.Add(new Ast.KeyValueNode { Key=new Ast.StringNode("key1"), Value=new Ast.IntegerNode(42) });
			Assert.AreEqual(dict1, dict2);
			
			dict2.Elements.Add(new Ast.KeyValueNode { Key=new Ast.StringNode("key2"), Value=new Ast.IntegerNode(43) });
			dict2.Elements.Add(new Ast.KeyValueNode { Key=new Ast.StringNode("key3"), Value=new Ast.IntegerNode(44) });
			Assert.AreNotEqual(dict1, dict2);
			
			Assert.AreEqual(new Ast.DictNode(), p.Parse("{}").Root);
			Assert.AreEqual(dict1, p.Parse("{'key1': 42}").Root);
			Assert.Throws<ParseException>(()=>p.Parse("{'key1': 42,}"));
			Assert.AreEqual(dict2, p.Parse("{'key1': 42, 'key2': 43, 'key3':44}").Root);

			Assert.Throws<ParseException>(()=>p.Parse("{'key': 42]"));
			Assert.Throws<ParseException>(()=>p.Parse("{}@"));
			Assert.Throws<ParseException>(()=>p.Parse("{'key': 42}@"));
			
			dict1 = p.Parse("{'a': 1, 'b': 2, 'c': 3, 'c': 4, 'c': 5, 'c': 6}").Root as Ast.DictNode;
			Assert.AreEqual("'a':1", dict1.Elements[0].ToString());
			Assert.AreEqual("'b':2", dict1.Elements[1].ToString());
			Assert.AreEqual("'c':6", dict1.Elements[2].ToString());
			Assert.AreEqual(3, dict1.Elements.Count);
		}		
		
		[Test]
		public void TestFile()
		{
			Parser p = new Parser();
			byte[] ser=File.ReadAllBytes("testserpent.utf8.bin");
			Ast ast = p.Parse(ser);
			
			string expr = ast.ToString();
			Ast ast2 = p.Parse(expr);
			string expr2 = ast2.ToString();
			Assert.AreEqual(expr, expr2);
			
			StringBuilder sb= new StringBuilder();
			Walk(ast.Root, sb);
			string walk1 = sb.ToString();
			sb= new StringBuilder();
			Walk(ast2.Root, sb);
			string walk2 = sb.ToString();
			Assert.AreEqual(walk1, walk2);
			
			// @TODO Assert.AreEqual(ast.Root, ast2.Root);
			ast = p.Parse(expr2);
			// @TODO Assert.AreEqual(ast.Root, ast2.Root);
		}

		[Test]
		[Ignore("can't yet get the ast to compare equal on mono")]
		public void TestAstEquals()
		{
			Parser p = new Parser ();
			byte[] ser = File.ReadAllBytes ("testserpent.utf8.bin");
			Ast ast = p.Parse(ser);
			Ast ast2 = p.Parse(ser);
			Assert.AreEqual(ast.Root, ast2.Root);
		}

		public void Walk(Ast.INode node, StringBuilder sb)
		{
			if(node is Ast.SequenceNode)
			{
				sb.AppendLine(string.Format("{0} (seq)", node.GetType()));
				Ast.SequenceNode seq = (Ast.SequenceNode)node;
				foreach(Ast.INode child in seq.Elements) {
					Walk(child, sb);
				}
			}
			else
				sb.AppendLine(string.Format("{0} = {1}", node.GetType(), node.ToString()));
		}
	}

	[TestFixture]
	public class VisitorTest
	{
		[Test]
		public void TestObjectify()
		{
			Parser p = new Parser();
			byte[] ser=File.ReadAllBytes("testserpent.utf8.bin");
			Ast ast = p.Parse(ser);
			var visitor = new ObjectifyVisitor();
			ast.Accept(visitor);
			object thing = visitor.GetObject();
			
			IDictionary dict = (IDictionary) thing;
			Assert.AreEqual(11, dict.Count);
			IList<object> list = dict["numbers"] as IList<object>;
			Assert.AreEqual(4, list.Count);
			Assert.AreEqual(999.1234, list[1]);
			Assert.AreEqual(new ComplexNumber(-3, 8), list[3]);
			string euro = dict["unicode"] as string;
			Assert.AreEqual("\u20ac", euro);
			IDictionary exc = (IDictionary)dict["exc"];
			object[] args = (object[]) exc["args"];
			Assert.AreEqual("fault", args[0]);
			Assert.AreEqual("ZeroDivisionError", exc["__class__"]);
		}
		
		object ZerodivisionFromDict(IDictionary dict)
		{
			string classname = (string)dict["__class__"];
			if(classname=="ZeroDivisionError")
			{
				object[] args = (object[]) dict["args"];
				return new DivideByZeroException((string)args[0]);
			}
			return null;
		}
		
		[Test]
		public void TestObjectifyDictToClass()
		{
			Parser p = new Parser();
			byte[] ser=File.ReadAllBytes("testserpent.utf8.bin");
			Ast ast = p.Parse(ser);
			
			var visitor = new ObjectifyVisitor(ZerodivisionFromDict);
			ast.Accept(visitor);
			object thing = visitor.GetObject();
			
			IDictionary dict = (IDictionary) thing;
			Assert.AreEqual(11, dict.Count);
			DivideByZeroException ex = (DivideByZeroException) dict["exc"];
			Assert.AreEqual("fault", ex.Message);
			
			thing = ast.GetData(ZerodivisionFromDict);
			dict = (IDictionary) thing;
			Assert.AreEqual(11, dict.Count);
			ex = (DivideByZeroException) dict["exc"];
			Assert.AreEqual("fault", ex.Message);
		}
	}
}