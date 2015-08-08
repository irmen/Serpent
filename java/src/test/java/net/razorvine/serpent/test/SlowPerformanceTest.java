package net.razorvine.serpent.test;

import java.io.IOException;

import net.razorvine.serpent.Parser;
import net.razorvine.serpent.Serializer;

import org.junit.Ignore;
import org.junit.Test;


public class SlowPerformanceTest {

	// tests some performance regressions when they occur

	@Test
	@Ignore
	public void TestManyFloats() throws IOException
	{
//		System.out.println("enter to start manyfloats");
//		System.in.read();
		
		int amount = 500000;
		double[] array = new double[amount];
		for(int i=0; i<amount; ++i)
			array[i] = 12345.987654;
		
		Serializer serpent = new Serializer();
		Parser parser = new Parser();
		long start = System.currentTimeMillis();
		byte[] data = serpent.serialize(array);
		long duration = System.currentTimeMillis()-start;
		System.out.println(""+duration+"  datalen="+data.length);
		start = System.currentTimeMillis();
		Object[] values = (Object[]) parser.parse(data).getData(); 
		duration = System.currentTimeMillis()-start;
		System.out.println(""+duration+"  valuelen="+values.length);
	}

	@Test
	@Ignore
	public void TestManyInts() throws IOException
	{
//		System.out.println("enter to start manyints");
//		System.in.read();

		int amount=500000;
		int[] array = new int[amount];
		for(int i=0; i<amount; ++i)
			array[i] = 12345;
		
		Serializer serpent = new Serializer();
		Parser parser = new Parser();
		long start = System.currentTimeMillis();
		byte[] data = serpent.serialize(array);
		long duration = System.currentTimeMillis()-start;
		System.out.println(""+duration+"  datalen="+data.length);
		start = System.currentTimeMillis();
		Object[] values = (Object[]) parser.parse(data).getData(); 
		duration = System.currentTimeMillis()-start;
		System.out.println(""+duration+"  valuelen="+values.length);
	}	
}
