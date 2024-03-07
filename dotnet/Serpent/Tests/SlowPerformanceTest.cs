using System;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable CheckNamespace

namespace Razorvine.Serpent.Test
{

public class SlowPerformanceTest {
	private readonly ITestOutputHelper _testOutputHelper;

	public SlowPerformanceTest(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper;
	}

	// tests some performance regressions when they occur

	[Fact(Skip="number parse performance in long lists has been resolved")]
	public void TestManyFloats()
	{
		const int amount = 200000;
		var array = new double[amount];
		for(int i=0; i<amount; ++i)
			array[i] = 12345.987654;
		
		Serializer serpent = new Serializer();
		Parser parser = new Parser();
		DateTime start = DateTime.Now;
		var data = serpent.Serialize(array);
		double duration = (DateTime.Now - start).TotalMilliseconds;
		_testOutputHelper.WriteLine(""+duration+"  datalen="+data.Length);
		start = DateTime.Now;
		var values = (object[]) parser.Parse(data).GetData();
		duration = (DateTime.Now - start).TotalMilliseconds;
		_testOutputHelper.WriteLine(""+duration+"  valuelen="+values.Length);
	}

	[Fact(Skip="number parse performance in long lists has been resolved")]
	public void TestManyInts()
	{
		const int amount=200000;
		var array = new int[amount];
		for(int i=0; i<amount; ++i)
			array[i] = 12345;
		
		Serializer serpent = new Serializer();
		Parser parser = new Parser();
		DateTime start = DateTime.Now;
		var data = serpent.Serialize(array);
		double duration = (DateTime.Now - start).TotalMilliseconds;
		_testOutputHelper.WriteLine(""+duration+"  datalen="+data.Length);
		start = DateTime.Now;
		var values = (object[]) parser.Parse(data).GetData();
		duration = (DateTime.Now - start).TotalMilliseconds;
		_testOutputHelper.WriteLine(""+duration+"  valuelen="+values.Length);
	}	
}
}
