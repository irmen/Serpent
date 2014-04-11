/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright 2014, Irmen de Jong (irmen@razorvine.net)
/// Software license: "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;

namespace Razorvine.Serpent
{
	/// <summary>
	/// Abstract syntax tree for the literal expression. This is what the parser returns.
	/// </summary>
	public class Ast
	{
		public INode Root;
		
		public override string ToString()
		{
			return "# serpent utf-8 .net\n" + Root.ToString();
		}
		
		/// <summary>
		/// Get the actual parsed data as C# object(s).
		/// </summary>
		public object GetData()
		{
			var visitor = new ObjectifyVisitor();
			Root.Accept(visitor);
			return visitor.GetObject();
		}
		
		/// <summary>
		/// Get the actual parsed data as C# object(s).
		/// </summary>
		/// <param name="dictToInstance">functin to convert dicts to actual instances for a class,
		/// instead of leaving them as dictionaries. Requires the __class__ key to be present
		/// in the dict node. If it returns null, the normal processing is done.</param>
		public object GetData(Func<IDictionary, object> dictToInstance)
		{
			var visitor = new ObjectifyVisitor(dictToInstance);
			Root.Accept(visitor);
			return visitor.GetObject();
		}

		public interface INodeVisitor
		{
			void Visit(Ast.ComplexNumberNode complex);
			void Visit(Ast.DictNode dict);
			void Visit(Ast.ListNode list);
			void Visit(Ast.NoneNode none);
			void Visit(Ast.IntegerNode value);
			void Visit(Ast.LongNode value);
			void Visit(Ast.DoubleNode value);
			void Visit(Ast.BooleanNode value);
			void Visit(Ast.StringNode value);
			void Visit(Ast.DecimalNode value);
			void Visit(Ast.SetNode setnode);
			void Visit(Ast.TupleNode tuple);
		}

		/// <summary>
		/// Visitor pattern: visit all nodes in the Ast with the given visitor.
		/// </summary>
		public void Accept(INodeVisitor visitor)
		{
			Root.Accept(visitor);
		}

		public interface INode
		{
			string ToString();
			bool Equals(object obj);
			void Accept(INodeVisitor visitor);
		}
		
		public abstract class PrimitiveNode<T> : INode, IComparable<PrimitiveNode<T>> where T: IComparable
		{
			public T Value;
			public PrimitiveNode(T value)
			{
				this.Value=value;
			}
			
			public override int GetHashCode()
			{
				return Value!=null? Value.GetHashCode() : 0;
			}
			
			public override bool Equals(object obj)
			{
				return (obj is Ast.PrimitiveNode<T>) &&
					Equals(Value, ((Ast.PrimitiveNode<T>)obj).Value);
			}

			public bool Equals(Ast.PrimitiveNode<T> other)
			{
				return object.Equals(this.Value, other.Value);
			}
			
			public int CompareTo(PrimitiveNode<T> other)
			{
				return Value.CompareTo(other.Value);
			}
			
			public override string ToString()
			{
				if(Value is string)
				{
					StringBuilder sb=new StringBuilder();
					sb.Append("'");
					foreach(char c in (Value as string))
					{
						switch(c)
						{
							case '\\':
								sb.Append("\\\\");
								break;
							case '\'':
								sb.Append("\\'");
								break;
							case '\a':
								sb.Append("\\a");
								break;
							case '\b':
								sb.Append("\\b");
								break;
							case '\f':
								sb.Append("\\f");
								break;
							case '\n':
								sb.Append("\\n");
								break;
							case '\r':
								sb.Append("\\r");
								break;
							case '\t':
								sb.Append("\\t");
								break;
							case '\v':
								sb.Append("\\v");
								break;
							default:
								sb.Append(c);
								break;
						}
					}
					sb.Append("'");
					return sb.ToString();
				}
				else if(Value is double || Value is float)
				{
					string d = Convert.ToString(Value, CultureInfo.InvariantCulture);
					if(d.IndexOfAny(new char[] {'.', 'e', 'E'})<=0)
						d+=".0";
					return d;
				}
				else return Value.ToString();
			}
			
			public abstract void Accept(Ast.INodeVisitor visitor);
		}
		
