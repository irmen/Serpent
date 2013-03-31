package net.razorvine.serpent;

import java.io.FileOutputStream;
import java.io.IOException;
import java.io.Serializable;
import java.util.LinkedList;
import java.util.List;

public class MainTest {

	enum Foo {
		Foobar,
		Jarjar
	}

	public class CustomClass implements Serializable
	{
		int i=42;
		String v="hi";
		
		public int getAge()
		{
			return i;
		}
		
		public String getName()
		{
			return v;
		}
	}
	
	public static void main(String[] args) throws IOException {
		MainTest t=new MainTest();
		t.run();
	}
	
	public void run() throws IOException
	{
		Serializer ser = new Serializer();
		List<String> list = new LinkedList<String>();
		list.add("one");
		list.add("two");
		list.add("three");
		byte[] output = ser.serialize(list);

		FileOutputStream fos = new FileOutputStream("output.utf8.bin");
		fos.write(output);
		fos.close();
	}

}
