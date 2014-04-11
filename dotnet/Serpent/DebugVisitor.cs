/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright 2014, Irmen de Jong (irmen@razorvine.net)
/// Software license: "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;
using System.Collections.Generic;
using System.Text;

namespace Razorvine.Serpent
{
	/// <summary>
	/// Ast nodevisitor that prints out the Ast as a string for debugging purposes
	/// </summary>
	public class DebugVisitor: Ast.INodeVisitor
	{
		private StringBuilder result = new StringBuilder();
		private int indent=0;
		
		public DebugVisitor()
		{
		}
		
		/// <summary>
		/// Get the debug string representation result.
		/// </summary>
		public override string ToString()
		{
			return result.ToString();
		}
		
		protected void Indent()
		{
			for(int i=0; i<indent; ++i)
				result.Append("    ");
		}
		
		public void Visit(Ast.ComplexNumberNode complex)
		{
			result.AppendFormat("complexnumber ({0}r,{1}i)", complex.Real, complex.Imaginary);
		}
		
		public void Visit(Ast.DictNode dict)
		{
			result.AppendLine("(dict");
			indent++;
			foreach(Ast.KeyValueNode kv in dict.Elements)
			{
				Indent();
				kv.Key.Accept(this);
				result.Append(" = ");
				kv.Value.Accept(this);
				result.AppendLine(",");
			}
			indent--;
			Indent();
			result.Append(")");
		}
		
		public void Visit(Ast.ListNode list)
		{
			result.AppendLine("(list");
			indent++;
			foreach(Ast.INode node in list.Elements)
			{
				Indent();
				node.Accept(this);
				result.AppendLine(",");
			}
			indent--;
			Indent();
			result.Append(")");
		}
		
		public void Visit(Ast.NoneNode none)
		{
			result.Append("None");
		}
		
		public void Visit(Ast.IntegerNode value)
		{
			result.AppendFormat("int {0}", value.Value);
		}
		
		public void Visit(Ast.LongNode value)
		{
			result.AppendFormat("long {0}", value.Value);
		}
		
		public void Visit(Ast.DoubleNode value)
		{
			result.AppendFormat("double {0}", value.Value);
		}
		
		public void Visit(Ast.BooleanNode value)
		{
			result.AppendFormat("bool {0}", value.Value);
		}
		
		public void Visit(Ast.StringNode value)
		{
			result.AppendFormat("string '{0}'", value.Value);
		}
		
		public void Visit(Ast.DecimalNode value)
		{
			result.AppendFormat("decimal {0}", value.Value);
		}
		
		public void Visit(Ast.SetNode setnode)
		{
			result.AppendLine("(set");
			indent++;
			foreach(Ast.INode node in setnode.Elements)
			{
				Indent();
				node.Accept(this);
				result.AppendLine(",");
			}
			indent--;
			Indent();
			result.Append(")");
		}
		
		public void Visit(Ast.TupleNode tuple)
		{
			result.AppendLine("(tuple");
			indent++;
			foreach(Ast.INode node in tuple.Elements)
			{
				Indent();
				node.Accept(this);
				result.AppendLine(",");
			}
			indent--;
			Indent();
			result.Append(")");
		}
	}
}