		public class IntegerNode: PrimitiveNode<int>
		{
			public IntegerNode(int value) : base(value)
			{
			}
			public override void Accept(Ast.INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public class LongNode: PrimitiveNode<long>
		{
			public LongNode(long value) : base(value)
			{
			}
			public override void Accept(Ast.INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class DoubleNode: PrimitiveNode<double>
		{
			public DoubleNode(double value) : base(value)
			{
			}
			public override void Accept(Ast.INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public class StringNode: PrimitiveNode<string>
		{
			public StringNode(string value) : base(value)
			{
			}
			public override void Accept(Ast.INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public class DecimalNode: PrimitiveNode<decimal>
		{
			public DecimalNode(decimal value) : base(value)
			{
			}
			public override void Accept(Ast.INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public class BooleanNode: PrimitiveNode<bool>
		{
			public BooleanNode(bool value) : base(value)
			{
			}
			public override void Accept(Ast.INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public struct ComplexNumberNode: INode
		{
			public double Real;
			public double Imaginary;
			
			public void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}

			public override string ToString()
			{
				string strReal = Real.ToString(CultureInfo.InvariantCulture);
				string strImag = Imaginary.ToString(CultureInfo.InvariantCulture);
				if(Imaginary>=0)
					return string.Format("({0}+{1}j)", strReal, strImag);
				return string.Format("({0}{1}j)", strReal, strImag);
			}
		}
		
		public class NoneNode: INode
		{
			public static NoneNode Instance = new NoneNode();
			private NoneNode()
			{
			}
			
			public override string ToString()
			{
				return "None";
			}

			public void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}			
		}
		
		public abstract class SequenceNode: INode
		{
			public List<INode> Elements = new List<INode>();
			public virtual char OpenChar {get { return '?'; }}
			public virtual char CloseChar {get { return '?'; }}

			public override int GetHashCode()
			{
				int hashCode = 0;
				unchecked {
					foreach(INode elt in Elements)
						hashCode += 1000000007 * elt.GetHashCode();
				}
				return hashCode;
			}

			public override bool Equals(object obj)
			{
				Ast.SequenceNode other = obj as Ast.SequenceNode;
				if (other == null)
					return false;
				return Enumerable.SequenceEqual(Elements, other.Elements);
			}

			public override string ToString()
			{
				StringBuilder sb=new StringBuilder();
				sb.Append(OpenChar);
				if(Elements != null)
				{
					foreach(var elt in Elements)
					{
						sb.Append(elt.ToString());
						sb.Append(',');
					}
				}
				if(Elements.Count>0)
					sb.Remove(sb.Length-1, 1);	// remove last comma
				sb.Append(CloseChar);
				return sb.ToString();
			}
			
			public abstract void Accept(Ast.INodeVisitor visitor);
		}
		
		public class TupleNode : SequenceNode
		{
			public override string ToString()
			{
				StringBuilder sb=new StringBuilder();
				sb.Append('(');
				if(Elements != null)
				{
					foreach(var elt in Elements)
					{
						sb.Append(elt.ToString());
						sb.Append(",");
					}
				}
				if(Elements.Count>1)
					sb.Remove(sb.Length-1, 1);
				sb.Append(')');
				return sb.ToString();
			}
			
			public override void Accept(Ast.INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class ListNode : SequenceNode
		{
			public override char OpenChar { get { return '['; } }
			public override char CloseChar { get { return ']'; } }
			public override void Accept(Ast.INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public abstract class UnorderedSequenceNode : SequenceNode
		{
			public override bool Equals(object obj)
			{
				if(!(obj is UnorderedSequenceNode))
					return false;
				var set1 = ElementsAsSet();
				var set2 = (obj as UnorderedSequenceNode).ElementsAsSet();
				return set1.SetEquals(set2);
			}
			
			public override int GetHashCode()
			{
				return ElementsAsSet().GetHashCode();
			}
			
			private HashSet<INode> ElementsAsSet()
			{
				var set = new HashSet<INode>();
				foreach(INode kv in Elements)
					set.Add(kv);
				return set;
			}
		}
		
		public class SetNode : UnorderedSequenceNode
		{
			public override char OpenChar { get { return '{'; } }
			public override char CloseChar { get { return '}'; } }
			public override void Accept(Ast.INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public class DictNode : UnorderedSequenceNode
		{
			public override char OpenChar { get { return '{'; } }
			public override char CloseChar { get { return '}'; } }
			public override void Accept(Ast.INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public struct KeyValueNode : INode
		{
			public INode Key;
			public INode Value;
			
			public override string ToString()
			{
				return string.Format("{0}:{1}", Key, Value);
			}
			
			public void Accept(INodeVisitor visitor)
			{
				throw new NotSupportedException("don't visit a keyvaluenode");
			}
		}
	}
}
