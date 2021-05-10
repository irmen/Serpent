using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Razorvine.Serpent
{
	/// <summary>
	/// Ast nodevisitor that prints out the Ast as a string for debugging purposes
	/// </summary>
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public class DebugVisitor: Ast.INodeVisitor
	{
		private readonly StringBuilder _result = new StringBuilder();
		private int _indent;
		
		/// <summary>
		/// Get the debug string representation result.
		/// </summary>
		public override string ToString()
		{
			return _result.ToString();
		}
		
		protected void Indent()
		{
			for(int i=0; i<_indent; ++i)
				_result.Append("    ");
		}
		
		public void Visit(Ast.ComplexNumberNode complex)
		{
			_result.AppendFormat("complexnumber ({0}r,{1}i)", complex.Real, complex.Imaginary);
		}
		
		public void Visit(Ast.DictNode dict)
		{
			_result.AppendLine("(dict");
			_indent++;
			foreach(var node in dict.Elements)
			{
				var kv = (Ast.KeyValueNode) node;
				Indent();
				kv.Key.Accept(this);
				_result.Append(" = ");
				kv.Value.Accept(this);
				_result.AppendLine(",");
			}
			_indent--;
			Indent();
			_result.Append(")");
		}
		
		public void Visit(Ast.ListNode list)
		{
			_result.AppendLine("(list");
			_indent++;
			foreach(Ast.INode node in list.Elements)
			{
				Indent();
				node.Accept(this);
				_result.AppendLine(",");
			}
			_indent--;
			Indent();
			_result.Append(")");
		}
		
		public void Visit(Ast.NoneNode none)
		{
			_result.Append("None");
		}
		
		public void Visit(Ast.IntegerNode value)
		{
			_result.AppendFormat("int {0}", value.Value);
		}
		
		public void Visit(Ast.LongNode value)
		{
			_result.AppendFormat("long {0}", value.Value);
		}
		
		public void Visit(Ast.DoubleNode value)
		{
			_result.AppendFormat("double {0}", value.Value);
		}
		
		public void Visit(Ast.BooleanNode value)
		{
			_result.AppendFormat("bool {0}", value.Value);
		}
		
		public void Visit(Ast.StringNode value)
		{
			_result.AppendFormat("string '{0}'", value.Value);
		}
		
		public void Visit(Ast.BytesNode value)
		{
			_result.AppendFormat("bytes {0}", value.Value);
		}		
		
		public void Visit(Ast.DecimalNode value)
		{
			_result.AppendFormat("decimal {0}", value.Value);
		}
		
		public void Visit(Ast.SetNode setnode)
		{
			_result.AppendLine("(set");
			_indent++;
			foreach(Ast.INode node in setnode.Elements)
			{
				Indent();
				node.Accept(this);
				_result.AppendLine(",");
			}
			_indent--;
			Indent();
			_result.Append(")");
		}
		
		public void Visit(Ast.TupleNode tuple)
		{
			_result.AppendLine("(tuple");
			_indent++;
			foreach(Ast.INode node in tuple.Elements)
			{
				Indent();
				node.Accept(this);
				_result.AppendLine(",");
			}
			_indent--;
			Indent();
			_result.Append(")");
		}
	}
}
