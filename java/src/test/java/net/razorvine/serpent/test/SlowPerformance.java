package net.razorvine.serpent.test;

import java.io.IOException;

import net.razorvine.serpent.Parser;
import net.razorvine.serpent.Serializer;
import org.junit.Test;


public class SlowPerformance {

	// tests the currently very slow performance of the SeekableStringReader.ReadUntil method
	// TODO fix this issue (#12) and then this can be removed

	@Test
	public void testManyFloats() throws IOException
	{
		int amount = 20000;
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
	public void testManyInts() throws IOException
	{
		int amount=20000;
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
