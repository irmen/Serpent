
import java.io.IOException;
import java.io.Serializable;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import net.razorvine.serpent.Serializer;

public class SerpentExample {

	enum Foo {
		Foobar,
		Jarjar
	}

	public class SampleClass implements Serializable
	{
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
	
	public static void main(String[] args) {
		SerpentExample t=new SerpentExample();
		try {
			t.run();
		} catch (IOException e) {
			e.printStackTrace();
		}
	}
	
	public void run() throws IOException
	{
		// some example use of Serpent
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
		Serializer serpent = new Serializer(true, true);
		byte[] ser = serpent.serialize(data);
		// print it on the screen, but normally you'd store byte bytes in a file or transfer them across a network connection
		System.out.println("Serialized:");
		System.out.println(new String(ser, "utf-8"));  
		
		/*** @todo
		// parse the serialized bytes back into an abstract syntax tree of the datastructure
		Parser parser = new Parser();
		Ast ast = parser.Parse(ser);
		System.out.println("\nParsed AST:");
		System.out.println(ast.Root.toString());
		
		// turn the Ast into regular .net objects
		ObjectifyVistior visitor = new ObjectifyVisitor();
		ast.Accept(visitor);
		var dict = (IDictionary<Object, Object>) visitor.GetObject();

		// print the results
		System.out.print("Tuple items: ");
		Object[] tuple = (object[]) dict["tuple"];
		System.out.println(string.Join(", ", tuple.Select(e=>e.ToString()).ToArray()));
		System.out.println("Date: {0}", dict["date"]);
		System.out.print("Set items: ");
		HashSet<Object> set = (HashSet<Object>) dict["set"];
		System.out.println(string.Join(", ", set.Select(e=>e.ToString()).ToArray()));
		System.out.println("Class attributes:");
		var clazz = (IDictionary<Object, Object>) dict["class"];	// custom classes are serialized as dicts
		System.out.println("type: {0}", clazz["__class__"]);
		System.out.println("name: {0}", clazz["name"]);
		System.out.println("age: {0}", clazz["age"]);
		***/
	}
}
