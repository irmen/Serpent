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

namespace Razorvine.Serpent.Test
{
	[TestFixture]
	public class CycleTest
	{
		[Test]
		public void testTupleOk()
		{
			var ser = new Serializer();
			var t = new int[] {1,2,3};
			var d = new object[] {t,t,t};
			var data = ser.Serialize(d);
			var parser = new Parser();
	        var ast = parser.Parse(data);
		}

		[Test]
		public void testListOk()
		{
			var ser = new Serializer();
			var t = new List<int>();
			t.Add(1);
			t.Add(2);
			t.Add(3);
			var d = new List<Object>();
			d.Add(t);
			d.Add(t);
			d.Add(t);
			var data = ser.Serialize(d);
			var parser = new Parser();
	        var ast = parser.Parse(data);
		}

		[Test]
		public void testDictOk()
		{
			var ser = new Serializer();
			var t = new Hashtable();
			t["a"] = 1;
			var d = new Hashtable();
			d["x"] = t;
			d["y"] = t;
			d["z"] = t;
			var data = ser.Serialize(d);
			var parser = new Parser();
	        var ast = parser.Parse(data);
		}

		[Test]
		[Ignore("stackoverflow")]
		public void testListCycle()
		{
			var ser = new Serializer();
			var d = new List<Object>();
			d.Add(1);
			d.Add(2);
			d.Add(d);
			var data = ser.Serialize(d);
		}

		[Test]
		[Ignore("stackoverflow")]
		public void testDictCycle()
		{
			var ser = new Serializer();
			var d = new Hashtable();
			d["x"] = 1;
			d["y"] = 2;
			d["z"] = d;
			var data = ser.Serialize(d);
		}
		
		[Test]
		[Ignore("stackoverflow")]
		public void testClassCycle()
		{
			var ser = new Serializer();
			var d = new SerializeTestClass();
			d.x = 42;
			d.i = 99;
			d.s = "hello";
			d.obj = d;
			var data = ser.Serialize(d);
		}
	}
}
