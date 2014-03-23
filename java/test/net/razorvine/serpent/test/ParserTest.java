/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent.test;

import static org.junit.Assert.*;

import java.io.DataInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.LinkedList;
import java.util.List;

import net.razorvine.serpent.ObjectifyVisitor;
import net.razorvine.serpent.ParseException;
import net.razorvine.serpent.Parser;
import net.razorvine.serpent.SeekableStringReader;
import net.razorvine.serpent.ast.*;

import org.junit.Ignore;
import org.junit.Test;


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
		
		List<Integer> expected = new ArrayList<Integer>();
		expected.add(1);
		expected.add(2);
		assertEquals(expected, obj);
		expected = new LinkedList<Integer>();
		expected.add(1);
		expected.add(2);
		assertEquals(expected, obj);

		ast = p.parse("# serpent utf-8 python2.7\n"+
"[ 1, 2,\n"+
"   # some comments here\n"+
"   3, 4]    # more here\n"+
"# and here.\n");
		visitor = new ObjectifyVisitor();
		ast.accept(visitor);
		obj = visitor.getObject();
		expected = new LinkedList<Integer>();
		expected.add(1);
		expected.add(2);
		expected.add(3);
		expected.add(4);
		assertEquals(expected, obj);
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
		assertEquals(new BigIntNode(new BigInteger("123456789123456789123456789")), p.parse("123456789123456789123456789").root);
		assertFalse(new LongNode(52).equals(p.parse("52").root));
		assertEquals(new LongNode(123456789123456789L), p.parse("123456789123456789").root);
		
		assertEquals(BigIntNode.class, p.parse("12345678912345678912345678912345678978571892375798273578927389758378467693485903859038593453475897349587348957893457983475983475893475893785732957398475").root.getClass());
	}
	
	@Test
	public void TestEquality()
	{
		INode n1, n2;

		n1 = new IntegerNode(42);
		n2 = new IntegerNode(42);
		assertEquals(n1, n2);
		n2 = new IntegerNode(43);
		assertFalse(n1.equals(n2));
		
		n1 = new StringNode("foo");
		n2 = new StringNode("foo");
		assertEquals(n1, n2);
		n2 = new StringNode("bar");
		assertFalse(n1.equals(n2));
		
		ComplexNumberNode cn1 = new ComplexNumberNode();
		cn1.real=1.1;  cn1.imaginary=2.2;
		ComplexNumberNode cn2 = new ComplexNumberNode();
		cn2.real=1.1;  cn2.imaginary=2.2;
		assertEquals(cn1, cn2);
		cn2 = new ComplexNumberNode();
		cn2.real=1.1; cn2.imaginary=3.3;
		assertFalse(cn1.equals(cn2));

		KeyValueNode kvn1=new KeyValueNode();
		kvn1.key = new IntegerNode(42);
		kvn1.value = new IntegerNode(42);
		KeyValueNode kvn2=new KeyValueNode();
		kvn2.key=new IntegerNode(42);
		kvn2.value=new IntegerNode(42);
		assertEquals(kvn1, kvn2);
		kvn1=new KeyValueNode();
		kvn1.key=new IntegerNode(43);
		kvn1.value=new IntegerNode(43);
		assertFalse(kvn1.equals(kvn2));
		
		n1=NoneNode.Instance;
		n2=NoneNode.Instance;
		assertEquals(n1, n2);
		n2=new IntegerNode(42);
		assertFalse(n1.equals(n2));
		
		DictNode dn1 = new DictNode();
		kvn1 = new KeyValueNode();
		kvn1.key = new IntegerNode(42);
		kvn1.value = new IntegerNode(42);
		dn1.elements.add(kvn1);
		DictNode dn2=new DictNode();
		kvn1 = 	new KeyValueNode();
		kvn1.key =new IntegerNode(42);
		kvn1.value = new IntegerNode(42);
		dn2.elements.add(kvn1);
		assertEquals(dn1, dn2);

		dn2=new DictNode();
		kvn1 = new KeyValueNode();
		kvn1.key = new IntegerNode(42);
		kvn1.value = new IntegerNode(43);
		dn2.elements.add(kvn1);
		assertFalse(dn1.equals(dn2));
		
		ListNode ln1=new ListNode();
		ln1.elements.add(new IntegerNode(42));
		ListNode ln2=new ListNode();
		ln2.elements.add(new IntegerNode(42));
		assertEquals(ln1,ln2);
		ln2=new ListNode();
		ln2.elements.add(new IntegerNode(43));
		assertFalse(ln1.equals(ln2));
		
		SetNode sn1=new SetNode();
		sn1.elements.add(new IntegerNode(42));
		SetNode sn2=new SetNode();
		sn2.elements.add(new IntegerNode(42));
		assertEquals(sn1,sn2);
		sn2=new SetNode();
		sn2.elements.add(new IntegerNode(43));
		assertFalse(sn1.equals(sn2));
		
		TupleNode tn1=new TupleNode();
		tn1.elements.add(new IntegerNode(42));
		TupleNode tn2=new TupleNode();
		tn2.elements.add(new IntegerNode(42));
		assertEquals(tn1,tn2);
		tn2=new TupleNode();
		tn2.elements.add(new IntegerNode(43));
		assertFalse(tn1.equals(tn2));
		
	}
	
	@Test
	public void TestPrintSingle()
	{
		Parser p = new Parser();
		
		// primitives
		assertEquals("42", p.parse("42").root.toString());
		assertEquals("-42.331", p.parse("-42.331").root.toString());
		assertEquals("-42.0", p.parse("-42.0").root.toString());
		assertEquals("-2.0E20", p.parse("-2E20").root.toString());
		assertEquals("2.0", p.parse("2.0").root.toString());
		assertEquals("1.2E19", p.parse("1.2e19").root.toString());
		assertEquals("True", p.parse("True").root.toString());
		assertEquals("'hello'", p.parse("'hello'").root.toString());
		assertEquals("'\\n'", p.parse("'\n'").root.toString());
		assertEquals("'\\''", p.parse("'\\''").root.toString());
		assertEquals("'\"'", p.parse("'\\\"'").root.toString());
		assertEquals("'\"'", p.parse("'\"'").root.toString());
		assertEquals("'\\\\'", p.parse("'\\\\'").root.toString());
		assertEquals("None", p.parse("None").root.toString());
		String ustr = "'\u20ac\u2603'";
		assertEquals(ustr, p.parse(ustr).root.toString());
		
		// complex
		assertEquals("(0.0+2.0j)", p.parse("2j").root.toString());
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
		
		String result = p.parse("{'a': 42, 'b': 43}").root.toString();
		assertTrue(result.equals("{'a':42,'b':43}") || result.equals("{'b':43,'a':42}"));
		result = p.parse("{'a': 42, 'b': 43, 'b': 44, 'b': 45}").root.toString();
		assertTrue(result.equals("{'a':42,'b':45}") || result.equals("{'b':45,'a':42}"));
	}
	
	@Test(expected=ParseException.class)
	public void TestInvalidPrimitive1()
	{
		new Parser().parse("1+2");
	}
	
	@Test(expected=ParseException.class)
	public void TestInvalidPrimitive2()
	{
		new Parser().parse("1-2");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidPrimitive3()
	{
		new Parser().parse("1.1+2.2");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidPrimitive4()
	{
		new Parser().parse("1.1-2.2");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidPrimitive5()
	{
		new Parser().parse("True+2");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidPrimitive6()
	{
		new Parser().parse("False-2");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidPrimitive7()
	{
		new Parser().parse("3j+2");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidPrimitive8()
	{
		new Parser().parse("3j-2");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidPrimitive9()
	{
		new Parser().parse("None+2");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidPrimitive10()
	{
		new Parser().parse("None-2");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidTuple1()
	{
		new Parser().parse("(42,43]");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidTuple2()
	{
		new Parser().parse("()@");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidTuple3()
	{
		new Parser().parse("(42,43)@");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidList1()
	{
		new Parser().parse("[42,]");
	}
			
	@Test(expected=ParseException.class)
	public void TestInvalidList2()
	{
		new Parser().parse("[42,43}");
	}
	
	@Test(expected=ParseException.class)
	public void TestInvalidList3()
	{
		new Parser().parse("[]@");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidList4()
	{
		new Parser().parse("[42,43]@");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidSet1()
	{
		new Parser().parse("{42,}");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidSet2()
	{
		new Parser().parse("{42,43]");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidSet3()
	{
		new Parser().parse("{42,43}@");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidDict1()
	{
		new Parser().parse("{'key1': 42,}");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidDict2()
	{
		new Parser().parse("{'key': 42]");
	}
	
	@Test(expected=ParseException.class)
	public void TestInvalidDict3()
	{
		new Parser().parse("{}@");
	}

	@Test(expected=ParseException.class)
	public void TestInvalidDict4()
	{
		new Parser().parse("{'key': 42}@");
	}
	
	@Test
	public void TestComplex()
	{
		Parser p = new Parser();
		ComplexNumberNode cplx = new ComplexNumberNode();
		cplx.real = 4.2;
		cplx.imaginary = 3.2;
		ComplexNumberNode cplx2 = new ComplexNumberNode();
		cplx2.real = 4.2;
		cplx2.imaginary = 99;
		assertFalse(cplx.equals(cplx2));
		cplx2.imaginary = 3.2;
		assertEquals(cplx, cplx2);

		assertEquals(cplx, p.parse("(4.2+3.2j)").root);
		cplx.real = 0;
		assertEquals(cplx, p.parse("(0+3.2j)").root);
		assertEquals(cplx, p.parse("3.2j").root);
		assertEquals(cplx, p.parse("+3.2j").root);
		cplx.imaginary = -3.2;
		assertEquals(cplx, p.parse("-3.2j").root);
		cplx.real = -9.9;
		assertEquals(cplx, p.parse("(-9.9-3.2j)").root);
	}
	
	@Test
	public void TestPrimitivesStuffAtEnd()
	{
		Parser p = new Parser();
		assertEquals(new IntegerNode(42), p.parseSingle(new SeekableStringReader("42@")));
		assertEquals(new DoubleNode(42.331), p.parseSingle(new SeekableStringReader("42.331@")));
		assertEquals(new BooleanNode(true), p.parseSingle(new SeekableStringReader("True@")));
		assertEquals(NoneNode.Instance, p.parseSingle(new SeekableStringReader("None@")));
		ComplexNumberNode cplx = new ComplexNumberNode();
		cplx.real=4;
		cplx.imaginary=3;
		assertEquals(cplx, p.parseSingle(new SeekableStringReader("(4+3j)@")));
		cplx.real=0;
		assertEquals(cplx, p.parseSingle(new SeekableStringReader("3j@")));
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
	public void TestUnicode() throws UnsupportedEncodingException
	{
		Parser p = new Parser();
		String str = "'\u20ac\u2603'";
		assertEquals(0x20ac, str.charAt(1));
		assertEquals(0x2603, str.charAt(2));
		byte[] bytes = str.getBytes("utf-8");
		
		String value = "\u20ac\u2603";
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
		try {
			p.parse("     (  42  ,  ( 'x',   'y'  )   ");  // missing tuple close )
			fail("expected parse error");
		} catch (ParseException x) {
			//ok
		}

		Ast ast = p.parse("     (  42  ,  ( 'x',   'y'  )  )  ");
		TupleNode tuple = (TupleNode) ast.root;
		assertEquals(new IntegerNode(42), tuple.elements.get(0));
		tuple = (TupleNode) tuple.elements.get(1);
		assertEquals(new StringNode("x"), tuple.elements.get(0));
		assertEquals(new StringNode("y"), tuple.elements.get(1));
		
		p.parse(" ( 52 , ) ");
		p.parse(" [ 52 ] ");
		p.parse(" { 'a' : 42 } ");
		p.parse(" { 52 } ");
	}
	
	@Test
	public void TestTuple()
	{
		Parser p = new Parser();
		TupleNode tuple = new TupleNode();
		TupleNode tuple2 = new TupleNode();
		assertEquals(tuple, tuple2);
		
		tuple.elements.add(new IntegerNode(42));
		tuple2.elements.add(new IntegerNode(99));
		assertFalse(tuple.equals(tuple2));
		tuple2.elements.clear();
		tuple2.elements.add(new IntegerNode(42));
		assertEquals(tuple, tuple2);
		tuple2.elements.add(new IntegerNode(43));
		tuple2.elements.add(new IntegerNode(44));
		assertFalse(tuple.equals(tuple2));
		
		assertEquals(new TupleNode(), p.parse("()").root);
		assertEquals(tuple, p.parse("(42,)").root);
		assertEquals(tuple2, p.parse("( 42,43, 44 )").root);
	}
	
	@Test
	public void TestList()
	{
		Parser p = new Parser();
		ListNode list = new ListNode();
		ListNode list2 = new ListNode();
		assertEquals(list, list2);
		
		list.elements.add(new IntegerNode(42));
		list2.elements.add(new IntegerNode(99));
		assertFalse(list.equals(list2));
		list2.elements.clear();
		list2.elements.add(new IntegerNode(42));
		assertEquals(list, list2);
		list2.elements.add(new IntegerNode(43));
		list2.elements.add(new IntegerNode(44));
		assertFalse(list.equals(list2));
		
		assertEquals(new ListNode(), p.parse("[]").root);
		assertEquals(list, p.parse("[42]").root);
		assertEquals(list2, p.parse("[ 42,43, 44 ]").root);

	}
	
	@Test
	public void TestSet()
	{
		Parser p = new Parser();
		SetNode set1 = new SetNode();
		SetNode set2 = new SetNode();
		assertEquals(set1, set2);
		
		set1.elements.add(new IntegerNode(42));
		set2.elements.add(new IntegerNode(99));
		assertFalse(set1.equals(set2));
		set2.elements.clear();
		set2.elements.add(new IntegerNode(42));
		assertEquals(set1, set2);
		
		set2.elements.add(new IntegerNode(43));
		set2.elements.add(new IntegerNode(44));
		assertFalse(set1.equals(set2));
		
		assertEquals(set1, p.parse("{42}").root);
		assertEquals(set2, p.parse("{ 42,43, 44 }").root);

		set1 = (SetNode) p.parse("{'first','second','third','fourth','fifth','second', 'first', 'third', 'third' }").root;
		assertTrue(set1.elements.contains(new StringNode("first")));
		assertTrue(set1.elements.contains(new StringNode("second")));
		assertTrue(set1.elements.contains(new StringNode("third")));
		assertTrue(set1.elements.contains(new StringNode("fourth")));
		assertTrue(set1.elements.contains(new StringNode("fifth")));
		assertEquals(5, set1.elements.size());
	}
	
	@Test
	public void TestDict()
	{
		Parser p = new Parser();
		DictNode dict1 = new DictNode();
		DictNode dict2 = new DictNode();
		assertEquals(dict1, dict2);
		
		KeyValueNode kv1 = new KeyValueNode();
		kv1.key = new StringNode("key");
		kv1.value = new IntegerNode(42);
		KeyValueNode kv2 = new KeyValueNode();
		kv2.key=new StringNode("key");
		kv2.value=new IntegerNode(99);
		assertFalse(kv1.equals(kv2));
		kv2.value = new IntegerNode(42);
		assertEquals(kv1, kv2);
		
		kv1=new KeyValueNode();
		kv1.key=new StringNode("key1");
		kv1.value=new IntegerNode(42);
		dict1.elements.add(kv1);
		kv1=new KeyValueNode();
		kv1.key=new StringNode("key1");
		kv1.value=new IntegerNode(99);
		dict2.elements.add(kv1);
		assertFalse(dict1.equals(dict2));
		
		dict2.elements.clear();
		
		kv1=new KeyValueNode();
		kv1.key=new StringNode("key1");
		kv1.value=new IntegerNode(42);
		dict2.elements.add(kv1);
		assertEquals(dict1, dict2);
		
		kv1=new KeyValueNode();
		kv1.key=new StringNode("key2");
		kv1.value=new IntegerNode(43);
		dict2.elements.add(kv1);
		kv1=new KeyValueNode();
		kv1.key=new StringNode("key3");
		kv1.value=new IntegerNode(44);
		dict2.elements.add(kv1);
		assertFalse(dict1.equals(dict2));
		
		assertEquals(new DictNode(), p.parse("{}").root);
		assertEquals(dict1, p.parse("{'key1': 42}").root);
		assertEquals(dict2, p.parse("{'key1': 42, 'key2': 43, 'key3':44}").root);

		dict1 = (DictNode) p.parse("{'a': 1, 'b': 2, 'c': 3, 'c': 4, 'c': 5, 'c': 6}").root;
		
		kv1 = new KeyValueNode();
		kv1.key=new StringNode("c");
		kv1.value=new IntegerNode(6);
		assertTrue(dict1.elements.contains(kv1));
		assertEquals(3, dict1.elements.size());
	}
	
	@Test
	public void TestKeyValueEquality()
	{
		KeyValueNode kv1=new KeyValueNode();
		kv1.key=new StringNode("key1");
		kv1.value=new IntegerNode(42);

		KeyValueNode kv2=new KeyValueNode();
		kv2.key=new StringNode("key1");
		kv2.value=new IntegerNode(42);
		
		assertEquals(kv1, kv2);
		kv2.value=new IntegerNode(43);
		assertFalse(kv1.equals(kv2));
	}
	
	@Test
	public void TestDictEquality()
	{
		DictNode dict1 = new DictNode();
		KeyValueNode kv=new KeyValueNode();
		kv.key=new StringNode("key1");
		kv.value=new IntegerNode(42);
		dict1.elements.add(kv);
		
		DictNode dict2 = new DictNode();
		kv=new KeyValueNode();
		kv.key=new StringNode("key1");
		kv.value=new IntegerNode(42);
		dict2.elements.add(kv);
		
		assertEquals(dict1, dict2);
		
		
		kv=new KeyValueNode();
		kv.key=new StringNode("key2");
		kv.value=new IntegerNode(43);
		dict1.elements.add(kv);
		kv=new KeyValueNode();
		kv.key=new StringNode("key3");
		kv.value=new IntegerNode(44);
		dict1.elements.add(kv);

		dict2 = new DictNode();
		kv=new KeyValueNode();
		kv.key=new StringNode("key2");
		kv.value=new IntegerNode(43);
		dict2.elements.add(kv);
		kv=new KeyValueNode();
		kv.key=new StringNode("key3");
		kv.value=new IntegerNode(44);
		dict2.elements.add(kv);
		kv=new KeyValueNode();
		kv.key=new StringNode("key1");
		kv.value=new IntegerNode(42);
		dict2.elements.add(kv);
		
		assertTrue(dict1.equals(dict2));
		assertEquals(dict1, dict2);
		kv=new KeyValueNode();
		kv.key=new StringNode("key4");
		kv.value=new IntegerNode(45);
		dict2.elements.add(kv);
		assertFalse(dict1.equals(dict2));
	}
	
	@Test
	public void TestSetEquality()
	{
		SetNode set1 = new SetNode();
		set1.elements.add(new IntegerNode(1));
		set1.elements.add(new IntegerNode(2));
		set1.elements.add(new IntegerNode(3));
		
		SetNode set2 = new SetNode();
		set2.elements.add(new IntegerNode(2));
		set2.elements.add(new IntegerNode(3));
		set2.elements.add(new IntegerNode(1));

		assertEquals(set1, set2);
		set2.elements.add(new IntegerNode(0));
		assertFalse(set1.equals(set2));
	}

	@Test
	public void TestFile() throws IOException
	{
		Parser p = new Parser();
		File testdatafile = new File("test/testserpent.utf8.bin");
		byte[] ser = new byte[(int) testdatafile.length()];
		FileInputStream fis=new FileInputStream(testdatafile);
		DataInputStream dis = new DataInputStream(fis);
		dis.readFully(ser);
		dis.close();
		fis.close();

		Ast ast = p.parse(ser);
	
		String expr = ast.toString();
		Ast ast2 = p.parse(expr);
		String expr2 = ast2.toString();
		assertEquals(expr.length(), expr2.length());
		
		StringBuilder sb= new StringBuilder();
		Walk(ast.root, sb);
		String walk1 = sb.toString();
		sb= new StringBuilder();
		Walk(ast2.root, sb);
		String walk2 = sb.toString();
		assertEquals(walk1.length(), walk2.length());
		
		// TODO assertEquals(ast.root, ast2.root);
		ast = p.parse(expr2);
		// TODO assertEquals(ast.root, ast2.root);
	}
	
	@Test
	@Ignore("can't get the ast compare to succeed :(")
	public void TestAstEquals() throws IOException
	{
		Parser p = new Parser();
		File testdatafile = new File("test/testserpent.utf8.bin");
		byte[] ser = new byte[(int) testdatafile.length()];
		FileInputStream fis=new FileInputStream(testdatafile);
		DataInputStream dis = new DataInputStream(fis);
		dis.readFully(ser);
		dis.close();
		fis.close();

		Ast ast = p.parse(ser);
		Ast ast2 = p.parse(ser);
		assertEquals(ast.root, ast2.root);	// TODO this fails :(
	}
	
	public void Walk(INode node, StringBuilder sb)
	{
		if(node instanceof SequenceNode)
		{
			sb.append(String.format("%s (seq)\n", node.getClass()));
			SequenceNode seq = (SequenceNode)node;
			for(INode child: seq.elements) {
				Walk(child, sb);
			}
		}
		else
			sb.append(String.format("  %s\n", node.toString()));
	}
}
