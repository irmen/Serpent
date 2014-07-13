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
using System.Runtime.Serialization;
using System.Text;

using NUnit.Framework;
using Hashtable = System.Collections.Hashtable;
using IDictionary = System.Collections.IDictionary;

namespace Razorvine.Serpent.Test
{
	[TestFixture]
	public class SerializeTest
	{
		public byte[] strip_header(byte[] data)
		{
			int start=Array.IndexOf(data, (byte)10); // the newline after the header
			if(start<0)
				throw new ArgumentException("need header in string");
			start++;
			byte[] result = new byte[data.Length-start];
			Array.Copy(data, start, result, 0, data.Length-start);
			return result;
		}
		
		public byte[] B(string s)
		{
			return Encoding.UTF8.GetBytes(s);
		}
		
		public string S(byte[] b)
		{
			return Encoding.UTF8.GetString(b);
		}


		[Test]
		public void TestHeader()
		{
			Serializer ser = new Serializer();
			byte[] data = ser.Serialize(null);
			Assert.AreEqual(35, data[0]);
			string strdata = S(data);
			Assert.AreEqual("# serpent utf-8 python3.2", strdata.Split('\n')[0]);
			
			ser.SetLiterals=false;
			data = ser.Serialize(null);
			strdata = S(data);
			Assert.AreEqual("# serpent utf-8 python2.6", strdata.Split('\n')[0]);

			data = B("# header\nfirst-line");
			data = strip_header(data);
			Assert.AreEqual(B("first-line"), data);
		}
		
		
		[Test]
		public void TestStuff()
		{
			Serializer ser=new Serializer();
			byte[] result = ser.Serialize("blerp");
			result=strip_header(result);
			Assert.AreEqual(B("'blerp'"), result);
			result = ser.Serialize(new Guid("f1f8d00e-49a5-4662-ac1d-d5f0426ed293"));
			result=strip_header(result);
			Assert.AreEqual(B("'f1f8d00e-49a5-4662-ac1d-d5f0426ed293'"), result);
			result = ser.Serialize(123456789.987654321987654321987654321987654321m);
			result=strip_header(result);
			Assert.AreEqual(B("'123456789.98765432198765432199'"), result);
		}

		[Test]
		public void TestNull()
		{
			Serializer ser = new Serializer();
			byte[] data = ser.Serialize(null);
			data=strip_header(data);
			Assert.AreEqual(B("None"),data);
		}
		
		[Test]
		public void TestStrings()
		{
			Serializer serpent = new Serializer();
			byte[] ser = serpent.Serialize("hello");
			byte[] data = strip_header(ser);
			Assert.AreEqual(B("'hello'"), data);
        	ser = serpent.Serialize("quotes'\"");
        	data = strip_header(ser);
        	Assert.AreEqual(B("'quotes\\'\"'"), data);
        	ser = serpent.Serialize("quotes2'");
        	data = strip_header(ser);
        	Assert.AreEqual(B("\"quotes2'\""), data);
		}
		
		[Test]
		public void TestUnicode()
		{
			Serializer serpent = new Serializer();
			byte[] ser = serpent.Serialize("euro\u20ac");
			byte[] data = strip_header(ser);
			Assert.AreEqual(new byte[] {39, 101, 117, 114, 111, 0xe2, 0x82, 0xac, 39}, data);

			ser = serpent.Serialize("A\n\t\\Z");
			// 'A\\n\\t\\\\Z'  (10 bytes)
			data = strip_header(ser);
			Assert.AreEqual(new byte[] {39, 65, 92, 110, 92, 116, 92, 92, 90, 39}, data);
			
			ser = serpent.Serialize("euro\u20ac\nlastline\ttab\\@slash");
			// 'euro\xe2\x82\xac\\nlastline\\ttab\\\\@slash'   (32 bytes)
			data = strip_header(ser);
			Assert.AreEqual(new byte[] {
								39, 101, 117, 114, 111, 226, 130, 172,
								92, 110, 108, 97, 115, 116, 108, 105,
								110, 101, 92, 116, 116, 97, 98, 92,
								92, 64, 115, 108, 97, 115, 104, 39}
				                , data);
		}

