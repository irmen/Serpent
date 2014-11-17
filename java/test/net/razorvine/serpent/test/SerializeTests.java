/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent.test;

import static org.junit.Assert.*;

import java.io.UnsupportedEncodingException;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.*;

import net.razorvine.serpent.ComplexNumber;
import net.razorvine.serpent.IClassSerializer;
import net.razorvine.serpent.Parser;
import net.razorvine.serpent.Serializer;
import org.junit.Test;

public class SerializeTests {

	public byte[] strip_header(byte[] data)
	{
		int start;
		for(start=0; start<data.length; ++start)
		{
			if(data[start]==(byte)10)	// the newline after the header
				break;
		}

		if(start>=data.length)
		{
			throw new IllegalArgumentException("need header in string");
		}
		start++;
		byte[] result = new byte[data.length-start];
		System.arraycopy(data, start, result, 0, data.length-start);
		return result;
	}

	public byte[] B(String s)
	{
		try {
			return s.getBytes("utf-8");
		} catch (UnsupportedEncodingException e) {
			e.printStackTrace();
			return null;
		}
	}
	
	public String S(byte[] b)
	{
		try {
			return new String(b, "utf-8");
		} catch (UnsupportedEncodingException e) {
			e.printStackTrace();
			return null;
		}
	}


	@Test
	public void testHeader()
	{
		Serializer ser = new Serializer();
		byte[] data = ser.serialize(null);
		assertEquals(35, data[0]);
		String strdata = S(data);
		assertEquals("# serpent utf-8 python3.2", strdata.split("\n")[0]);
		
		ser.setliterals=false;
		data = ser.serialize(null);
		strdata = S(data);
		assertEquals("# serpent utf-8 python2.6", strdata.split("\n")[0]);

		data = B("# header\nfirst-line");
		data = strip_header(data);
		assertEquals("first-line", S(data));
	}
	

	@Test
	public void testException()
	{
		Serializer.registerClass(IllegalArgumentException.class, null);
		Exception x = new IllegalArgumentException("errormessage");
		Serializer serpent = new Serializer(true, true, false);
		byte[] ser = strip_header(serpent.serialize(x));
		assertEquals("{\n  '__class__': 'IllegalArgumentException',\n  '__exception__': True,\n  'args': (\n    'errormessage',\n  ),\n  'attributes': {}\n}", S(ser));
	}

	@Test
	public void testExceptionPackage()
	{
		Serializer.registerClass(IllegalArgumentException.class, null);
		Exception x = new IllegalArgumentException("errormessage");
		Serializer serpent = new Serializer(true, true, true);
		byte[] ser = strip_header(serpent.serialize(x));
		assertEquals("{\n  '__class__': 'java.lang.IllegalArgumentException',\n  '__exception__': True,\n  'args': (\n    'errormessage',\n  ),\n  'attributes': {}\n}", S(ser));
	}

	@Test
	public void testStuff()
	{
		Serializer ser=new Serializer();
		byte[] result = ser.serialize("blerp");
		result=strip_header(result);
		assertEquals("'blerp'", S(result));
		result = ser.serialize(UUID.fromString("f1f8d00e-49a5-4662-ac1d-d5f0426ed293"));
		result=strip_header(result);
		assertEquals("'f1f8d00e-49a5-4662-ac1d-d5f0426ed293'", S(result));
		result = ser.serialize(new BigDecimal("123456789.987654321987654321987654321987654321"));
		result=strip_header(result);
		assertEquals("'123456789.987654321987654321987654321987654321'", S(result));
	}


	@Test
	public void testNull()
	{
		Serializer ser = new Serializer();
		byte[] data = ser.serialize(null);
		data=strip_header(data);
		assertEquals("None", S(data));
	}

	
	@Test
	public void testStrings()
	{
		Serializer serpent = new Serializer();
		byte[] ser = serpent.serialize("hello");
		byte[] data = strip_header(ser);
		assertEquals("'hello'", S(data));
	  	ser = serpent.serialize("quotes'\"");
	  	data = strip_header(ser);
	  	assertEquals("'quotes\\'\"'", S(data));
	  	ser = serpent.serialize("quotes2'");
	  	data = strip_header(ser);
	  	assertEquals("\"quotes2'\"", S(data));
	}
	
