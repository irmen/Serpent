using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Xunit;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeMadeStatic.Local

namespace Razorvine.Serpent.Test
{
	public class SerializeTest
	{
		private byte[] strip_header(byte[] data)
		{
			int start=Array.IndexOf(data, (byte)10); // the newline after the header
			if(start<0)
				throw new ArgumentException("need header in string");
			start++;
			var result = new byte[data.Length-start];
			Array.Copy(data, start, result, 0, data.Length-start);
			return result;
		}

		private byte[] B(string s)
		{
			return Encoding.UTF8.GetBytes(s);
		}

		private string S(byte[] b)
		{
			return Encoding.UTF8.GetString(b);
		}


		[Fact]
		public void TestHeader()
		{
			Serializer ser = new Serializer();
			var data = ser.Serialize(null);
			Assert.Equal(35, data[0]);
			string strdata = S(data);
			Assert.Equal("# serpent utf-8 python3.2", strdata.Split('\n')[0]);
			data = B("# header\nfirst-line");
			data = strip_header(data);
			Assert.Equal(B("first-line"), data);
		}
		
		
		[Fact]
		public void TestStuff()
		{
			Serializer ser=new Serializer();
			var result = ser.Serialize("blerp");
			result=strip_header(result);
			Assert.Equal(B("'blerp'"), result);
			result = ser.Serialize(new Guid("f1f8d00e-49a5-4662-ac1d-d5f0426ed293"));
			result=strip_header(result);
			Assert.Equal(B("'f1f8d00e-49a5-4662-ac1d-d5f0426ed293'"), result);
			result = ser.Serialize(123456789.987654321987654321987654321987654321m);
			result=strip_header(result);
			Assert.Equal(B("'123456789.98765432198765432199'"), result);
		}

		[Fact]
		public void TestNull()
		{
			Serializer ser = new Serializer();
			var data = ser.Serialize(null);
			data=strip_header(data);
			Assert.Equal(B("None"),data);
		}
		
		[Fact]
		public void TestStrings()
		{
			Serializer serpent = new Serializer();
			var ser = serpent.Serialize("hello");
			var data = strip_header(ser);
			Assert.Equal(B("'hello'"), data);
        	ser = serpent.Serialize("quotes'\"");
        	data = strip_header(ser);
        	Assert.Equal(B("'quotes\\'\"'"), data);
        	ser = serpent.Serialize("quotes2'");
        	data = strip_header(ser);
        	Assert.Equal(B("\"quotes2'\""), data);
		}
		
