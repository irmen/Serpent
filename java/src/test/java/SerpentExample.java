
import java.io.DataInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.Serializable;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

import net.razorvine.serpent.DebugVisitor;
import net.razorvine.serpent.LibraryVersion;
import net.razorvine.serpent.Parser;
import net.razorvine.serpent.Serializer;
import net.razorvine.serpent.ast.Ast;

public class SerpentExample {

	enum Foo {
		Foobar,
		Jarjar
	}

	public static void main(String[] args) {
		SerpentExample t=new SerpentExample();
		try {
			t.run();
		} catch (IOException e) {
			e.printStackTrace();
		}
	}

	@SuppressWarnings("unchecked")
	public void run() throws IOException
	{
		// some example use of Serpent
		System.out.println("Using serpent library version "+LibraryVersion.VERSION);

		Map<String, Object> data = new HashMap<String, Object>();
		data.put("tuple", new int[] { 1,2,3 });
		data.put("date", new java.util.Date());

		Set<String> set = new HashSet<String>();
		set.add("c");
		set.add("b");
		set.add("a");
		data.put("set", set);
		data.put("class", new SampleClass("Sally", 26));

		// serialize data structure to bytes
		Serializer serpent = new Serializer(true, false);
		byte[] ser = serpent.serialize(data);
		// print it on the screen, but normally you'd store byte bytes in a file or transfer them across a network connection
		System.out.println("Serialized:");
		System.out.println(new String(ser, "utf-8"));

		// parse the serialized bytes back into an abstract syntax tree of the datastructure
		Parser parser = new Parser();
		Ast ast = parser.parse(ser);
		System.out.println("\nParsed AST:");
		System.out.println(ast.root.toString());

		// print debug representation
		DebugVisitor dv = new DebugVisitor();
		ast.accept(dv);
		System.out.println("DEBUG string representation:");
		System.out.println(dv.toString());

		// turn the Ast into regular Java objects
		Map<Object, Object> dict = (Map<Object, Object>)ast.getData();
		// You can get the data out of the Ast manually as well, by using the supplied visitor:
		// ObjectifyVisitor visitor = new ObjectifyVisitor();
		// ast.accept(visitor);
		// Map<Object, Object> dict = (Map<Object, Object>) visitor.getObject();

		// print the results
		System.out.println("PARSED results:");
		System.out.print("tuple items: ");
		Object[] tuple = (Object[]) dict.get("tuple");
		for(Object o: tuple)
			System.out.print(" "+o.toString()+",");
		System.out.println("");
		System.out.println("date: "+dict.get("date"));
		System.out.print("set items: ");
		Set<Object> set2 = (Set<Object>) dict.get("set");
		for(Object o: set2)
			System.out.print(" "+o.toString()+",");
		System.out.println("");
		System.out.println("class attributes:");
		Map<Object, Object> clazz = (Map<Object, Object>) dict.get("class");	// custom classes are serialized as dicts
		System.out.println("  type: "+clazz.get("__class__"));
		System.out.println("  name: "+clazz.get("name"));
		System.out.println("  age: "+clazz.get("age"));

		System.out.println("");

		// parse and print the example file
		File testdatafile = new File("src/test/java/testserpent.utf8.bin");
		ser = new byte[(int) testdatafile.length()];
		FileInputStream fis=new FileInputStream(testdatafile);
		DataInputStream dis = new DataInputStream(fis);
		dis.readFully(ser);
		dis.close();
		fis.close();
		ast = parser.parse(ser);
		dv = new DebugVisitor();
		ast.accept(dv);
		System.out.println("DEBUG string representation of the test file:");
		System.out.println(dv.toString());
	}

	public class SampleClass implements Serializable
	{
		private static final long serialVersionUID = -782424804184940436L;
		int a;
		String n;

		public SampleClass(String name, int age)
		{
			a=age;
			n=name;
		}

		public int getAge()
		{
			return a;
		}

		public String getName()
		{
			return n;
		}
	}
}