	@Test
	public void testUnicode()
	{
		Serializer serpent = new Serializer();
		byte[] ser = serpent.serialize("euro\u20ac");
		byte[] data = strip_header(ser);
		assertArrayEquals(new byte[] {39, 101, 117, 114, 111, (byte) 0xe2, (byte) 0x82, (byte) 0xac, 39}, data);

		ser = serpent.serialize("A\n\t\\Z");
		// 'A\\n\\t\\\\Z'  (10 bytes)
		data = strip_header(ser);
		assertArrayEquals(new byte[] {39, 65, 92, 110, 92, 116, 92, 92, 90, 39}, data);
		
		ser = serpent.serialize("euro\u20ac\nlastline\ttab\\@slash");
		// 'euro\xe2\x82\xac\\nlastline\\ttab\\\\@slash'   (32 bytes)
		data = strip_header(ser);
		assertArrayEquals(new byte[] {
							39, 101, 117, 114, 111, (byte) 226, (byte) 130, (byte) 172,
							92, 110, 108, 97, 115, 116, 108, 105,
							110, 101, 92, 116, 116, 97, 98, 92,
							92, 64, 115, 108, 97, 115, 104, 39}
			                , data);
	}

	@Test
	public void testBool()
	{
		Serializer serpent = new Serializer();
		byte[] ser = serpent.serialize(true);
		byte[] data = strip_header(ser);
		assertEquals("True", S(data));
		ser = serpent.serialize(false);
		data = strip_header(ser);
		assertEquals("False", S(data));
	}


	@Test
	public void testBytes()
	{
		Serializer serpent = new Serializer(true, true, false);
		byte[] bytes = new byte[] { 97, 98, 99, 100, 101, 102 };	// abcdef
		byte[] ser = serpent.serialize(bytes);
		assertEquals("{\n  'data': 'YWJjZGVm',\n  'encoding': 'base64'\n}", S(strip_header(ser)));

		Parser p = new Parser();
		String parsed = p.parse(ser).root.toString();
		assertEquals(39, parsed.length());
	}
	

	@Test
	public void testDateTime()
	{
		Serializer serpent = new Serializer();
		Calendar cal = new GregorianCalendar(2013, 0, 20, 23, 59, 45);
		cal.set(Calendar.MILLISECOND, 999);
		
		byte[] ser = strip_header(serpent.serialize(cal));
		assertEquals("'2013-01-20T23:59:45.999'", S(ser));

		Date date = cal.getTime();
		ser = strip_header(serpent.serialize(date));
		assertEquals("'2013-01-20T23:59:45.999'", S(ser));
		
		cal.set(Calendar.MILLISECOND, 0);
		ser = strip_header(serpent.serialize(cal));
		assertEquals("'2013-01-20T23:59:45'", S(ser));
	}	


	@Test
	public void testNumbers()
	{
		Serializer serpent = new Serializer();
		byte[] ser = serpent.serialize((int)12345);
		byte[] data = strip_header(ser);
		assertEquals("12345", S(data));
		ser = serpent.serialize((long)1234567891234567891L);
        data = strip_header(ser);
        assertEquals("1234567891234567891", S(data));
        ser = serpent.serialize(99.1234);
        data = strip_header(ser);
        assertEquals("99.1234", S(data));
        ser = serpent.serialize(new BigInteger("1234999999999912345678901234567890"));
        data = strip_header(ser);
        assertEquals("1234999999999912345678901234567890", S(data));
		ser = serpent.serialize(new BigDecimal("123456789.987654321987654321987654321987654321"));
		data=strip_header(ser);
		assertEquals("'123456789.987654321987654321987654321987654321'", S(data));
        ComplexNumber cplx = new ComplexNumber(2.2, 3.3);
        ser = serpent.serialize(cplx);
        data = strip_header(ser);
        assertEquals("(2.2+3.3j)", S(data));
        cplx = new ComplexNumber(0, 3);
        ser = serpent.serialize(cplx);
        data = strip_header(ser);
        assertEquals("(0.0+3.0j)", S(data));
        cplx = new ComplexNumber(-2, -3);
        ser = serpent.serialize(cplx);
        data = strip_header(ser);
        assertEquals("(-2.0-3.0j)", S(data));
        cplx = new ComplexNumber(-2.5, -3.9);
        ser = serpent.serialize(cplx);
        data = strip_header(ser);
        assertEquals("(-2.5-3.9j)", S(data));
	}
	
