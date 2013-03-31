package net.razorvine.serpent;

import java.io.FileOutputStream;
import java.io.IOException;
import java.io.Serializable;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.Set;

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
	
	public static void main(String[] args) {
		MainTest t=new MainTest();
		t.run();
	}
	
	public void run()
	{
		Serializer ser = new Serializer();
		ser.indent=true;
		List<String> list = new LinkedList<String>();
		list.add("one");
		list.add("two");
		list.add("three");
		
		Set<Object> set = new HashSet<Object>();
		set.add("x");
		set.add("y");
		set.add("z");
		set.add("b");
		set.add("c");
		set.add("a");
		
		Map<Object, String> map = new HashMap<Object, String>();
		map.put("c", "blurp1");
		map.put("b", "blurp2");
		map.put("a", "blurp3");
		map.put("z", "blurp4");
		map.put("x", "blurp5");
		map.put("y", "blurp6");
		
		
		byte[] output = ser.serialize(set);

		FileOutputStream fos;
		try {
			fos = new FileOutputStream("output.utf8.bin");
			fos.write(output);
			fos.close();
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}

}