		[Fact]
		public void TestUnicodeEscapes()
		{
			Serializer serpent=new Serializer();
			
			// regular escaped chars first
		  	var ser = serpent.Serialize("\b\r\n\f\t \\");
		  	var data = strip_header(ser);
		  	// '\\x08\\r\\n\\x0c\\t \\\\'
		  	Assert.Equal(new byte[] {39,
		  			92, 120, 48, 56,
		  			92, 114,
		  			92, 110,
		  			92, 120, 48, 99,
		  			92, 116,
		  			32,
		  			92, 92,
		  			39}, data);
		  	
			// simple cases  (chars < 0x80)
		  	ser = serpent.Serialize("\u0000\u0001\u001f\u007f");
		    data = strip_header(ser);
		  	// '\\x00\\x01\\x1f\\x7f'
		  	Assert.Equal(new byte[] {39,
		  			92, 120, 48, 48,
		  			92, 120, 48, 49,
		  			92, 120, 49, 102,
		  			92, 120, 55, 102,
		  			39 }, data);
	
		  	// chars 0x80 .. 0xff
		  	ser = serpent.Serialize("\u0080\u0081\u00ff");
		  	data = strip_header(ser);
		  	// '\\x80\\x81\xc3\xbf'  (has some utf-8 encoded chars in it)
		  	Assert.Equal(new byte[] {39, 
		  	        92, 120, 56, 48,
		  	        92, 120, 56, 49,
		  	        195, 191,
		  	        39}, data);
	
		  	// chars above 0xff
		  	ser = serpent.Serialize("\u0100\u20ac\u8899");
		  	data = strip_header(ser);
		  	// '\xc4\x80\xe2\x82\xac\xe8\xa2\x99'   (has some utf-8 encoded chars in it)
		  	Assert.Equal(new byte[] {39, 196, 128, 226, 130, 172, 232, 162, 153, 39}, data);
		  	
//		  	// some random high chars that are all printable in python and not escaped
//		  	ser = serpent.Serialize("\u0377\u082d\u10c5\u135d\uac00");
//		  	data = strip_header(ser);
//		  	Console.WriteLine(S(data)); // XXX
//		  	// '\xcd\xb7\xe0\xa0\xad\xe1\x83\x85\xe1\x8d\x9d\xea\xb0\x80'   (only a bunch of utf-8 encoded chars)
//		  	Assert.Equal(new byte[] {39, 205, 183, 224, 160, 173, 225, 131, 133, 225, 141, 157, 234, 176, 128, 39}, data);
		  	
		  	// some random high chars that are all non-printable in python and that are escaped
		  	ser = serpent.Serialize("\u0378\u082e\u10c6\u135c\uabff");
		  	data = strip_header(ser);
		  	// '\\u0378\\u082e\\u10c6\\u135c\\uabff'
		  	Assert.Equal(new byte[] {39,
		  			92, 117, 48, 51, 55, 56,
		  			92, 117, 48, 56, 50, 101,
		  			92, 117, 49, 48, 99, 54,
		  			92, 117, 49, 51, 53, 99,
		  			92, 117, 97, 98, 102, 102,
		  			39}, data);
		}

		[Fact]
		public void TestNumbers()
		{
			Serializer serpent = new Serializer();
			var ser = serpent.Serialize(12345);
			var data = strip_header(ser);
			Assert.Equal(B("12345"), data);
			ser = serpent.Serialize((uint)12345);
			data = strip_header(ser);
			Assert.Equal(B("12345"), data);
			ser = serpent.Serialize(-1234567891234567891L);
	        data = strip_header(ser);
	        Assert.Equal(B("-1234567891234567891"), data);
			ser = serpent.Serialize(12345678912345678912L);
	        data = strip_header(ser);
	        Assert.Equal(B("12345678912345678912"), data);
	        ser = serpent.Serialize(99.1234);
	        data = strip_header(ser);
	        Assert.Equal(B("99.1234"), data);
	        ser = serpent.Serialize(1234.9999999999m);
	        data = strip_header(ser);
	        Assert.Equal(B("'1234.9999999999'"), data);
			ser = serpent.Serialize(123456789.987654321987654321987654321987654321m);
			data=strip_header(ser);
			Assert.Equal(B("'123456789.98765432198765432199'"), data);
	        ComplexNumber cplx = new ComplexNumber(2.2, 3.3);
	        ser = serpent.Serialize(cplx);
	        data = strip_header(ser);
	        Assert.Equal(B("(2.2+3.3j)"), data);
	        cplx = new ComplexNumber(0, 3);
	        ser = serpent.Serialize(cplx);
	        data = strip_header(ser);
	        Assert.Equal(B("(0+3j)"), data);
	        cplx = new ComplexNumber(-2, -3);
	        ser = serpent.Serialize(cplx);
	        data = strip_header(ser);
	        Assert.Equal(B("(-2-3j)"), data);
		}
				
		[Fact]
		public void TestDoubleNanInf()
		{
			Serializer serpent = new Serializer();
			var doubles = new object[] {double.PositiveInfinity, double.NegativeInfinity, double.NaN,
			        float.PositiveInfinity, float.NegativeInfinity, float.NaN,
			        new ComplexNumber(double.PositiveInfinity, 3.4)};
			var ser = serpent.Serialize(doubles);
			var data = strip_header(ser);
			Assert.Equal("(1e30000,-1e30000,{'__class__':'float','value':'nan'},1e30000,-1e30000,{'__class__':'float','value':'nan'},(1e30000+3.4j))", S(data));
		}