	@Test
	public void testList()
	{
		Serializer serpent = new Serializer();
		List<Object> list = new LinkedList<Object>();
		
		// test empty list
		byte[] ser = strip_header(serpent.serialize(list));
		assertEquals("[]", S(ser));
		serpent.indent=true;
		ser = strip_header(serpent.serialize(list));
		assertEquals("[]", S(ser));
		serpent.indent=false;
		
		// test nonempty list
		list.add(42);
		list.add("Sally");
		list.add(16.5);
		ser = strip_header(serpent.serialize(list));
		assertEquals("[42,'Sally',16.5]", S(ser));
		serpent.indent=true;
		ser = strip_header(serpent.serialize(list));
		assertEquals("[\n  42,\n  'Sally',\n  16.5\n]", S(ser));
	}

	public class UnserializableClass
	{
	}
	
	@Test(expected = IllegalArgumentException.class)
	public void testClassFail()
	{
		Serializer serpent = new Serializer(true, true, false);
		Object obj = new UnserializableClass();
		serpent.serialize(obj);
	}
	
	@Test
	public void testClassOk()
	{
		Serializer.registerClass(SerializationHelperClass.class, null);
		Serializer serpent = new Serializer(true, true, false);
		SerializationHelperClass obj = new SerializationHelperClass();
		obj.i=99;
		obj.s="hi";
		obj.x=42;
		byte[] ser = strip_header(serpent.serialize(obj));
		assertEquals("{\n  'NUMBER': 42,\n  '__class__': 'SerializationHelperClass',\n  'object': None,\n  'theInteger': 99,\n  'theString': 'hi',\n  'thingy': True,\n  'x': 'X'\n}", S(ser));
	}
	
	@Test
	public void testClassPackageOk()
	{
		Serializer.registerClass(SerializationHelperClass.class, null);
		Serializer serpent = new Serializer(true, true, true);
		SerializationHelperClass obj = new SerializationHelperClass();
		obj.i=99;
		obj.s="hi";
		obj.x=42;
		byte[] ser = strip_header(serpent.serialize(obj));
		assertEquals("{\n  'NUMBER': 42,\n  '__class__': 'net.razorvine.serpent.test.SerializationHelperClass',\n  'object': None,\n  'theInteger': 99,\n  'theString': 'hi',\n  'thingy': True,\n  'x': 'X'\n}", S(ser));
	}

	class TestclassConverter implements IClassSerializer
	{
		@Override
		public Map<String, Object> convert(Object obj) {
		    SerializationHelperClass o = (SerializationHelperClass) obj;
		    Map<String, Object> result = new HashMap<String, Object>();
		    result.put("__class@__", o.getClass().getSimpleName()+"@");
		    result.put("i@", o.i);
		    result.put("s@", o.s);
		    result.put("x@", o.x);
		    return result; 
		}
	}

	class ExceptionConverter implements IClassSerializer
	{
		@Override
		public Map<String, Object> convert(Object obj) {
			IllegalArgumentException e = (IllegalArgumentException) obj;
		    Map<String, Object> result = new HashMap<String, Object>();
		    result.put("__class@__", e.getClass().getSimpleName());
		    result.put("msg@", e.getMessage());
		    return result; 
		}
	}
	
	@Test
	public void testCustomClassDict()
	{
		Serializer.registerClass(SerializationHelperClass.class, new TestclassConverter());
	    Serializer serpent = new Serializer(true, true, false);
	      
		SerializationHelperClass obj = new SerializationHelperClass();
		obj.i=99;
		obj.s="hi";
		obj.x=42;

		byte[] ser = strip_header(serpent.serialize(obj));
	    assertEquals("{\n  '__class@__': 'SerializationHelperClass@',\n  'i@': 99,\n  's@': 'hi',\n  'x@': 42\n}", S(ser));
	} 
	
