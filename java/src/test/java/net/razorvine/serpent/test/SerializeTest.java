/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent.test;

import static org.junit.Assert.*;

import java.io.Serializable;
import java.io.UnsupportedEncodingException;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.*;

import net.razorvine.serpent.ComplexNumber;
import net.razorvine.serpent.IClassSerializer;
import net.razorvine.serpent.Parser;
import net.razorvine.serpent.Serializer;

import net.razorvine.serpent.ast.BytesNode;
import org.junit.Test;

public class SerializeTest {

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

		data = B("# header\nfirst-line");
		data = strip_header(data);
		assertEquals("first-line", S(data));
	}


	@Test
	public void testException()
	{
		Serializer.registerClass(IllegalArgumentException.class, null);
		Exception x = new IllegalArgumentException("errormessage");
		Serializer serpent = new Serializer(true, false);
		byte[] ser = strip_header(serpent.serialize(x));
		assertEquals("{\n  '__class__': 'IllegalArgumentException',\n  '__exception__': True,\n  'args': (\n    'errormessage',\n  ),\n  'attributes': {}\n}", S(ser));
	}

	@Test
	public void testExceptionPackage()
	{
		Serializer.registerClass(IllegalArgumentException.class, null);
		Exception x = new IllegalArgumentException("errormessage");
		Serializer serpent = new Serializer(true, true);
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
		result = ser.serialize("\\");
		result=strip_header(result);
		assertEquals("'\\\\'", S(result));
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
	public void testUnicodeEscapes()
	{
		Serializer serpent=new Serializer();

		// regular escaped chars first
	  	byte[] ser = serpent.serialize("\b\r\n\f\t \\");
	  	byte[] data = strip_header(ser);
	  	// '\\x08\\r\\n\\x0c\\t \\\\'
	  	assertArrayEquals(new byte[] {39,
	  			92, 120, 48, 56,
	  			92, 114,
	  			92, 110,
	  			92, 120, 48, 99,
	  			92, 116,
	  			32,
	  			92, 92,
	  			39}, data);

		// simple cases  (chars < 0x80)
	  	ser = serpent.serialize("\u0000\u0001\u001f\u007f");
	    data = strip_header(ser);
	  	// '\\x00\\x01\\x1f\\x7f'
	  	assertArrayEquals(new byte[] {39,
	  			92, 120, 48, 48,
	  			92, 120, 48, 49,
	  			92, 120, 49, 102,
	  			92, 120, 55, 102,
	  			39 }, data);

	  	// chars 0x80 .. 0xff
	  	ser = serpent.serialize("\u0080\u0081\u00ff");
	  	data = strip_header(ser);
	  	// '\\x80\\x81\xc3\xbf'  (has some utf-8 encoded chars in it)
	  	assertArrayEquals(new byte[] {39,
	  			92, 120, 56, 48,
	  			92, 120, 56, 49,
	  			-61, -65,
	  			39}, data);

	  	// chars above 0xff
	  	ser = serpent.serialize("\u0100\u20ac\u8899");
	  	data = strip_header(ser);
	  	// '\xc4\x80\xe2\x82\xac\xe8\xa2\x99'   (has some utf-8 encoded chars in it)
	  	assertArrayEquals(new byte[] {39, -60, -128, -30, -126, -84, -24, -94, -103, 39}, data);

	  	// some random high chars that are all printable in python and not escaped
	  	ser = serpent.serialize("\u0377\u082d\u10c5\u135d\uac00");
	  	data = strip_header(ser);
	  	// '\xcd\xb7\xe0\xa0\xad\xe1\x83\x85\xe1\x8d\x9d\xea\xb0\x80'   (only a bunch of utf-8 encoded chars)
	  	assertArrayEquals(new byte[] {39, -51, -73, -32, -96, -83, -31, -125, -123, -31, -115, -99, -22, -80, -128, 39}, data);

	  	// some random high chars that are all non-printable in python and that are escaped
	  	ser = serpent.serialize("\u0378\u082e\u10c6\u135c\uabff");
	  	data = strip_header(ser);
	  	// '\\u0378\\u082e\\u10c6\\u135c\\uabff'
	  	assertArrayEquals(new byte[] {39,
	  			92, 117, 48, 51, 55, 56,
	  			92, 117, 48, 56, 50, 101,
	  			92, 117, 49, 48, 99, 54,
	  			92, 117, 49, 51, 53, 99,
	  			92, 117, 97, 98, 102, 102,
	  			39}, data);
	}

	@Test
	public void testNullByte()
	{
		Serializer serpent = new Serializer();
		byte[] ser = serpent.serialize("null\u0000byte");
		byte[] data = strip_header(ser);
		assertEquals("'null\\x00byte'", new String(data));
		for(byte b: ser) {
			if(b==0)
				fail("serialized data may not contain 0-bytes");
		}
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
	public void testBytesDefault()
	{
		Serializer serpent = new Serializer(true, false);
		byte[] bytes = new byte[] { 97, 98, 99, 100, 101, 102 };	// abcdef
		byte[] ser = serpent.serialize(bytes);
		assertEquals("{\n  'data': 'YWJjZGVm',\n  'encoding': 'base64'\n}", S(strip_header(ser)));

		Parser p = new Parser();
		String parsed = p.parse(ser).root.toString();
		assertEquals(39, parsed.length());

		Map<String,String> dict = new HashMap<String, String>();
		dict.put("data", "YWJjZGVm");
		dict.put("encoding", "base64");

        byte[] bytes2 = Parser.toBytes(dict);
        assertArrayEquals(bytes, bytes2);

        dict.put("encoding", "base99");
        try {
        	Parser.toBytes(dict);
        	fail("error expected");
        } catch (IllegalArgumentException x) {
        	//
        }

        dict.clear();
        try {
        	Parser.toBytes(dict);
        	fail("error expected");
        } catch (IllegalArgumentException x) {
        	//
        }
        dict.clear();
        dict.put("data", "YWJjZGVm");
        try {
        	Parser.toBytes(dict);
        	fail("error expected");
        } catch (IllegalArgumentException x) {
        	//
        }
        dict.clear();
        dict.put("encoding", "base64");
        try {
        	Parser.toBytes(dict);
        	fail("error expected");
        } catch (IllegalArgumentException x) {
        	//
        }
        try {
        	Parser.toBytes(12345);
        	fail("error expected");
        } catch (IllegalArgumentException x) {
        	//
        }
        try {
        	Parser.toBytes(null);
        	fail("error expected");
        } catch (IllegalArgumentException x) {
        	//
        }
	}


	@Test
	public void testBytesRepr()
	{
		Serializer serpent = new Serializer(true, false, true);
		byte[] bytes = new byte[] { 97, 98, 99, 100, 101, 102, 0, -1, '\'', '\"' };	// abcdef\x00\xff'"
		byte[] ser = serpent.serialize(bytes);
		assertEquals("b'abcdef\\x00\\xff\\'\"'", S(strip_header(ser)));

		Parser p = new Parser();
		BytesNode parsed = (BytesNode) p.parse(ser).root;
		assertArrayEquals(bytes, parsed.toByteArray());
	}


	@Test
	public void testDateTime()
	{
		Serializer serpent = new Serializer();
		Calendar cal = new GregorianCalendar(2013, 0, 20, 23, 59, 45);
		cal.set(Calendar.MILLISECOND, 999);
		cal.setTimeZone(TimeZone.getTimeZone("GMT+0"));

		byte[] ser = strip_header(serpent.serialize(cal));
		assertEquals("'2013-01-20T23:59:45.999Z'", S(ser));

		Date date = cal.getTime();
		ser = strip_header(serpent.serialize(date));
		assertEquals("'2013-01-20T23:59:45.999Z'", S(ser));

		ser = strip_header(serpent.serialize(date));
		assertEquals("'2013-01-20T23:59:45.999Z'", S(ser));

		cal.set(Calendar.MILLISECOND, 0);
		ser = strip_header(serpent.serialize(cal));
		assertEquals("'2013-01-20T23:59:45Z'", S(ser));

		date = cal.getTime();
		ser = strip_header(serpent.serialize(date));
		assertEquals("'2013-01-20T23:59:45Z'", S(ser));
	}

	@Test
	public void testDateTimeWithTimezone()
	{
		Serializer serpent = new Serializer();
		Calendar cal = new GregorianCalendar(2013, 0, 20, 23, 59, 45);
		cal.set(Calendar.MILLISECOND, 999);
		cal.setTimeZone(TimeZone.getTimeZone("Europe/Amsterdam"));

		byte[] ser = strip_header(serpent.serialize(cal));
		assertEquals("'2013-01-20T23:59:45.999+0100'", S(ser));		// normal time

		cal = new GregorianCalendar(2013, 4, 10, 13, 59, 45);
		cal.set(Calendar.MILLISECOND, 999);
		cal.setTimeZone(TimeZone.getTimeZone("Europe/Amsterdam"));

		ser = strip_header(serpent.serialize(cal));
		assertEquals("'2013-05-10T13:59:45.999+0200'", S(ser));		// daylight saving time

		Date date=cal.getTime();
		ser = strip_header(serpent.serialize(date));
		assertEquals("'2013-05-10T11:59:45.999Z'", S(ser));		  // the date and time in UTC

		cal.set(Calendar.MILLISECOND, 0);
		date=cal.getTime();
		ser = strip_header(serpent.serialize(date));
		assertEquals("'2013-05-10T11:59:45Z'", S(ser));		  // the date and time in UTC
	}

	@Test
	public void testNumbers()
	{
		Serializer serpent = new Serializer();
		byte[] ser = serpent.serialize(12345);
		byte[] data = strip_header(ser);
		assertEquals("12345", S(data));
		ser = serpent.serialize(1234567891234567891L);
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
	public void testDoubleNanInf()
	{
		Serializer serpent = new Serializer();
		Object[] doubles = new Object[] {Double.POSITIVE_INFINITY, Double.NEGATIVE_INFINITY, Double.NaN,
				Float.POSITIVE_INFINITY, Float.NEGATIVE_INFINITY, Float.NaN,
		        new ComplexNumber(Double.POSITIVE_INFINITY, 3.3)};
		byte[] ser = serpent.serialize(doubles);
		byte[] data = strip_header(ser);
		assertEquals("(1e30000,-1e30000,{'__class__':'float','value':'nan'},1e30000,-1e30000,{'__class__':'float','value':'nan'},(1e30000+3.3j))", S(data));
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
		Serializer serpent = new Serializer(true, false);
		Object obj = new UnserializableClass();
		serpent.serialize(obj);
	}

	@Test
	public void testClassOk()
	{
		Serializer.registerClass(SerializationHelperClass.class, null);
		Serializer serpent = new Serializer(true, false);
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
		Serializer serpent = new Serializer(true, true);
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
	    Serializer serpent = new Serializer(true, false);

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
	    Serializer serpent = new Serializer(true, false);

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

	interface IBaseInterface {}
	interface ISubInterface extends IBaseInterface {}
	class BaseClassWithInterface implements IBaseInterface, Serializable {}
	class SubClassWithInterface extends BaseClassWithInterface implements ISubInterface, Serializable {}
	class BaseClass implements Serializable {}
	class SubClass extends BaseClass implements Serializable {}
	abstract class AbstractBaseClass {}
	class ConcreteSubClass extends AbstractBaseClass implements Serializable {}

	class AnyClassConverter implements IClassSerializer
	{
		@Override
		public Map<String, Object> convert(Object obj) {
		    Map<String, Object> result = new HashMap<String, Object>();
		    result.put("(SUB)CLASS", obj.getClass().getSimpleName());
		    return result;
		}
	}

	@Test
	public void testAbstractBaseClassHierarchyPickler()
	{
		ConcreteSubClass c = new ConcreteSubClass();
		Serializer serpent=new Serializer();
		byte[] data = serpent.serialize(c);
		assertEquals("{'__class__':'ConcreteSubClass'}", S(strip_header(data)));  // the default serializer

		Serializer.registerClass(AbstractBaseClass.class, new AnyClassConverter());
		data = serpent.serialize(c);
		assertEquals("{'(SUB)CLASS':'ConcreteSubClass'}", S(strip_header(data)));  // custom serializer
	}

	@Test
	public void testInterfaceHierarchyPickler()
	{
		BaseClassWithInterface b = new BaseClassWithInterface();
		SubClassWithInterface sub = new SubClassWithInterface();
		Serializer serpent=new Serializer();
		byte[] data = serpent.serialize(b);
		assertEquals("{'__class__':'BaseClassWithInterface'}", S(strip_header(data)));  // the default serializer
		data = serpent.serialize(sub);
		assertEquals("{'__class__':'SubClassWithInterface'}", S(strip_header(data)));  // the default serializer

		Serializer.registerClass(IBaseInterface.class, new AnyClassConverter());
		data = serpent.serialize(b);
		assertEquals("{'(SUB)CLASS':'BaseClassWithInterface'}", S(strip_header(data)));  // custom serializer
		data = serpent.serialize(sub);
		assertEquals("{'(SUB)CLASS':'SubClassWithInterface'}", S(strip_header(data)));  // custom serializer
	}
}
