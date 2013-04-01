/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent.test;

import static org.junit.Assert.*;

import java.io.BufferedInputStream;
import java.io.ByteArrayInputStream;
import java.io.DataInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.util.List;
import java.util.Map;

import org.junit.Test;

import net.razorvine.serpent.ComplexNumber;
import net.razorvine.serpent.ObjectifyVisitor;
import net.razorvine.serpent.Parser;
import net.razorvine.serpent.ast.*;


public class ParserTest
{
	@Test
	public void TestBasic()
	{
		Parser p = new Parser();
		assertNull(p.parse((String)null).root);
		assertNull(p.parse("").root);
		assertNotNull(p.parse("# comment\n42\n").root);
	}

	@Test
	public void TestComments()
	{
		Parser p = new Parser();

		Ast ast = p.parse("[ 1, 2 ]");  // no header whatsoever
		ObjectifyVisitor visitor = new ObjectifyVisitor();
		ast.accept(visitor);
		Object obj = visitor.getObject();
		assertEquals(new int[] {1,2}, obj);

		ast = p.parse(@"# serpent utf-8 python2.7
[ 1, 2,
   # some comments here
   3, 4]    # more here
# and here.
");			
		visitor = new ObjectifyVisitor();
		ast.Accept(visitor);
		obj = visitor.GetObject();
		assertEquals(new int[] {1,2,3,4}, obj);
	}

	@Test
	public void TestPrimitives()
	{
		Parser p = new Parser();
		assertEquals(new IntegerNode(42), p.parse("42").root);
		assertEquals(new IntegerNode(-42), p.parse("-42").root);
		assertEquals(new DoubleNode(42.331), p.parse("42.331").root);
		assertEquals(new DoubleNode(-42.331), p.parse("-42.331").root);
		assertEquals(new DoubleNode(-1.2e19), p.parse("-1.2e+19").root);
		assertEquals(new DoubleNode(0.0004), p.parse("4e-4").root);
		assertEquals(new DoubleNode(40000), p.parse("4e4").root);
		assertEquals(new BooleanNode(true), p.parse("True").root);
		assertEquals(new BooleanNode(false), p.parse("False").root);
		assertEquals(NoneNode.Instance, p.parse("None").root);
		
		// long ints
		assertEquals(new DecimalNode(123456789123456789123456789M), p.parse("123456789123456789123456789").root);
		Assert.AreNotEqual(new LongNode(52), p.parse("52").root);
		assertEquals(new LongNode(123456789123456789L), p.parse("123456789123456789").root);
		Assert.Throws<ParseException>(()=>p.parse("123456789123456789123456789123456789")); // overflow
	}
	
	@Test
	public void TestEquality()
	{
		INode n1, n2;

		n1 = new IntegerNode(42);
		n2 = new IntegerNode(42);
		assertEquals(n1, n2);
		n2 = new IntegerNode(43);
		Assert.AreNotEqual(n1, n2);
		
		n1 = new StringNode("foo");
		n2 = new StringNode("foo");
		assertEquals(n1, n2);
		n2 = new StringNode("bar");
		Assert.AreNotEqual(n1, n2);
		
		n1 = new ComplexNumberNode() {
			Real=1.1,
			Imaginary=2.2
		};
		n2 = new ComplexNumberNode() {
			Real=1.1,
			Imaginary=2.2
		};
		assertEquals(n1, n2);
		n2 = new ComplexNumberNode() {
			Real=1.1,
			Imaginary=3.3
		};
		Assert.AreNotEqual(n1, n2);
		
		n1=new KeyValueNode() {
			Key=new IntegerNode(42),
			Value=new IntegerNode(42)
		};
		n2=new KeyValueNode() {
			Key=new IntegerNode(42),
			Value=new IntegerNode(42)
		};
		assertEquals(n1, n2);
		n1=new KeyValueNode() {
			Key=new IntegerNode(43),
			Value=new IntegerNode(43)
		};
		Assert.AreNotEqual(n1,n2);
		
		n1=NoeNode.Instance;
		n2=NoeNode.Instance;
		assertEquals(n1, n2);
		n2=new IntegerNode(42);
		Assert.AreNotEqual(n1, n2);
		
		n1=new DictNode() {
			Elements=new List<INode>() {
				new KeyValueNode() {
					Key=new IntegerNode(42),
					Value=new IntegerNode(42)
				}
			}
		};
		n2=new DictNode() {
			Elements=new List<INode>() {
				new KeyValueNode() {
					Key=new IntegerNode(42),
					Value=new IntegerNode(42)
				}
			}
		};
		assertEquals(n1, n2);
		n2=new DictNode() {
			Elements=new List<INode>() {
				new KeyValueNode() {
					Key=new IntegerNode(42),
					Value=new IntegerNode(43)
				}
			}
		};
		Assert.AreNotEqual(n1, n2);
		
		n1=new ListNode() {
			Elements=new List<INode>() {
				new IntegerNode(42)
			}
		};
		n2=new ListNode() {
			Elements=new List<INode>() {
				new IntegerNode(42)
			}
		};
		assertEquals(n1,n2);
		n2=new ListNode() {
			Elements=new List<INode>() {
				new IntegerNode(43)
			}
		};
		Assert.AreNotEqual(n1,n2);
		
		n1=new SetNode() {
			Elements=new List<INode>() {
				new IntegerNode(42)
			}
		};
		n2=new SetNode() {
			Elements=new List<INode>() {
				new IntegerNode(42)
			}
		};
		assertEquals(n1,n2);
		n2=new SetNode() {
			Elements=new List<INode>() {
				new IntegerNode(43)
			}
		};
		Assert.AreNotEqual(n1,n2);
		
		n1=new TupleNode() {
			Elements=new List<INode>() {
				new IntegerNode(42)
			}
		};
		n2=new TupleNode() {
			Elements=new List<INode>() {
				new IntegerNode(42)
			}
		};
		assertEquals(n1,n2);
		n2=new TupleNode() {
			Elements=new List<INode>() {
				new IntegerNode(43)
			}
		};
		Assert.AreNotEqual(n1,n2);
		
	}
	
	@Test
	public void TestPrintSingle()
	{
		Parser p = new Parser();
		
		// primitives
		assertEquals("42", p.parse("42").root.toString());
		assertEquals("-42.331", p.parse("-42.331").root.toString());
		assertEquals("-42.0", p.parse("-42.0").root.toString());
		assertEquals("-2E+20", p.parse("-2E20").root.toString());
		assertEquals("2.0", p.parse("2.0").root.toString());
		assertEquals("1.2E+19", p.parse("1.2e19").root.toString());
		assertEquals("True", p.parse("True").root.toString());
		assertEquals("'hello'", p.parse("'hello'").root.toString());
		assertEquals("'\\n'", p.parse("'\n'").root.toString());
		assertEquals("'\\''", p.parse("'\\''").root.toString());
		assertEquals("'\"'", p.parse("'\\\"'").root.toString());
		assertEquals("'\"'", p.parse("'\"'").root.toString());
		assertEquals("'\\\\'", p.parse("'\\\\'").root.toString());
		assertEquals("None", p.parse("None").root.toString());
		string ustr = "'\u20ac\u2603'";
		assertEquals(ustr, p.parse(ustr).root.toString());
		
		// complex
		assertEquals("(0+2j)", p.parse("2j").root.toString());
		assertEquals("(-1.1-2.2j)", p.parse("(-1.1-2.2j)").root.toString());
		assertEquals("(1.1+2.2j)", p.parse("(1.1+2.2j)").root.toString());
		
		// long int
		assertEquals("123456789123456789123456789", p.parse("123456789123456789123456789").root.toString());
	}
	
	@Test
	public void TestPrintSeq()
	{
		Parser p=new Parser();
		
		//tuple
		assertEquals("()", p.parse("()").root.toString());
		assertEquals("(42,)", p.parse("(42,)").root.toString());
		assertEquals("(42,43)", p.parse("(42,43)").root.toString());

		// list			
		assertEquals("[]", p.parse("[]").root.toString());
		assertEquals("[42]", p.parse("[42]").root.toString());
		assertEquals("[42,43]", p.parse("[42,43]").root.toString());

		// set			
		assertEquals("{42}", p.parse("{42}").root.toString());
		assertEquals("{42,43}", p.parse("{42,43,43,43}").root.toString());

		// dict			
		assertEquals("{}", p.parse("{}").root.toString());
		assertEquals("{'a':42}", p.parse("{'a': 42}").root.toString());
		assertEquals("{'a':42,'b':43}", p.parse("{'a': 42, 'b': 43}").root.toString());
		assertEquals("{'a':42,'b':45}", p.parse("{'a': 42, 'b': 43, 'b': 44, 'b': 45}").root.toString());
	}
	
	@Test
	public void TestInvalidPrimitives()
	{
		Parser p = new Parser();
		Assert.Throws<ParseException>(()=>p.parse("1+2"));
		Assert.Throws<ParseException>(()=>p.parse("1-2"));
		Assert.Throws<ParseException>(()=>p.parse("1.1+2.2"));
		Assert.Throws<ParseException>(()=>p.parse("1.1-2.2"));
		Assert.Throws<ParseException>(()=>p.parse("True+2"));
		Assert.Throws<ParseException>(()=>p.parse("False-2"));
		Assert.Throws<ParseException>(()=>p.parse("3j+2"));
		Assert.Throws<ParseException>(()=>p.parse("3j-2"));
		Assert.Throws<ParseException>(()=>p.parse("None+2"));
		Assert.Throws<ParseException>(()=>p.parse("None-2"));
	}
	
	@Test
	public void TestComplex()
	{
		Parser p = new Parser();
		var cplx = new ComplexNumberNode() {
			Real = 4.2,
			Imaginary = 3.2
		};
		var cplx2 = new ComplexNumberNode() {
			Real = 4.2,
			Imaginary = 99
		};
		Assert.AreNotEqual(cplx, cplx2);
		cplx2.Imaginary = 3.2;
		assertEquals(cplx, cplx2);

		assertEquals(cplx, p.parse("(4.2+3.2j)").root);
		cplx.Real = 0;
		assertEquals(cplx, p.parse("(0+3.2j)").root);
		assertEquals(cplx, p.parse("3.2j").root);
		assertEquals(cplx, p.parse("+3.2j").root);
		cplx.Imaginary = -3.2;
		assertEquals(cplx, p.parse("-3.2j").root);
		cplx.Real = -9.9;
		assertEquals(cplx, p.parse("(-9.9-3.2j)").root);
	}
	
	@Test
	public void TestPrimitivesStuffAtEnd()
	{
		Parser p = new Parser();
		assertEquals(new IntegerNode(42), p.ParseSingle(new SeekableStringReader("42@")));
		assertEquals(new DoubleNode(42.331), p.ParseSingle(new SeekableStringReader("42.331@")));
		assertEquals(new BooleanNode(true), p.ParseSingle(new SeekableStringReader("True@")));
		assertEquals(NoeNode.Instance, p.ParseSingle(new SeekableStringReader("None@")));
		var cplx = new ComplexNumberNode() {
			Real = 4,
			Imaginary = 3
		};
		assertEquals(cplx, p.ParseSingle(new SeekableStringReader("(4+3j)@")));
		cplx.Real=0;
		assertEquals(cplx, p.ParseSingle(new SeekableStringReader("3j@")));
	}
	
	@Test
	public void TestStrings()
	{
		Parser p = new Parser();
		assertEquals(new StringNode("hello"), p.parse("'hello'").root);
		assertEquals(new StringNode("hello"), p.parse("\"hello\"").root);
		assertEquals(new StringNode("\\"), p.parse("'\\\\'").root);
		assertEquals(new StringNode("\\"), p.parse("\"\\\\\"").root);
		assertEquals(new StringNode("'"), p.parse("\"'\"").root);
		assertEquals(new StringNode("\""), p.parse("'\"'").root);
		assertEquals(new StringNode("tab\tnewline\n."), p.parse("'tab\\tnewline\\n.'").root);
	}
	
	@Test
	public void TestUnicode()
	{
		Parser p = new Parser();
		string str = "'\u20ac\u2603'";
		assertEquals(0x20ac, str[1]);
		assertEquals(0x2603, str[2]);
		byte[] bytes = Encoding.UTF8.GetBytes(str);
		
		string value = "\u20ac\u2603";
		assertEquals(new StringNode(value), p.parse(str).root);
		assertEquals(new StringNode(value), p.parse(bytes).root);
	}
	
	@Test
	public void TestWhitespace()
	{
		Parser p = new Parser();
		assertEquals(new IntegerNode(42), p.parse(" 42 ").root);
		assertEquals(new IntegerNode(42), p.parse("  42  ").root);
		assertEquals(new IntegerNode(42), p.parse("\t42\r\n").root);
		assertEquals(new IntegerNode(42), p.parse(" \t 42 \r \n ").root);
		assertEquals(new StringNode("   string value    "), p.parse("  '   string value    '   ").root);
		Assert.Throws<ParseException>(()=>p.parse("     (  42  ,  ( 'x',   'y'  )   "));  // missing tuple close )
		Ast ast = p.parse("     (  42  ,  ( 'x',   'y'  )  )  ");
		Ast.TupleNode tuple = (Ast.TupleNode) ast.root;
		assertEquals(new IntegerNode(42), tuple.Elements[0]);
		tuple = (Ast.TupleNode) tuple.Elements[1];
		assertEquals(new StringNode("x"), tuple.Elements[0]);
		assertEquals(new StringNode("y"), tuple.Elements[1]);
		
		p.parse(" ( 52 , ) ");
		p.parse(" [ 52 ] ");
		p.parse(" { 'a' : 42 } ");
		p.parse(" { 52 } ");
	}
	
	@Test
	public void TestTuple()
	{
		Parser p = new Parser();
		Ast.TupleNode tuple = new TupleNode();
		Ast.TupleNode tuple2 = new TupleNode();
		assertEquals(tuple, tuple2);
		
		tuple.Elements.Add(new IntegerNode(42));
		tuple2.Elements.Add(new IntegerNode(99));
		Assert.AreNotEqual(tuple, tuple2);
		tuple2.Elements.Clear();
		tuple2.Elements.Add(new IntegerNode(42));
		assertEquals(tuple, tuple2);
		tuple2.Elements.Add(new IntegerNode(43));
		tuple2.Elements.Add(new IntegerNode(44));
		Assert.AreNotEqual(tuple, tuple2);
		
		assertEquals(new TupleNode(), p.parse("()").root);
		assertEquals(tuple, p.parse("(42,)").root);
		assertEquals(tuple2, p.parse("( 42,43, 44 )").root);

		Assert.Throws<ParseException>(()=>p.parse("(42,43]"));
		Assert.Throws<ParseException>(()=>p.parse("()@"));
		Assert.Throws<ParseException>(()=>p.parse("(42,43)@"));
	}
	
	@Test
	public void TestList()
	{
		Parser p = new Parser();
		Ast.ListNode list = new ListNode();
		Ast.ListNode list2 = new ListNode();
		assertEquals(list, list2);
		
		list.Elements.Add(new IntegerNode(42));
		list2.Elements.Add(new IntegerNode(99));
		Assert.AreNotEqual(list, list2);
		list2.Elements.Clear();
		list2.Elements.Add(new IntegerNode(42));
		assertEquals(list, list2);
		list2.Elements.Add(new IntegerNode(43));
		list2.Elements.Add(new IntegerNode(44));
		Assert.AreNotEqual(list, list2);
		
		assertEquals(new ListNode(), p.parse("[]").root);
		assertEquals(list, p.parse("[42]").root);
		Assert.Throws<ParseException>(()=>p.parse("[42,]"));
		assertEquals(list2, p.parse("[ 42,43, 44 ]").root);

		Assert.Throws<ParseException>(()=>p.parse("[42,43}"));
		Assert.Throws<ParseException>(()=>p.parse("[]@"));
		Assert.Throws<ParseException>(()=>p.parse("[42,43]@"));
	}
	
	@Test
	public void TestSet()
	{
		Parser p = new Parser();
		Ast.SetNode set1 = new SetNode();
		Ast.SetNode set2 = new SetNode();
		assertEquals(set1, set2);
		
		set1.Elements.Add(new IntegerNode(42));
		set2.Elements.Add(new IntegerNode(99));
		Assert.AreNotEqual(set1, set2);
		set2.Elements.Clear();
		set2.Elements.Add(new IntegerNode(42));
		assertEquals(set1, set2);
		
		set2.Elements.Add(new IntegerNode(43));
		set2.Elements.Add(new IntegerNode(44));
		Assert.AreNotEqual(set1, set2);
		
		assertEquals(set1, p.parse("{42}").root);
		Assert.Throws<ParseException>(()=>p.parse("{42,}"));
		assertEquals(set2, p.parse("{ 42,43, 44 }").root);

		Assert.Throws<ParseException>(()=>p.parse("{42,43]"));
		Assert.Throws<ParseException>(()=>p.parse("{42,43}@"));
		
		set1 = p.parse("{'first','second','third','fourth','fifth','second', 'first', 'third', 'third' }").root as Ast.SetNode;
		assertEquals("'first'", set1.Elements[0].toString());
		assertEquals("'second'", set1.Elements[1].toString());
		assertEquals("'third'", set1.Elements[2].toString());
		assertEquals("'fourth'", set1.Elements[3].toString());
		assertEquals("'fifth'", set1.Elements[4].toString());
		assertEquals(5, set1.Elements.Count);
	}
	
	@Test
	public void TestDict()
	{
		Parser p = new Parser();
		Ast.DictNode dict1 = new DictNode();
		Ast.DictNode dict2 = new DictNode();
		assertEquals(dict1, dict2);
		
		Ast.KeyValueNode kv1 = new KeyValueNode { Key=new StringNode("key"), Value=new IntegerNode(42) };
		Ast.KeyValueNode kv2 = new KeyValueNode { Key=new StringNode("key"), Value=new IntegerNode(99) };
		Assert.AreNotEqual(kv1, kv2);
		kv2.Value = new IntegerNode(42);
		assertEquals(kv1, kv2);
		
		dict1.Elements.Add(new KeyValueNode { Key=new StringNode("key1"), Value=new IntegerNode(42) });
		dict2.Elements.Add(new KeyValueNode { Key=new StringNode("key1"), Value=new IntegerNode(99) });
		Assert.AreNotEqual(dict1, dict2);
		dict2.Elements.Clear();
		dict2.Elements.Add(new KeyValueNode { Key=new StringNode("key1"), Value=new IntegerNode(42) });
		assertEquals(dict1, dict2);
		
		dict2.Elements.Add(new KeyValueNode { Key=new StringNode("key2"), Value=new IntegerNode(43) });
		dict2.Elements.Add(new KeyValueNode { Key=new StringNode("key3"), Value=new IntegerNode(44) });
		Assert.AreNotEqual(dict1, dict2);
		
		assertEquals(new DictNode(), p.parse("{}").root);
		assertEquals(dict1, p.parse("{'key1': 42}").root);
		Assert.Throws<ParseException>(()=>p.parse("{'key1': 42,}"));
		assertEquals(dict2, p.parse("{'key1': 42, 'key2': 43, 'key3':44}").root);

		Assert.Throws<ParseException>(()=>p.parse("{'key': 42]"));
		Assert.Throws<ParseException>(()=>p.parse("{}@"));
		Assert.Throws<ParseException>(()=>p.parse("{'key': 42}@"));
		
		dict1 = p.parse("{'a': 1, 'b': 2, 'c': 3, 'c': 4, 'c': 5, 'c': 6}").root as Ast.DictNode;
		assertEquals("'a':1", dict1.Elements[0].toString());
		assertEquals("'b':2", dict1.Elements[1].toString());
		assertEquals("'c':6", dict1.Elements[2].toString());
		assertEquals(3, dict1.Elements.Count);
	}		
	
	@Test
	public void TestFile()
	{
		Parser p = new Parser();
		byte[] ser=File.ReadAllBytes("testserpent.utf8.bin");
		Ast ast = p.parse(ser);
		
		string expr = ast.toString();
		Ast ast2 = p.parse(expr);
		string expr2 = ast2.toString();
		assertEquals(expr, expr2);
		
		StringBuilder sb= new StringBuilder();
		Walk(ast.root, sb);
		string walk1 = sb.toString();
		sb= new StringBuilder();
		Walk(ast2.root, sb);
		string walk2 = sb.toString();
		assertEquals(walk1, walk2);
		
		assertEquals(ast.root, ast2.root);
		ast = p.parse(expr2);
		assertEquals(ast.root, ast2.root);
	}
	
	public void Walk(INode node, StringBuilder sb)
	{
		if(node is Ast.SequenceNode)
		{
			sb.AppendLine(string.Format("{0} (seq)", node.GetType()));
			Ast.SequenceNode seq = (Ast.SequenceNode)node;
			foreach(INode child in seq.Elements) {
				Walk(child, sb);
			}
		}
		else
			sb.AppendLine(string.Format("{0} = {1}", node.GetType(), node.toString()));
	}
}