	@Test
	public void testCustomExceptionDict()
	{
		Serializer.registerClass(IllegalArgumentException.class, new ExceptionConverter());
	    Serializer serpent = new Serializer(true, true, false);
	      
		Exception x = new IllegalArgumentException("errormessage");
		byte[] ser = strip_header(serpent.serialize(x));
		assertEquals("{\n  '__class@__': 'IllegalArgumentException',\n  'msg@': 'errormessage'\n}", S(ser));
	} 
	
	@Test
	public void testSet()
	{
		Serializer serpent = new Serializer();
		Set<Object> set = new HashSet<Object>();
		
		// test empty set
		byte[] ser = strip_header(serpent.serialize(set));
		assertEquals("()", S(ser));  // empty set is serialized as a tuple.
		serpent.indent=true;
		ser = strip_header(serpent.serialize(set));
		assertEquals("()", S(ser));  // empty set is serialized as a tuple.
		serpent.indent=false;
		
		// test nonempty set
		set.add("X");
		set.add("Sally");
		set.add("Y");
		ser = strip_header(serpent.serialize(set));
		assertEquals(17, ser.length);
		assertTrue(S(ser).contains("'Sally'"));
		assertTrue(S(ser).contains("'X'"));
		assertTrue(S(ser).contains("'Y'"));
		serpent.indent=true;
		ser = strip_header(serpent.serialize(set));
		assertEquals("{\n  'Sally',\n  'X',\n  'Y'\n}", S(ser));
		
		// test no set literals
		serpent.indent=false;
		serpent.setliterals=false;
		ser = strip_header(serpent.serialize(set));
		assertEquals(17, ser.length);
		assertTrue(S(ser).contains("'Sally'"));
		assertTrue(S(ser).contains("'X'"));
		assertTrue(S(ser).contains("'Y'"));
		assertTrue(ser[0]=='(');
		assertTrue(ser[ser.length-1]==')');
	}
	
	@Test
	public void testCollection()
	{
		Collection<Integer> intlist = new LinkedList<Integer>();
		intlist.add(42);
		intlist.add(43);
		Serializer serpent = new Serializer();
		byte[] ser = serpent.serialize(intlist);
		ser = strip_header(ser);
		assertEquals("[42,43]", S(ser));
		
		ser=strip_header(serpent.serialize(new int[] {42}));
		assertEquals("(42,)", S(ser));
		ser=strip_header(serpent.serialize(new int[] {42, 43}));
		assertEquals("(42,43)", S(ser));
		
		serpent.indent=true;
		ser = strip_header(serpent.serialize(intlist));
		assertEquals("[\n  42,\n  43\n]", S(ser));
		ser=strip_header(serpent.serialize(new int[] {42}));
		assertEquals("(\n  42,\n)", S(ser));
		ser=strip_header(serpent.serialize(new int[] {42, 43}));
		assertEquals("(\n  42,\n  43\n)", S(ser));
	}	


