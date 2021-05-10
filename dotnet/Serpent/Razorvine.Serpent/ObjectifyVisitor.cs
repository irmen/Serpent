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
		private readonly Stack<object> _generated = new Stack<object>();
		private readonly Func<IDictionary, object> _dictToInstance;

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
			_dictToInstance = dictToInstance;
		}

		/// <summary>
		/// get the resulting object tree.
		/// </summary>
		public object GetObject()
		{
			return _generated.Pop();
		}
		
		public void Visit(Ast.ComplexNumberNode complex)
		{
			_generated.Push(new ComplexNumber(complex.Real, complex.Imaginary));
		}
		
		public void Visit(Ast.DictNode dict)
		{
			IDictionary obj = new Dictionary<object, object>(dict.Elements.Count);
			foreach(var node in dict.Elements)
			{
				var kv = (Ast.KeyValueNode) node;
				kv.Key.Accept(this);
				object key = _generated.Pop();
				kv.Value.Accept(this);
				object value = _generated.Pop();
				obj[key] = value;
			}

			if(_dictToInstance==null || !obj.Contains("__class__"))
			{
				_generated.Push(obj);
			}
			else
			{
				object result = _dictToInstance(obj);
				_generated.Push(result ?? obj);
			}
		}
		
		public void Visit(Ast.ListNode list)
		{
			IList<object> obj = new List<object>(list.Elements.Count);
			foreach(Ast.INode node in list.Elements)
			{
				node.Accept(this);
				obj.Add(_generated.Pop());
			}
			_generated.Push(obj);
		}
		
		public void Visit(Ast.NoneNode none)
		{
			_generated.Push(null);
		}
		
		public void Visit(Ast.IntegerNode value)
		{
			_generated.Push(value.Value);
		}
		
		public void Visit(Ast.LongNode value)
		{
			_generated.Push(value.Value);
		}
		
		public void Visit(Ast.DoubleNode value)
		{
			_generated.Push(value.Value);
		}
		
		public void Visit(Ast.BooleanNode value)
		{
			_generated.Push(value.Value);
		}
		
		public void Visit(Ast.StringNode value)
		{
			_generated.Push(value.Value);
		}
		
		public void Visit(Ast.BytesNode value)
		{
			_generated.Push(value.Value);
		}
		
		public void Visit(Ast.DecimalNode value)
		{
			_generated.Push(value.Value);
		}
		
		public void Visit(Ast.SetNode setnode)
		{
			var obj = new HashSet<object>();
			foreach(Ast.INode node in setnode.Elements)
			{
				node.Accept(this);
				obj.Add(_generated.Pop());
			}
			_generated.Push(obj);
		}
		
		public void Visit(Ast.TupleNode tuple)
		{
			var array = new object[tuple.Elements.Count];
			int index=0;
			foreach(Ast.INode node in tuple.Elements)
			{
				node.Accept(this);
				array[index++] = _generated.Pop();
			}
			_generated.Push(array);
		}
	}
}