		[Fact]
		public void TestBool()
		{
			Serializer serpent = new Serializer();
			var ser = serpent.Serialize(true);
			var data = strip_header(ser);
			Assert.Equal(B("True"),data);
			ser = serpent.Serialize(false);
			data = strip_header(ser);
			Assert.Equal(B("False"),data);
		}
		
		[Fact]
		public void TestList()
		{
			Serializer serpent = new Serializer();
			IList<object> list = new List<object>();
			
			// test empty list
			var ser = strip_header(serpent.Serialize(list));
			Assert.Equal("[]", S(ser));
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(list));
			Assert.Equal("[]", S(ser));
			serpent.Indent=false;
			
			// test nonempty list
			list.Add(42);
			list.Add("Sally");
			list.Add(16.5);
			ser = strip_header(serpent.Serialize(list));
			Assert.Equal("[42,'Sally',16.5]", S(ser));
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(list));
			Assert.Equal("[\n  42,\n  'Sally',\n  16.5\n]", S(ser));
		}

		[Fact]
		public void TestSet()
		{
			// test with set literals
			Serializer serpent = new Serializer();
			var set = new HashSet<object>();
			
			// test empty set
			var ser = strip_header(serpent.Serialize(set));
			Assert.Equal("()", S(ser));  // empty set is serialized as a tuple.
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(set));
			Assert.Equal("()", S(ser));  // empty set is serialized as a tuple.
			serpent.Indent=false;
			
			// test nonempty set
			set.Add(42);
			set.Add("Sally");
			set.Add(16.5);
			ser = strip_header(serpent.Serialize(set));
			Assert.Equal("{42,'Sally',16.5}", S(ser));
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(set));
			Assert.Equal("{\n  42,\n  'Sally',\n  16.5\n}", S(ser));
		}

		[Fact]
		public void TestDictionary()
		{
			Serializer serpent = new Serializer();
			Parser p = new Parser();
			
			// test empty dict
			IDictionary ht = new Hashtable();
			var ser = serpent.Serialize(ht);
			Assert.Equal(B("{}"), strip_header(ser));
			string parsed = p.Parse(ser).Root.ToString();
            Assert.Equal("{}", parsed);
			
            // empty dict with indentation
            serpent.Indent=true;
			ser = serpent.Serialize(ht);
			Assert.Equal(B("{}"), strip_header(ser));
			parsed = p.Parse(ser).Root.ToString();
            Assert.Equal("{}", parsed);
			
			// test dict with values
			serpent.Indent=false;
			ht = new Hashtable
			{
				{42, "fortytwo"},
				{"sixteen-and-half", 16.5},
				{"name", "Sally"},
				{"status", false}
			};
			
			ser = serpent.Serialize(ht);
			Assert.Equal((int)'}', ser[ser.Length-1]);
			Assert.NotEqual((int)',', ser[ser.Length-2]);
			parsed = p.Parse(ser).Root.ToString();
            Assert.Equal(69, parsed.Length);
            
            // test indentation
            serpent.Indent=true;
            ser = serpent.Serialize(ht);
			Assert.Equal((int)'}', ser[ser.Length-1]);
			Assert.Equal((int)'\n', ser[ser.Length-2]);
			Assert.NotEqual((int)',', ser[ser.Length-3]);
			string ser_str = S(strip_header(ser));
			Assert.Contains("'name': 'Sally'", ser_str);
			Assert.Contains("'status': False", ser_str);
			Assert.Contains("42: 'fortytwo'", ser_str);
			Assert.Contains("'sixteen-and-half': 16.5", ser_str);
			parsed = p.Parse(ser).Root.ToString();
            Assert.Equal(69, parsed.Length);
            serpent.Indent=false;
            
            // generic Dictionary test
            IDictionary<int, string> mydict = new Dictionary<int, string> {
            	{ 1, "one" },
            	{ 2, "two" }
            };
            ser = serpent.Serialize(mydict);
            ser_str = S(strip_header(ser));
            Assert.True(ser_str=="{2:'two',1:'one'}" || ser_str=="{1:'one',2:'two'}");
		}

		[Fact]
		public void TestBytesDefault()
		{
			Serializer serpent = new Serializer(true);
			byte[] bytes = { 97, 98, 99, 100, 101, 102 };	// abcdef
			var ser = serpent.Serialize(bytes);
			Assert.Equal("{\n  'data': 'YWJjZGVm',\n  'encoding': 'base64'\n}", S(strip_header(ser)));

			Parser p = new Parser();
			string parsed = p.Parse(ser).Root.ToString();
            Assert.Equal(39, parsed.Length);

            var hashtable = new Hashtable {
            	{"data", "YWJjZGVm"},
            	{"encoding", "base64"}
            };
            var bytes2 = Parser.ToBytes(hashtable);
            Assert.Equal(bytes, bytes2);

            var dict = new Dictionary<string, string> {
            	{"data", "YWJjZGVm"},
            	{"encoding", "base64"}
            };
            bytes2 = Parser.ToBytes(dict);
            Assert.Equal(bytes, bytes2);
            
            var dict2 = new Dictionary<object, object> {
            	{"data", "YWJjZGVm"},
            	{"encoding", "base64"}
            };
            bytes2 = Parser.ToBytes(dict2);
            Assert.Equal(bytes, bytes2);

            dict["encoding"] = "base99";
            Assert.Throws<ArgumentException>(()=>Parser.ToBytes(dict));
            dict.Clear();
            Assert.Throws<ArgumentException>(()=>Parser.ToBytes(dict));
            dict.Clear();
            dict["data"] = "YWJjZGVm";
            Assert.Throws<ArgumentException>(()=>Parser.ToBytes(dict));
            dict.Clear();
            dict["encoding"] = "base64";
            Assert.Throws<ArgumentException>(()=>Parser.ToBytes(dict));
            Assert.Throws<ArgumentException>(()=>Parser.ToBytes(12345));
            Assert.Throws<ArgumentException>(()=>Parser.ToBytes(null));
		}

		[Fact]
		public void TestBytesRepr()
		{
			Serializer serpent = new Serializer(indent: true, bytesRepr:true);
			byte[] bytes = { 97, 98, 99, 100, 101, 102, 0, 255, (byte)'\'', (byte)'\"' };	// abcdef\x00\xff'"
			var ser = serpent.Serialize(bytes);
			Assert.Equal("b'abcdef\\x00\\xff\\'\"'", S(strip_header(ser)));

			Parser p = new Parser();
			Ast.BytesNode parsed = (Ast.BytesNode) p.Parse(ser).Root;
			Assert.Equal(bytes, parsed.Value);
		}
		
		[Fact]
		public void TestCollection()
		{
			ICollection<int> intlist = new LinkedList<int>();
			intlist.Add(42);
			intlist.Add(43);
			Serializer serpent = new Serializer();
			var ser = serpent.Serialize(intlist);
			ser = strip_header(ser);
			Assert.Equal("[42,43]", S(ser));
			
			ser=strip_header(serpent.Serialize(new [] {42}));
			Assert.Equal("(42,)", S(ser));
			ser=strip_header(serpent.Serialize(new [] {42, 43}));
			Assert.Equal("(42,43)", S(ser));
			
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(intlist));
			Assert.Equal("[\n  42,\n  43\n]", S(ser));
			ser=strip_header(serpent.Serialize(new [] {42}));
			Assert.Equal("(\n  42,\n)", S(ser));
			ser=strip_header(serpent.Serialize(new [] {42, 43}));
			Assert.Equal("(\n  42,\n  43\n)", S(ser));
		}
		
		
		[Fact]
		public void TestIndentation()
		{
			var dict = new Dictionary<string, object>();
			var list = new List<object>
			{
				1,
				2,
				new [] {"a", "b"}
			};
			dict.Add("first", list);
			dict.Add("second", new Dictionary<int, bool> {
			         	{1, false}
			         });
			dict.Add("third", new HashSet<int> { 3, 4} );

			Serializer serpent = new Serializer {Indent = true};
			var ser = strip_header(serpent.Serialize(dict));
			string txt=@"{
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
}";
			// bit of trickery to deal with Windows/Unix line ending differences
			txt = txt.Replace("\n","\r\n");
			txt = txt.Replace("\r\r\n", "\r\n");
			string ser_txt = S(ser);
			ser_txt = ser_txt.Replace("\n", "\r\n");
			ser_txt = ser_txt.Replace("\r\r\n", "\r\n");
			Assert.Equal(txt, ser_txt);
		}
		
		[Fact]
		public void TestSorting()
		{
			Serializer serpent=new Serializer();
			object data = new List<int> { 3, 2, 1};
			var ser = strip_header(serpent.Serialize(data));
			Assert.Equal("[3,2,1]", S(ser));
			data = new [] { 3,2,1 };
			ser = strip_header(serpent.Serialize(data));
			Assert.Equal("(3,2,1)", S(ser));
			
			data = new HashSet<object> {
				42,
				"hi"
			};
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(data));
			Assert.True(S(ser)=="{\n  42,\n  'hi'\n}" || S(ser)=="{\n  'hi',\n  42\n}");

			data = new Dictionary<int, string> {
				{5, "five"},
				{3, "three"},
				{1, "one"},
				{4, "four"},
				{2, "two"}
			};
			serpent.Indent=true;
			ser = strip_header(serpent.Serialize(data));
			Assert.Equal("{\n  1: 'one',\n  2: 'two',\n  3: 'three',\n  4: 'four',\n  5: 'five'\n}", S(ser));
			
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
			Assert.Equal("{\n  'a',\n  'b',\n  'c',\n  'x',\n  'y',\n  'z'\n}", S(ser));
		}

		[Fact]
		public void TestClass()
		{
			Serializer.RegisterClass(typeof(SerializeTestClass), null);
			Serializer serpent = new Serializer(true);
			
			var obj = new SerializeTestClass
			{
				i = 99,
				s = "hi",
				x = 42
			};
			var ser = strip_header(serpent.Serialize(obj));
			Assert.Equal("{\n  '__class__': 'SerializeTestClass',\n  'i': 99,\n  'obj': None,\n  's': 'hi'\n}", S(ser));
		}

		[Fact]
		public void TestClass2()
		{
			Serializer.RegisterClass(typeof(SerializeTestClass), null);
			Serializer serpent = new Serializer(true, namespaceInClassName: true);
			object obj = new SerializeTestClass
			{
				i = 99,
				s = "hi",
				x = 42
			};
			var ser = strip_header(serpent.Serialize(obj));
			Assert.Equal("{\n  '__class__': 'Razorvine.Serpent.Test.SerializeTestClass',\n  'i': 99,\n  'obj': None,\n  's': 'hi'\n}", S(ser));
		}

		private IDictionary testclassConverter(object obj)
		{
			SerializeTestClass o = (SerializeTestClass) obj;
			IDictionary result = new Hashtable();
			result["__class@__"] = o.GetType().Name+"@";
			result["i@"] = o.i;
			result["s@"] = o.s;
			result["x@"] = o.x;
			return result;
		}
		
		[Fact]
		public void TestCustomClassDict()
		{
			Serializer.RegisterClass(typeof(SerializeTestClass), testclassConverter);
			Serializer serpent = new Serializer(true);
			
			var obj = new SerializeTestClass
			{
				i = 99,
				s = "hi",
				x = 42
			};
			var ser = strip_header(serpent.Serialize(obj));
			Assert.Equal("{\n  '__class@__': 'SerializeTestClass@',\n  'i@': 99,\n  's@': 'hi',\n  'x@': 42\n}", S(ser));
		}
		
		[Fact]
		public void TestStruct()
		{
			Serializer serpent = new Serializer(true);
			
			var obj2 = new SerializeTestStruct
			{
				i = 99,
				s = "hi",
				x = 42
			};
			var ser = strip_header(serpent.Serialize(obj2));
			Assert.Equal("{\n  '__class__': 'SerializeTestStruct',\n  'i': 99,\n  's': 'hi'\n}", S(ser));
		}
		
		[Fact]
		public void TestStruct2()
		{
			Serializer serpent = new Serializer(true, namespaceInClassName: true);
			
			var obj2 = new SerializeTestStruct
			{
				i = 99,
				s = "hi",
				x = 42
			};
			var ser = strip_header(serpent.Serialize(obj2));
			Assert.Equal("{\n  '__class__': 'Razorvine.Serpent.Test.SerializeTestStruct',\n  'i': 99,\n  's': 'hi'\n}", S(ser));
		}

		[Fact]
		public void TestAnonymousClass()
		{
			Serializer serpent = new Serializer(true);
			object obj = new {
				Name="Harry",
				Age=33,
				Country="NL"
			};
			
			var ser = strip_header(serpent.Serialize(obj));
			Assert.Equal("{\n  'Age': 33,\n  'Country': 'NL',\n  'Name': 'Harry'\n}", S(ser));
		}
		
		[Fact]
		public void TestDateTime()
		{
			Serializer serpent = new Serializer();
			
			DateTime date = new DateTime(2013, 1, 20, 23, 59, 45, 999, DateTimeKind.Local);
			var ser = strip_header(serpent.Serialize(date));
			Assert.Equal("'2013-01-20T23:59:45.999'", S(ser));
			
			date = new DateTime(2013, 1, 20, 23, 59, 45, 999, DateTimeKind.Utc);
			ser = strip_header(serpent.Serialize(date));
			Assert.Equal("'2013-01-20T23:59:45.999'", S(ser));

			date = new DateTime(2013, 1, 20, 23, 59, 45, 999, DateTimeKind.Unspecified);
			ser = strip_header(serpent.Serialize(date));
			Assert.Equal("'2013-01-20T23:59:45.999'", S(ser));

			date = new DateTime(2013, 1, 20, 23, 59, 45);
			ser = strip_header(serpent.Serialize(date));
			Assert.Equal("'2013-01-20T23:59:45'", S(ser));
		
			TimeSpan timespan = new TimeSpan(1, 10, 20, 30, 999);
			ser = strip_header(serpent.Serialize(timespan));
			Assert.Equal("123630.999", S(ser));
		}
		
		[Fact]
		public void TestDateTimeOffset()
		{
			Serializer serpent = new Serializer();

			DateTimeOffset date = new DateTimeOffset(2013, 1, 20, 23, 59, 45, 999, TimeSpan.FromHours(+2));
			var ser = strip_header(serpent.Serialize(date));
			Assert.Equal("'2013-01-20T23:59:45.999+02:00'", S(ser));
			
			date = new DateTimeOffset(2013, 5, 10, 13, 59, 45, TimeSpan.FromHours(+2));
			ser = strip_header(serpent.Serialize(date));
			Assert.Equal("'2013-05-10T13:59:45+02:00'", S(ser));
		}

		[Fact]
		public void TestException()
		{
			Exception x = new ApplicationException("errormessage");
			Serializer serpent = new Serializer(true);
			var ser = strip_header(serpent.Serialize(x));
			Assert.Equal("{\n  '__class__': 'ApplicationException',\n  '__exception__': True,\n  'args': (\n    'errormessage',\n  ),\n  'attributes': {}\n}", S(ser));

			x.Data["custom_attribute"]=999;
			ser = strip_header(serpent.Serialize(x));
			Assert.Equal("{\n  '__class__': 'ApplicationException',\n  '__exception__': True,\n  'args': (\n    'errormessage',\n  ),\n  'attributes': {\n    'custom_attribute': 999\n  }\n}", S(ser));
		}
		
		[Fact]
		public void TestExceptionWithNamespace()
		{
			Exception x = new ApplicationException("errormessage");
			Serializer serpent = new Serializer(true, namespaceInClassName: true);
			var ser = strip_header(serpent.Serialize(x));
			Assert.Equal("{\n  '__class__': 'System.ApplicationException',\n  '__exception__': True,\n  'args': (\n    'errormessage',\n  ),\n  'attributes': {}\n}", S(ser));
		}

		private enum FooType {
			Jarjar
		}

		[Fact]
		public void TestEnum()
		{
			const FooType e = FooType.Jarjar;
			Serializer serpent = new Serializer();
			var ser = strip_header(serpent.Serialize(e));
			Assert.Equal("'Jarjar'", S(ser));
		}


		private interface IBaseInterface {}

		private interface ISubInterface : IBaseInterface {}

		private class BaseClassWithInterface : IBaseInterface {}

		private class SubClassWithInterface : BaseClassWithInterface, ISubInterface {}
		
		private abstract class AbstractBaseClass {}

		private class ConcreteSubClass : AbstractBaseClass {}

		private IDictionary AnyClassSerializer(object arg)
		{
			IDictionary result = new Hashtable();
			result["(SUB)CLASS"] = arg.GetType().Name;
			return result;
		}

		[Fact]
		public void testAbstractBaseClassHierarchyPickler()
		{
			ConcreteSubClass c = new ConcreteSubClass();
			Serializer serpent = new Serializer();
			serpent.Serialize(c);
			
			Serializer.RegisterClass(typeof(AbstractBaseClass), AnyClassSerializer);
			var data = serpent.Serialize(c);
			Assert.Equal("{'(SUB)CLASS':'ConcreteSubClass'}", S(strip_header(data)));
		}
		
		[Fact]
		public void TestInterfaceHierarchyPickler()
		{
			BaseClassWithInterface b = new BaseClassWithInterface();
			SubClassWithInterface sub = new SubClassWithInterface();
			Serializer serpent = new Serializer();
			serpent.Serialize(b);
			serpent.Serialize(sub);
			Serializer.RegisterClass(typeof(IBaseInterface), AnyClassSerializer);
			var data = serpent.Serialize(b);
			Assert.Equal("{'(SUB)CLASS':'BaseClassWithInterface'}", S(strip_header(data)));
			data = serpent.Serialize(sub);
			Assert.Equal("{'(SUB)CLASS':'SubClassWithInterface'}", S(strip_header(data)));
		}			
	}

	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	public class SerializeTestClass
	{
		public int x;
		public string s {get; set;}
		public int i {get; set;}
		public object obj {get; set;}

		protected bool Equals(SerializeTestClass other)
		{
			return x == other.x && string.Equals(s, other.s) && i == other.i && Equals(obj, other.obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = x;
				hashCode = (hashCode * 397) ^ (s != null ? s.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ i;
				hashCode = (hashCode * 397) ^ (obj != null ? obj.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
	
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	public struct SerializeTestStruct
	{
		// ReSharper disable once NotAccessedField.Global
		public int x;
		public string s {get; set;}
		public int i {get; set;}

		public bool Equals(SerializeTestStruct other)
		{
			return x == other.x && string.Equals(s, other.s) && i == other.i;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = x;
				hashCode = (hashCode * 397) ^ (s != null ? s.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ i;
				return hashCode;
			}
		}
	}
}