	@Test
	public void testDictionary()
	{
		Serializer serpent = new Serializer();
		Parser p = new Parser();
		
		// test empty dict
		Hashtable<Object, Object> ht = new Hashtable<Object, Object>();
		byte[] ser = serpent.serialize(ht);
		assertEquals("{}", S(strip_header(ser)));
		
		String parsed = p.parse(ser).root.toString();
		assertEquals("{}", parsed);
		
		//empty dict with indentation
	    serpent.indent=true;
		ser = serpent.serialize(ht);
		assertEquals("{}", S(strip_header(ser)));
		
		parsed = p.parse(ser).root.toString();
        assertEquals("{}", parsed);
		
		// test dict with values
		serpent.indent=false;
		ht = new Hashtable<Object, Object>();
		ht.put(42, "fortytwo");
		ht.put("sixteen-and-half", 16.5);
		ht.put("name", "Sally");
		ht.put("status", false);
		
		ser = serpent.serialize(ht);
		assertEquals('}', ser[ser.length-1]);
		assertTrue(ser[ser.length-2]!=',');
		
		parsed = p.parse(ser).root.toString();
		assertEquals(69, parsed.length());
      
        // test indentation
        serpent.indent=true;
        ser = serpent.serialize(ht);
		assertEquals('}', ser[ser.length-1]);
		assertEquals('\n', ser[ser.length-2]);
		assertTrue(ser[ser.length-3]!=',');
		String ser_str = S(strip_header(ser));
		assertTrue(ser_str.contains("'name': 'Sally'"));
		assertTrue(ser_str.contains("'status': False"));
		assertTrue(ser_str.contains("42: 'fortytwo'"));
		assertTrue(ser_str.contains("'sixteen-and-half': 16.5"));
		
		parsed = p.parse(ser).root.toString();
        assertEquals(69, parsed.length());
		
        serpent.indent=false;
      
        // generic Dictionary test
        Map<Integer, String> mydict = new HashMap<Integer, String>();
        mydict.put(1, "one");
        mydict.put(2, "two");
        ser = serpent.serialize(mydict);
        ser_str = S(strip_header(ser));
        assertTrue(ser_str.equals("{2:'two',1:'one'}") || ser_str.equals("{1:'one',2:'two'}"));
	}

	
	@Test
	public void testIndentation()
	{
		Map<String, Object> dict = new HashMap<String, Object>();
		List<Object> list = new LinkedList<Object>();
		list.add(1);
		list.add(2);
		list.add(new String[] {"a", "b"});
		dict.put("first", list);
		
		Map<Integer, Boolean> subdict = new HashMap<Integer, Boolean>();
		subdict.put(1, false);
		dict.put("second", subdict);
		
		Set<Integer> subset = new HashSet<Integer>();
		subset.add(3);
		subset.add(4);
		dict.put("third", subset);
		
		Serializer serpent = new Serializer();
		serpent.indent=true;
		byte[] ser = strip_header(serpent.serialize(dict));
		assertEquals("{\n"+
"  'first': [\n"+
"    1,\n"+				
"    2,\n"+				
"    (\n"+				
"      'a',\n"+				
"      'b'\n"+				
"    )\n"+				
"  ],\n"+				
"  'second': {\n"+				
"    1: False\n"+				
"  },\n"+				
"  'third': {\n"+				
"    3,\n"+				
"    4\n"+				
"  }\n"+				
"}", S(ser));				
	}

	@Test
	public void testSorting()
	{
		Serializer serpent=new Serializer();
		ArrayList<Integer> data1 = new ArrayList<Integer>();
		data1.add(3);
		data1.add(2);
		data1.add(1);
		byte[] ser = strip_header(serpent.serialize(data1));
		assertEquals("[3,2,1]", S(ser));
		int[] data2 = new int[] { 3,2,1 };
		ser = strip_header(serpent.serialize(data2));
		assertEquals("(3,2,1)", S(ser));
		
		Set<Object> data3 = new HashSet<Object>();
		data3.add(42);
		data3.add("hi");
		serpent.indent=true;
		ser = strip_header(serpent.serialize(data3));
		assertTrue(S(ser).equals("{\n  42,\n  'hi'\n}") || S(ser).equals("{\n  'hi',\n  42\n}"));

		Map<Integer, String> data4 = new HashMap<Integer, String>();
		data4.put(5, "five");
		data4.put(3, "three");
		data4.put(1, "one");
		data4.put(4, "four");
		data4.put(2, "two");
		serpent.indent=true;
		ser = strip_header(serpent.serialize(data4));
		assertEquals("{\n  1: 'one',\n  2: 'two',\n  3: 'three',\n  4: 'four',\n  5: 'five'\n}", S(ser));
		
		Set<String> data5 = new HashSet<String>();
		data5.add("x");
		data5.add("y");
		data5.add("z");
		data5.add("c");
		data5.add("b");
		data5.add("a");
		serpent.indent=true;
		ser = strip_header(serpent.serialize(data5));
		assertEquals("{\n  'a',\n  'b',\n  'c',\n  'x',\n  'y',\n  'z'\n}", S(ser));
	}
}
