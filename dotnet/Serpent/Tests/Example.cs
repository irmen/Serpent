using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

// ReSharper disable CheckNamespace

namespace Razorvine.Serpent.Test
{
	/// <summary>
	///     Example usage.
	/// </summary>
	public class Example
    {
        [Fact(Skip = "this is an example")]
        public void ExampleUsage()
        {
            Console.WriteLine("using serpent library version {0}", LibraryVersion.Version);

            var data = new Dictionary<string, object>
            {
                {"tuple", new[] {1, 2, 3}},
                {"date", DateTime.Now},
                {"set", new HashSet<string> {"a", "b", "c"}},
                {
                    "class", new SampleClass
                    {
                        Name = "Sally",
                        Age = 26
                    }
                }
            };

            // serialize data structure to bytes
            var serpent = new Serializer(true);
            var ser = serpent.Serialize(data);
            // print it on the screen, but normally you'd store byte bytes in a file or transfer them across a network connection
            Console.WriteLine("Serialized:");
            Console.WriteLine(Encoding.UTF8.GetString(ser));

            // parse the serialized bytes back into an abstract syntax tree of the datastructure
            var parser = new Parser();
            var ast = parser.Parse(ser);
            Console.WriteLine("\nParsed AST:");
            Console.WriteLine(ast.Root.ToString());

            // print debug representation
            var dv = new DebugVisitor();
            ast.Accept(dv);
            Console.WriteLine("DEBUG string representation:");
            Console.WriteLine(dv.ToString());

            // turn the Ast into regular .net objects
            var dict = (IDictionary) ast.GetData();
            // You can get the data out of the Ast manually as well, by using the supplied visitor:
            // var visitor = new ObjectifyVisitor();
            // ast.Accept(visitor);
            // var dict = (IDictionary) visitor.GetObject();

            // print the results
            Console.WriteLine("PARSED results:");
            Console.Write("tuple items: ");
            var tuple = (object[]) dict["tuple"];
            Console.WriteLine(string.Join(", ", tuple.Select(e => e.ToString()).ToArray()));
            Console.WriteLine("date: {0}", dict["date"]);
            Console.Write("set items: ");
            var set = (HashSet<object>) dict["set"];
            Console.WriteLine(string.Join(", ", set.Select(e => e.ToString()).ToArray()));
            Console.WriteLine("class attributes:");
            var clazz = (IDictionary) dict["class"]; // custom classes are serialized as dicts
            Console.WriteLine("  type: {0}", clazz["__class__"]);
            Console.WriteLine("  name: {0}", clazz["name"]);
            Console.WriteLine("  age: {0}", clazz["age"]);

            Console.WriteLine("");

            // parse and print the example file
            ser = File.ReadAllBytes("testserpent.utf8.bin");
            ast = parser.Parse(ser);
            dv = new DebugVisitor();
            ast.Accept(dv);
            Console.WriteLine("DEBUG string representation of the test file:");
            Console.WriteLine(dv.ToString());
        }

        [Serializable]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class SampleClass
        {
            public int Age { get; set; }
            public string Name { get; set; }
        }
    }
}