		[Test]
		public void TestNumbers()
		{
			Serializer serpent = new Serializer();
			byte[] ser = serpent.Serialize((int)12345);
			byte[] data = strip_header(ser);
			Assert.AreEqual(B("12345"), data);
			ser = serpent.Serialize((uint)12345);
			data = strip_header(ser);
			Assert.AreEqual(B("12345"), data);
			ser = serpent.Serialize((long)1234567891234567891L);
	        data = strip_header(ser);
	        Assert.AreEqual(B("1234567891234567891"), data);
			ser = serpent.Serialize((ulong)12345678912345678912L);
	        data = strip_header(ser);
	        Assert.AreEqual(B("12345678912345678912"), data);
	        ser = serpent.Serialize(99.1234);
	        data = strip_header(ser);
	        Assert.AreEqual(B("99.1234"), data);
	        ser = serpent.Serialize(1234.9999999999m);
	        data = strip_header(ser);
	        Assert.AreEqual(B("'1234.9999999999'"), data);
			ser = serpent.Serialize(123456789.987654321987654321987654321987654321m);
			data=strip_header(ser);
			Assert.AreEqual(B("'123456789.98765432198765432199'"), data);
	        ComplexNumber cplx = new ComplexNumber(2.2, 3.3);
	        ser = serpent.Serialize(cplx);
	        data = strip_header(ser);
	        Assert.AreEqual(B("(2.2+3.3j)"), data);
	        cplx = new ComplexNumber(0, 3);
	        ser = serpent.Serialize(cplx);
	        data = strip_header(ser);
	        Assert.AreEqual(B("(0+3j)"), data);
	        cplx = new ComplexNumber(-2, -3);
	        ser = serpent.Serialize(cplx);
	        data = strip_header(ser);
	        Assert.AreEqual(B("(-2-3j)"), data);
		}
		
		[Test]
		public void TestBool()
		{
			Serializer serpent = new Serializer();
			byte[] ser = serpent.Serialize(true);
			byte[] data = strip_header(ser);
			Assert.AreEqual(B("True"),data);
			ser = serpent.Serialize(false);
			data = strip_header(ser);
			Assert.AreEqual(B("False"),data);
		}
		
		[Test]
		public void TestList()
		{
			Serializer serpent = new Serializer();
			IList<object> list = new List<object>();
			
			// test empty list
			byte[] ser = strip_header(serpent.Serialize(list));
			Assert.AreEqual("[]", S(ser));
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(list));
			Assert.AreEqual("[]", S(ser));
			serpent.Indent=false;
			
