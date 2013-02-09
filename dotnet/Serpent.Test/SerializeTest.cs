/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright 2013, Irmen de Jong (irmen@razorvine.net)
/// This code is open-source, but licensed under the "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using Razorvine.Serpent.Serializing;

namespace Razorvine.Serpent.Test
{
	[TestFixture]
	public class SerializeTest
	{
		[Test]
		public void TestHeader()
		{
			Serializer ser = new Serializer();
			byte[] data = ser.Serialize(null);
			Assert.AreEqual(35, data[0]);
			string strdata = Encoding.UTF8.GetString(data);
			string header = "# serpent utf-8 dotnet-cli"+Environment.Version.ToString(2);
			Assert.AreEqual(header, strdata.Split('\n')[0]);
		}
		
		
		[Test]
		public void TestStuff()
		{
			Serializer ser=new Serializer();
			byte[] result = ser.Serialize("blerp");
			Console.WriteLine(Encoding.UTF8.GetString(result));
			result = ser.Serialize(Guid.NewGuid());
			Console.WriteLine(Encoding.UTF8.GetString(result));
			result = ser.Serialize(123.456789);
			Console.WriteLine(Encoding.UTF8.GetString(result));

			result = ser.Serialize("quote\"");
			Console.WriteLine(Encoding.UTF8.GetString(result));

			result = ser.Serialize("apo'");
			Console.WriteLine(Encoding.UTF8.GetString(result));

			result = ser.Serialize("both'\"");
			Console.WriteLine(Encoding.UTF8.GetString(result));

			result = ser.Serialize(123456789.987654321987654321987654321987654321m);
			Console.WriteLine(Encoding.UTF8.GetString(result));
		}
	}
}
