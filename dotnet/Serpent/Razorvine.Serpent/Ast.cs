using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Linq;
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedParameter.Global

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
			void Visit(ComplexNumberNode complex);
			void Visit(DictNode dict);
			void Visit(ListNode list);
			void Visit(NoneNode none);
			void Visit(IntegerNode value);
			void Visit(LongNode value);
			void Visit(DoubleNode value);
			void Visit(BooleanNode value);
			void Visit(StringNode value);
			void Visit(BytesNode value);
			void Visit(DecimalNode value);
			void Visit(SetNode setnode);
			void Visit(TupleNode tuple);
		}

		/// <summary>
		/// Visitor pattern: visit all nodes in the Ast with the given visitor.
		/// </summary>
		public void Accept(INodeVisitor visitor)
		{
			Root.Accept(visitor);
		}

		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public interface INode
		{
			string ToString();
			bool Equals(object obj);
			void Accept(INodeVisitor visitor);
		}
		
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public abstract class PrimitiveNode<T> : INode, IComparable<PrimitiveNode<T>>
		{
			public readonly T Value;
			protected PrimitiveNode(T value)
			{
				Value=value;
			}
			
			public override int GetHashCode()
			{
				return Value!=null? Value.GetHashCode() : 0;
			}
			
			public override bool Equals(object obj)
			{
				var node = obj as PrimitiveNode<T>;
				return node != null && Equals(Value, node.Value);
			}

			public bool Equals(PrimitiveNode<T> other)
			{
				return Equals(Value, other.Value);
			}
			
			public int CompareTo(PrimitiveNode<T> other)
			{
				var cv = Value as IComparable;
				var otherCv = other.Value as IComparable;
				if (cv != null && otherCv != null)
					return cv.CompareTo(otherCv);
				return 0;
			}
			
			public override string ToString()
			{
				var s = Value as string;
				if(s != null)
				{
					var sb=new StringBuilder();
					sb.Append("'");
					foreach(var c in s)
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

				if (!(Value is double) && !(Value is float)) 
					return Value.ToString();
				
				var d = Convert.ToString(Value, CultureInfo.InvariantCulture);
				if (d == null)
					throw new ParseException("ast value is null");

				if(d.IndexOfAny(new [] {'.', 'e', 'E'})<=0)
					d+=".0";
				return d;
			}
			
			public abstract void Accept(INodeVisitor visitor);
		}
		
		public class IntegerNode: PrimitiveNode<int>
		{
			public IntegerNode(int value) : base(value)
			{
			}
			public override void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public class LongNode: PrimitiveNode<long>
		{
			public LongNode(long value) : base(value)
			{
			}
			public override void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class DoubleNode: PrimitiveNode<double>
		{
			public DoubleNode(double value) : base(value)
			{
			}
			public override void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public class StringNode: PrimitiveNode<string>
		{
			public StringNode(string value) : base(value)
			{
			}
			public override void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public class BytesNode: PrimitiveNode<byte[]>
		{
			public BytesNode(byte[] value) : base(value)
			{
			}
			public override void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}		
		
		public class DecimalNode: PrimitiveNode<decimal>
		{
			public DecimalNode(decimal value) : base(value)
			{
			}
			public override void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public class BooleanNode: PrimitiveNode<bool>
		{
			public BooleanNode(bool value) : base(value)
			{
			}
			public override void Accept(INodeVisitor visitor)
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
				return string.Format(Imaginary>=0 ? "({0}+{1}j)" : "({0}{1}j)", strReal, strImag);
			}
		}
		
		public class NoneNode: INode
		{
			public static readonly NoneNode Instance = new NoneNode();
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
			public virtual char OpenChar => '?';
			public virtual char CloseChar => '?';

			public override int GetHashCode()
			{
				int hashCode = 0;
				unchecked {
					// ReSharper disable once NonReadonlyMemberInGetHashCode
					foreach(var elt in Elements)
						hashCode += 1000000007 * elt.GetHashCode();
				}
				return hashCode;
			}

			public override bool Equals(object obj)
			{
				var other = obj as SequenceNode;
				return other != null && Elements.SequenceEqual(other.Elements);
			}

			public override string ToString()
			{
				var sb=new StringBuilder();
				sb.Append(OpenChar);
				if(Elements != null)
				{
					foreach(var elt in Elements)
					{
						sb.Append(elt.ToString());
						sb.Append(',');
					}
				}
				// ReSharper disable once PossibleNullReferenceException
				if(Elements.Count>0)
					sb.Remove(sb.Length-1, 1);	// remove last comma
				sb.Append(CloseChar);
				return sb.ToString();
			}
			
			public abstract void Accept(INodeVisitor visitor);
		}
		
		public class TupleNode : SequenceNode
		{
			public override string ToString()
			{
				var sb=new StringBuilder();
				sb.Append('(');
				if(Elements != null)
				{
					foreach(var elt in Elements)
					{
						sb.Append(elt.ToString());
						sb.Append(",");
					}
					if(Elements.Count>1)
						sb.Remove(sb.Length-1, 1);
				}
				sb.Append(')');
				return sb.ToString();
			}
			
			public override void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class ListNode : SequenceNode
		{
			public override char OpenChar => '[';
			public override char CloseChar => ']';

			public override void Accept(INodeVisitor visitor)
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
				var set2 = ((UnorderedSequenceNode) obj).ElementsAsSet();
				return set1.SetEquals(set2);
			}
			
			public override int GetHashCode()
			{
				return ElementsAsSet().GetHashCode();
			}
			
			public HashSet<INode> ElementsAsSet()
			{
				var set = new HashSet<INode>();
				foreach(var kv in Elements)
					set.Add(kv);
				return set;
			}
		}
		
		public class SetNode : UnorderedSequenceNode
		{
			public override char OpenChar => '{';
			public override char CloseChar => '}';

			public override void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public class DictNode : UnorderedSequenceNode
		{
			public override char OpenChar => '{';
			public override char CloseChar => '}';

			public override void Accept(INodeVisitor visitor)
			{
				visitor.Visit(this);
			}
		}
		
		public struct KeyValueNode : INode
		{
			public INode Key;
			public INode Value;
			
			public KeyValueNode(INode key, INode value)
			{
				Key = key;
				Value = value;
			}
			
			public override string ToString()
			{
				return $"{Key}:{Value}";
			}
			
			public void Accept(INodeVisitor visitor)
			{
				throw new NotSupportedException("don't visit a keyvaluenode");
			}
		}
	}
}
