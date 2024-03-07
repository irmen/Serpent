using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable CheckNamespace

namespace Razorvine.Serpent.Test
{
	/// <summary>
	///     Example usage.
	/// </summary>
	public class Example
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public Example(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact(Skip = "this is an example")]
        public void ExampleUsage()
        {
            _testOutputHelper.WriteLine("using serpent library version {0}", LibraryVersion.Version);

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
            _testOutputHelper.WriteLine("Serialized:");
            _testOutputHelper.WriteLine(Encoding.UTF8.GetString(ser));

            // parse the serialized bytes back into an abstract syntax tree of the datastructure
            var parser = new Parser();
            var ast = parser.Parse(ser);
            _testOutputHelper.WriteLine("\nParsed AST:");
            _testOutputHelper.WriteLine(ast.Root.ToString());

            // print debug representation
            var dv = new DebugVisitor();
            ast.Accept(dv);
            _testOutputHelper.WriteLine("DEBUG string representation:");
            _testOutputHelper.WriteLine(dv.ToString());

            // turn the Ast into regular .net objects
            var dict = (IDictionary) ast.GetData();
            // You can get the data out of the Ast manually as well, by using the supplied visitor:
            // var visitor = new ObjectifyVisitor();
            // ast.Accept(visitor);
            // var dict = (IDictionary) visitor.GetObject();

            // print the results
            _testOutputHelper.WriteLine("PARSED results:");
            _testOutputHelper.WriteLine("tuple items: ");
            var tuple = (object[]) dict["tuple"];
            _testOutputHelper.WriteLine(string.Join(", ", tuple.Select(e => e.ToString()).ToArray()));
            _testOutputHelper.WriteLine("date: {0}", dict["date"]);
            _testOutputHelper.WriteLine("set items: ");
            var set = (HashSet<object>) dict["set"];
            _testOutputHelper.WriteLine(string.Join(", ", set.Select(e => e.ToString()).ToArray()));
            _testOutputHelper.WriteLine("class attributes:");
            var clazz = (IDictionary) dict["class"]; // custom classes are serialized as dicts
            _testOutputHelper.WriteLine("  type: {0}", clazz["__class__"]);
            _testOutputHelper.WriteLine("  name: {0}", clazz["name"]);
            _testOutputHelper.WriteLine("  age: {0}", clazz["age"]);

            _testOutputHelper.WriteLine("");

            // parse and print the example file
            ser = File.ReadAllBytes("testserpent.utf8.bin");
            ast = parser.Parse(ser);
            dv = new DebugVisitor();
            ast.Accept(dv);
            _testOutputHelper.WriteLine("DEBUG string representation of the test file:");
            _testOutputHelper.WriteLine(dv.ToString());
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