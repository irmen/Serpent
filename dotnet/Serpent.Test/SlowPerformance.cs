using System;
using NUnit.Framework;

namespace Razorvine.Serpent.Test
{

[TestFixture]
public class SlowPerformance {

	// tests the currently very slow performance of the SeekableStringReader.ReadUntil method
	// TODO fix this issue (#12) and then this can be removed

	[Test]
	public static void testManyFloats()
	{
		int amount = 20000;
		double[] array = new double[amount];
		for(int i=0; i<amount; ++i)
			array[i] = 12345.987654;
		
		Serializer serpent = new Serializer();
		Parser parser = new Parser();
		DateTime start = DateTime.Now;
		byte[] data = serpent.Serialize(array);
		double duration = (DateTime.Now - start).TotalMilliseconds;
		Console.WriteLine(""+duration+"  datalen="+data.Length);
		start = DateTime.Now;
		object[] values = (object[]) parser.Parse(data).GetData();
		duration = (DateTime.Now - start).TotalMilliseconds;
		Console.WriteLine(""+duration+"  valuelen="+values.Length);
	}

	[Test]
	public static void testManyInts()
	{
		int amount=20000;
		int[] array = new int[amount];
		for(int i=0; i<amount; ++i)
			array[i] = 12345;
		
		Serializer serpent = new Serializer();
		Parser parser = new Parser();
		DateTime start = DateTime.Now;
		byte[] data = serpent.Serialize(array);
		double duration = (DateTime.Now - start).TotalMilliseconds;
		Console.WriteLine(""+duration+"  datalen="+data.Length);
		start = DateTime.Now;
		object[] values = (object[]) parser.Parse(data).GetData();
		duration = (DateTime.Now - start).TotalMilliseconds;
		Console.WriteLine(""+duration+"  valuelen="+values.Length);
	}	
}
}
