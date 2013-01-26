/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright 2013, Irmen de Jong (irmen@razorvine.net)
/// This code is open-source, but licensed under the "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;
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
		
		public override bool Equals(object obj)
		{
			Ast other = obj as Ast;
			if (other == null)
				return false;
			return this.Root.Equals(other.Root);
		}
		
		public override int GetHashCode()
		{
			return this.Root.GetHashCode();
		}

		public interface INode
		{
			string ToString();
			bool Equals(object obj);
		}
		
		public struct PrimitiveNode<T> : INode, IComparable<PrimitiveNode<T>> where T: IComparable
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
		}
		
		
		
		public struct ComplexNumberNode: INode
		{
			public double Real;
			public double Imaginary;
			
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
		}
		
		public abstract class SequenceNode: INode
		{
			public List<INode> Elements = new List<INode>();
			public virtual char OpenChar {get { return '?'; }}
			public virtual char CloseChar {get { return '?'; }}

			public override int GetHashCode()
			{
				int hashCode = Elements.GetHashCode();
				unchecked {
					foreach(var elt in Elements)
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
		}

		public class ListNode : SequenceNode
		{
			public override char OpenChar { get { return '['; } }
			public override char CloseChar { get { return ']'; } }
		}
		
		public class SetNode : SequenceNode
		{
			public override char OpenChar { get { return '{'; } }
			public override char CloseChar { get { return '}'; } }
		}
		
		public class DictNode : SequenceNode
		{
			public override char OpenChar { get { return '{'; } }
			public override char CloseChar { get { return '}'; } }
		}
		
		public struct KeyValueNode : INode
		{
			public INode Key;
			public INode Value;
			
			public override string ToString()
			{
				return string.Format("{0}:{1}", Key, Value);
			}
		}
			
	}
}