			// test nonempty list
			list.Add(42);
			list.Add("Sally");
			list.Add(16.5);
			ser = strip_header(serpent.Serialize(list));
			Assert.AreEqual("[42,'Sally',16.5]", S(ser));
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(list));
			Assert.AreEqual("[\n  42,\n  'Sally',\n  16.5\n]", S(ser));
		}

		[Test]
		public void TestSet()
		{
			// test with set literals
			Serializer serpent = new Serializer();
			serpent.SetLiterals = true;
			HashSet<object> set = new HashSet<object>();
			
			// test empty set
			byte[] ser = strip_header(serpent.Serialize(set));
			Assert.AreEqual("()", S(ser));  // empty set is serialized as a tuple.
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(set));
			Assert.AreEqual("()", S(ser));  // empty set is serialized as a tuple.
			serpent.Indent=false;
			
			// test nonempty set
			set.Add(42);
			set.Add("Sally");
			set.Add(16.5);
			ser = strip_header(serpent.Serialize(set));
			Assert.AreEqual("{42,'Sally',16.5}", S(ser));
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(set));
			Assert.AreEqual("{\n  42,\n  'Sally',\n  16.5\n}", S(ser));
			
			// test no set literals
			serpent.Indent=false;
			serpent.SetLiterals=false;
			ser = strip_header(serpent.Serialize(set));
			Assert.AreEqual("(42,'Sally',16.5)", S(ser));	// needs to be tuple now
		}

		[Test]
		public void TestDictionary()
		{
			Serializer serpent = new Serializer();
			Parser p = new Parser();
			
			// test empty dict
			IDictionary ht = new Hashtable();
			byte[] ser = serpent.Serialize(ht);
			Assert.AreEqual(B("{}"), strip_header(ser));
			string parsed = p.Parse(ser).Root.ToString();
            Assert.AreEqual("{}", parsed);
			
            // empty dict with indentation
            serpent.Indent=true;
			ser = serpent.Serialize(ht);
			Assert.AreEqual(B("{}"), strip_header(ser));
			parsed = p.Parse(ser).Root.ToString();
            Assert.AreEqual("{}", parsed);
			
			// test dict with values
			serpent.Indent=false;
			ht = new Hashtable() {
				{42, "fortytwo"},
				{"sixteen-and-half", 16.5},
				{"name", "Sally"},
				{"status", false}
			};
			
			ser = serpent.Serialize(ht);
			Assert.AreEqual('}', ser[ser.Length-1]);
			Assert.AreNotEqual(',', ser[ser.Length-2]);
			parsed = p.Parse(ser).Root.ToString();
            Assert.AreEqual(69, parsed.Length);
            
            // test indentation
            serpent.Indent=true;
            ser = serpent.Serialize(ht);
			Assert.AreEqual('}', ser[ser.Length-1]);
			Assert.AreEqual('\n', ser[ser.Length-2]);
			Assert.AreNotEqual(',', ser[ser.Length-3]);
			string ser_str = S(strip_header(ser));
			Assert.IsTrue(ser_str.Contains("'name': 'Sally'"));
			Assert.IsTrue(ser_str.Contains("'status': False"));
			Assert.IsTrue(ser_str.Contains("42: 'fortytwo'"));
			Assert.IsTrue(ser_str.Contains("'sixteen-and-half': 16.5"));
			parsed = p.Parse(ser).Root.ToString();
            Assert.AreEqual(69, parsed.Length);
            serpent.Indent=false;
            
            // generic Dictionary test
            IDictionary<int, string> mydict = new Dictionary<int, string> {
            	{ 1, "one" },
            	{ 2, "two" },
            };
            ser = serpent.Serialize(mydict);
            ser_str = S(strip_header(ser));
            Assert.IsTrue(ser_str=="{2:'two',1:'one'}" || ser_str=="{1:'one',2:'two'}");
		}

		[Test]
		public void TestBytes()
		{
			Serializer serpent = new Serializer(indent: true);
			byte[] bytes = new byte[] { 97, 98, 99, 100, 101, 102 };	// abcdef
			byte[] ser = serpent.Serialize(bytes);
			Assert.AreEqual("{\n  'data': 'YWJjZGVm',\n  'encoding': 'base64'\n}", S(strip_header(ser)));

			Parser p = new Parser();
			string parsed = p.Parse(ser).Root.ToString();
            Assert.AreEqual(39, parsed.Length);
		}
		
		[Test]
		public void TestCollection()
		{
			ICollection<int> intlist = new LinkedList<int>();
			intlist.Add(42);
			intlist.Add(43);
			Serializer serpent = new Serializer();
			byte[] ser = serpent.Serialize(intlist);
			ser = strip_header(ser);
			Assert.AreEqual("[42,43]", S(ser));
			
			ser=strip_header(serpent.Serialize(new int[] {42}));
			Assert.AreEqual("(42,)", S(ser));
			ser=strip_header(serpent.Serialize(new int[] {42, 43}));
			Assert.AreEqual("(42,43)", S(ser));
			
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(intlist));
			Assert.AreEqual("[\n  42,\n  43\n]", S(ser));
			ser=strip_header(serpent.Serialize(new int[] {42}));
			Assert.AreEqual("(\n  42,\n)", S(ser));
			ser=strip_header(serpent.Serialize(new int[] {42, 43}));
			Assert.AreEqual("(\n  42,\n  43\n)", S(ser));
		}
		
		
		[Test]
		public void TestIndentation()
		{
			var dict = new Dictionary<string, object>();
			var list = new List<object>() {
				1,
				2,
				new string[] {"a", "b"}
			};
			dict.Add("first", list);
			dict.Add("second", new Dictionary<int, bool> {
			         	{1, false}
			         });
			dict.Add("third", new HashSet<int> { 3, 4} );
			
			Serializer serpent = new Serializer();
			serpent.Indent=true;
			byte[] ser = strip_header(serpent.Serialize(dict));
			Assert.AreEqual(@"{
  'first': [
    1,
    2,
    (
      'a',
      'b'
    )
  ],
  'second': {
    1: False
  },
  'third': {
    3,
    4
  }
}", S(ser).Replace("\n", "\r\n"));

		}
		
		[Test]
		public void TestSorting()
		{
			Serializer serpent=new Serializer();
			object data = new List<int> { 3, 2, 1};
			byte[] ser = strip_header(serpent.Serialize(data));
			Assert.AreEqual("[3,2,1]", S(ser));
			data = new int[] { 3,2,1 };
			ser = strip_header(serpent.Serialize(data));
			Assert.AreEqual("(3,2,1)", S(ser));
			
			data = new HashSet<object> {
				42,
				"hi"
			};
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(data));
			Assert.IsTrue(S(ser)=="{\n  42,\n  'hi'\n}" || S(ser)=="{\n  'hi',\n  42\n}");

			data = new Dictionary<int, string> {
				{5, "five"},
				{3, "three"},
				{1, "one"},
				{4, "four"},
				{2, "two"}
			};
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(data));
			Assert.AreEqual("{\n  1: 'one',\n  2: 'two',\n  3: 'three',\n  4: 'four',\n  5: 'five'\n}", S(ser));
			
			data = new HashSet<string> {
				"x",
				"y",
				"z",
				"c",
				"b",
				"a"
			};
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(data));
			Assert.AreEqual("{\n  'a',\n  'b',\n  'c',\n  'x',\n  'y',\n  'z'\n}", S(ser));
		}

		[Test]
		public void TestClass()
		{
			Serializer.RegisterClass(typeof(SerializeTestClass), null);
			Serializer serpent = new Serializer(indent: true);
			object obj = new UnserializableClass();
			Assert.Throws<SerializationException>( ()=>serpent.Serialize(obj) );
			
			obj = new SerializeTestClass() {
				i = 99,
				s = "hi",
				x = 42
			};
			byte[] ser = strip_header(serpent.Serialize(obj));
			Assert.AreEqual("{\n  '__class__': 'SerializeTestClass',\n  'i': 99,\n  'obj': None,\n  's': 'hi'\n}", S(ser));
		}

		[Test]
		public void TestClass2()
		{
			Serializer.RegisterClass(typeof(SerializeTestClass), null);
			Serializer serpent = new Serializer(indent: true, namespaceInClassName: true);
			object obj = new SerializeTestClass() {
				i = 99,
				s = "hi",
				x = 42
			};
			byte[] ser = strip_header(serpent.Serialize(obj));
			Assert.AreEqual("{\n  '__class__': 'Razorvine.Serpent.Test.SerializeTestClass',\n  'i': 99,\n  'obj': None,\n  's': 'hi'\n}", S(ser));
		}

		protected IDictionary testclassConverter(object obj)
		{
			SerializeTestClass o = (SerializeTestClass) obj;
			IDictionary result = new Hashtable();
			result["__class@__"] = o.GetType().Name+"@";
			result["i@"] = o.i;
			result["s@"] = o.s;
			result["x@"] = o.x;
			return result;
		}
		
		[Test]
		public void TestCustomClassDict()
		{
			Serializer.RegisterClass(typeof(SerializeTestClass), testclassConverter);
			Serializer serpent = new Serializer(indent: true);
			
			var obj = new SerializeTestClass() {
				i = 99,
				s = "hi",
				x = 42
			};
			byte[] ser = strip_header(serpent.Serialize(obj));
			Assert.AreEqual("{\n  '__class@__': 'SerializeTestClass@',\n  'i@': 99,\n  's@': 'hi',\n  'x@': 42\n}", S(ser));
		}
		
		[Test]
		public void TestStruct()
		{
			Serializer serpent = new Serializer(indent: true);
			UnserializableStruct obj;
			Assert.Throws<SerializationException>( ()=>serpent.Serialize(obj) );
			
			var obj2 = new SerializeTestStruct() {
				i = 99,
				s = "hi",
				x = 42
			};
			byte[] ser = strip_header(serpent.Serialize(obj2));
			Assert.AreEqual("{\n  '__class__': 'SerializeTestStruct',\n  'i': 99,\n  's': 'hi'\n}", S(ser));
		}
		
		[Test]
		public void TestStruct2()
		{
			Serializer serpent = new Serializer(indent: true, namespaceInClassName: true);
			UnserializableStruct obj;
			Assert.Throws<SerializationException>( ()=>serpent.Serialize(obj) );
			
			var obj2 = new SerializeTestStruct() {
				i = 99,
				s = "hi",
				x = 42
			};
			byte[] ser = strip_header(serpent.Serialize(obj2));
			Assert.AreEqual("{\n  '__class__': 'Razorvine.Serpent.Test.SerializeTestStruct',\n  'i': 99,\n  's': 'hi'\n}", S(ser));
		}

		[Test]
		public void TestAnonymousClass()
		{
			Serializer serpent = new Serializer(indent: true);
			Object obj = new {
				Name="Harry",
				Age=33,
				Country="NL"
			};
			
			byte[] ser = strip_header(serpent.Serialize(obj));
			Assert.AreEqual("{\n  'Age': 33,\n  'Country': 'NL',\n  'Name': 'Harry'\n}", S(ser));
		}
		
		[Test]
		public void TestDateTime()
		{
			Serializer serpent = new Serializer();
			
			DateTime date = new DateTime(2013, 1, 20, 23, 59, 45, 999);
			byte[] ser = strip_header(serpent.Serialize(date));
			Assert.AreEqual("'2013-01-20T23:59:45.999000'", S(ser));
			
			date = new DateTime(2013, 1, 20, 23, 59, 45);
			ser = strip_header(serpent.Serialize(date));
			Assert.AreEqual("'2013-01-20T23:59:45'", S(ser));
		
			TimeSpan timespan = new TimeSpan(1, 10, 20, 30, 999);
			ser = strip_header(serpent.Serialize(timespan));
			Assert.AreEqual("123630.999", S(ser));
		}
		
		[Test]
		public void TestException()
		{
			Exception x = new ApplicationException("errormessage");
			Serializer serpent = new Serializer(indent:true);
			byte[] ser = strip_header(serpent.Serialize(x));
			Assert.AreEqual("{\n  '__class__': 'ApplicationException',\n  '__exception__': True,\n  'args': (\n    'errormessage',\n  ),\n  'attributes': {}\n}", S(ser));

			x.Data["custom_attribute"]=999;
			ser = strip_header(serpent.Serialize(x));
			Assert.AreEqual("{\n  '__class__': 'ApplicationException',\n  '__exception__': True,\n  'args': (\n    'errormessage',\n  ),\n  'attributes': {\n    'custom_attribute': 999\n  }\n}", S(ser));
		}
		
		[Test]
		public void TestExceptionWithNamespace()
		{
			Exception x = new ApplicationException("errormessage");
			Serializer serpent = new Serializer(indent:true, namespaceInClassName: true);
			byte[] ser = strip_header(serpent.Serialize(x));
			Assert.AreEqual("{\n  '__class__': 'System.ApplicationException',\n  '__exception__': True,\n  'args': (\n    'errormessage',\n  ),\n  'attributes': {}\n}", S(ser));
		}

		enum FooType {
			Foobar,
			Jarjar
		}

		[Test]
		public void TestEnum()
		{
			FooType e = FooType.Jarjar;
			Serializer serpent = new Serializer();
			byte[] ser = strip_header(serpent.Serialize(e));
			Assert.AreEqual("'Jarjar'", S(ser));
		}
	}

	[Serializable]
	public class SerializeTestClass
	{
		public int x;
		public string s {get; set;}
		public int i {get; set;}
		public object obj {get; set;}
		
	}
	
	[Serializable]
	public struct SerializeTestStruct
	{
		public int x;
		public string s {get; set;}
		public int i {get; set;}
	}

	public class UnserializableClass
	{
	}
	
	public struct UnserializableStruct
	{
	}
}
