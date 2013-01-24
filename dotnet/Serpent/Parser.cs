using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Razorvine.Serpent
{
	public class Ast
	{
		public INode Root;
			
		public interface INode
		{
		}
		
		public struct PrimitiveNode<T> : INode, IComparable<PrimitiveNode<T>> where T: IComparable
		{
			public T Value;
			public PrimitiveNode(T value)
			{
				this.Value=value;
			}
			
			#region Equals and GetHashCode implementation
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
			
			public static bool operator ==(Ast.PrimitiveNode<T> lhs, Ast.PrimitiveNode<T> rhs)
			{
				return lhs.Equals(rhs);
			}
			
			public static bool operator !=(Ast.PrimitiveNode<T> lhs, Ast.PrimitiveNode<T> rhs)
			{
				return !(lhs == rhs);
			}
			#endregion

			public int CompareTo(PrimitiveNode<T> other)
			{
				return Value.CompareTo(other.Value);
			}
			
			public override string ToString()
			{
				return string.Format("[PrimitiveNode Type={0} Value={1}]", typeof(T), Value);
			}

		}
		
		public abstract class SequenceNode
		{
			public List<INode> Elements = new List<INode>();

			public override int GetHashCode()
			{
				int hashCode = Elements.GetHashCode();
				unchecked {
					foreach(var elt in Elements)
						hashCode += 1000000007 * elt.GetHashCode();
				}
				return hashCode;
			}
		}
		
		public class TupleNode : SequenceNode
		{
		}

		public class ListNode : SequenceNode
		{
		}
		
		public class SetNode : SequenceNode
		{
		}
		
		public class DictNode : INode
		{
			public IList<KeyValuePair<INode, INode>> Elements = new List<KeyValuePair<INode, INode>>();
			
			public override bool Equals(object obj)
			{
				Ast.DictNode other = obj as Ast.DictNode;
				if (other == null)
					return false;
				return this.Elements.Equals(other.Elements);
			}
			
			public override int GetHashCode()
			{
				int hashCode = Elements.GetHashCode();
				unchecked {
					foreach(var elt in Elements)
						hashCode += 1000000007 * elt.GetHashCode();
				}
				return hashCode;
			}			
		}
	}

	public class Parser
	{
		public Ast Parse(byte[] serialized)
		{
			return Parse(Encoding.UTF8.GetString(serialized));
		}
		
		public Ast Parse(string expression)
		{
			Ast ast=new Ast();
			if(string.IsNullOrEmpty(expression))
				return ast;
			
			SeekableStringReader sr = new SeekableStringReader(expression);
			ast.Root = Parse(sr);
			return ast;
		}
		
		protected Ast.INode Parse(SeekableStringReader sr)
		{
			switch(sr.Peek())
			{
				case '\'':
					sr.Read();
					return new Ast.PrimitiveNode<string>(sr.ReadUntil('\''));
				case '"':
					sr.Read();
					return new Ast.PrimitiveNode<string>(sr.ReadUntil('"'));
				default:
					return new Ast.PrimitiveNode<int>(int.Parse(sr.Read(999)));
			}
		}
	}
}

