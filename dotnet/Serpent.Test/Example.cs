using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Linq;
using NUnit.Framework;
using Razorvine.Serpent;

namespace Razorvine.Serpent.Test
{
	/// <summary>
	/// Example usage.
	/// </summary>
	[TestFixture]
	[Ignore("example")]
	public class Example 
	{
		[Test]
		public void ExampleUsage()
		{
			var data = new Dictionary<string, object> {
				{"tuple", new int[] { 1,2,3 } },
				{"date", DateTime.Now},
				{"set", new HashSet<string> { "a", "b", "c" } },
				{"class", new SampleClass() {
						name = "Sally",
						age = 26
					}}
			};
			
			// serialize data structure to bytes
			Serializer serpent = new Serializer(indent: true);
			byte[] ser = serpent.Serialize(data);
			// print it on the screen, but normally you'd store byte bytes in a file or transfer them across a network connection
			Console.WriteLine("Serialized:");
			Console.WriteLine(Encoding.UTF8.GetString(ser));  
			
			// parse the serialized bytes back into an abstract syntax tree of the datastructure
			Parser parser = new Parser();
			Ast ast = parser.Parse(ser);
			Console.WriteLine("\nParsed AST:");
			Console.WriteLine(ast.Root.ToString());
			
			// turn the Ast into regular .net objects
			var visitor = new ObjectifyVisitor();
			ast.Accept(visitor);
			var dict = (IDictionary<object, object>) visitor.GetObject();

			// print the results
			Console.Write("Tuple items: ");
			object[] tuple = (object[]) dict["tuple"];
			Console.WriteLine(string.Join(", ", tuple.Select(e=>e.ToString()).ToArray()));
			Console.WriteLine("Date: {0}", dict["date"]);
			Console.Write("Set items: ");
			HashSet<object> set = (HashSet<object>) dict["set"];
			Console.WriteLine(string.Join(", ", set.Select(e=>e.ToString()).ToArray()));
			Console.WriteLine("Class attributes:");
			var clazz = (IDictionary<object, object>) dict["class"];	// custom classes are serialized as dicts
			Console.WriteLine("type: {0}", clazz["__class__"]);
			Console.WriteLine("name: {0}", clazz["name"]);
			Console.WriteLine("age: {0}", clazz["age"]);
		}
		
		[Serializable]
		public class SampleClass
		{
			public int age {get;set;}
			public string name {get;set;}
		}
	}
}
