/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright 2014, Irmen de Jong (irmen@razorvine.net)
/// Software license: "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;
using System.Collections.Generic;
using System.Collections;

namespace Razorvine.Serpent
{
	/// <summary>
	/// Ast nodevisitor that turns the AST into actual .NET objects (array, int, IDictionary, string, etc...)
	/// </summary>
	public class ObjectifyVisitor: Ast.INodeVisitor
	{
		private Stack<object> generated = new Stack<object>();
		private Func<IDictionary, object> dictToInstance = null;

		/// <summary>
		/// Create the visitor that converts AST in actual objects.
		/// </summary>
		public ObjectifyVisitor()
		{
		}
		
		/// <summary>
		/// Create the visitor that converts AST in actual objects.
		/// </summary>
		/// <param name="dictToInstance">functin to convert dicts to actual instances for a class,
		/// instead of leaving them as dictionaries. Requires the __class__ key to be present
		/// in the dict node. If it returns null, the normal processing is done.</param>
		public ObjectifyVisitor(Func<IDictionary, object> dictToInstance)
		{
			this.dictToInstance = dictToInstance;
		}

		/// <summary>
		/// get the resulting object tree.
		/// </summary>
		public object GetObject()
		{
			return generated.Pop();
		}
		
		public void Visit(Ast.ComplexNumberNode complex)
		{
			generated.Push(new ComplexNumber(complex.Real, complex.Imaginary));
		}
		
		public void Visit(Ast.DictNode dict)
		{
			IDictionary obj = new Hashtable(dict.Elements.Count);
			foreach(Ast.KeyValueNode kv in dict.Elements)
			{
				kv.Key.Accept(this);
				object key = generated.Pop();
				kv.Value.Accept(this);
				object value = generated.Pop();
				obj[key] = value;
			}

			if(dictToInstance==null || !obj.Contains("__class__"))
			{
				generated.Push(obj);
			}
			else
			{
				object result = dictToInstance(obj);
				if(result==null)
					generated.Push(obj);
				else
					generated.Push(result);
			}
		}
		
		public void Visit(Ast.ListNode list)
		{
			IList<object> obj = new List<object>(list.Elements.Count);
			foreach(Ast.INode node in list.Elements)
			{
				node.Accept(this);
				obj.Add(generated.Pop());
			}
			generated.Push(obj);
		}
		
		public void Visit(Ast.NoneNode none)
		{
			generated.Push(null);
		}
		
		public void Visit(Ast.IntegerNode value)
		{
			generated.Push(value.Value);
		}
		
		public void Visit(Ast.LongNode value)
		{
			generated.Push(value.Value);
		}
		
		public void Visit(Ast.DoubleNode value)
		{
			generated.Push(value.Value);
		}
		
		public void Visit(Ast.BooleanNode value)
		{
			generated.Push(value.Value);
		}
		
		public void Visit(Ast.StringNode value)
		{
			generated.Push(value.Value);
		}
		
		public void Visit(Ast.DecimalNode value)
		{
			generated.Push(value.Value);
		}
		
		public void Visit(Ast.SetNode setnode)
		{
			HashSet<object> obj = new HashSet<object>();
			foreach(Ast.INode node in setnode.Elements)
			{
				node.Accept(this);
				obj.Add(generated.Pop());
			}
			generated.Push(obj);
		}
		
		public void Visit(Ast.TupleNode tuple)
		{
			object[] array = new object[tuple.Elements.Count];
			int index=0;
			foreach(Ast.INode node in tuple.Elements)
			{
				node.Accept(this);
				array[index++] = generated.Pop();
			}
			generated.Push(array);
		}
	}
}
