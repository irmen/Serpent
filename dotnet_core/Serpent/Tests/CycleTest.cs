/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright Irmen de Jong (irmen@razorvine.net)
/// Software license: "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Razorvine.Serpent.Test
{
	[TestClass]
	public class CycleTest
	{
		[TestMethod]
		public void testTupleOk()
		{
			var ser = new Serializer();
			var t = new int[] {1,2,3};
			var d = new object[] {t,t,t};
			var data = ser.Serialize(d);
			var parser = new Parser();
	        parser.Parse(data);
		}

		[TestMethod]
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
	        parser.Parse(data);
		}

		[TestMethod]
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
	        parser.Parse(data);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void testListCycle()
		{
			var ser = new Serializer();
			var d = new List<Object>();
			d.Add(1);
			d.Add(2);
			d.Add(d);
			ser.Serialize(d);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void testDictCycle()
		{
			var ser = new Serializer();
			var d = new Hashtable();
			d["x"] = 1;
			d["y"] = 2;
			d["z"] = d;
			ser.Serialize(d);
		}
		
		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void testClassCycle()
		{
			var ser = new Serializer();
			var d = new SerializeTestClass();
			d.x = 42;
			d.i = 99;
			d.s = "hello";
			d.obj = d;
			ser.Serialize(d);
		}
		
		[TestMethod]
		public void testMaxLevel()
		{
			Serializer ser = new Serializer();
			Assert.AreEqual(500, ser.MaximumLevel);
			
			Object[] array = new Object[] {
				"level1",
				new Object[] {
					"level2",
					new Object[] {
						"level3",
						new Object[] {
							"level 4"
						}
					}
				}
			};
	
			ser.MaximumLevel = 4;
			ser.Serialize(array);		// should work
			
			ser.MaximumLevel = 3;
			try {
				ser.Serialize(array);
				Assert.Fail("should fail");
			} catch(ArgumentException x) {
				Assert.IsTrue(x.Message.Contains("too deep"));
			}
		}
	}
}
