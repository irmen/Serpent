using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
		
		public struct ComplexNumberNode: INode
		{
			public double Real;
			public double Imaginary;
		}
		
		public class NoneNode: INode
		{
			public static NoneNode Instance = new NoneNode();
			private NoneNode()
			{
			}
		}
		
		public abstract class SequenceNode: INode
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
			
			public override bool Equals(object obj)
			{
				Ast.SequenceNode other = obj as Ast.SequenceNode;
				if (other == null)
					return false;
				return Enumerable.SequenceEqual<INode>(Elements, other.Elements);
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
			try {
				ast.Root = ParseExpr(sr);
				if(sr.HasMore())
					throw new ParseException("garbage at end of expression");
				return ast;
			} catch (ParseException x) {
				throw new ParseException(x.Message + " (at position "+sr.Bookmark()+")", x);
			}
		}
		
		protected Ast.INode ParseExpr(SeekableStringReader sr)
		{
			// expr =  [ <whitespace> ] single | compound [ <whitespace> ] .
			sr.SkipWhitespace();
			char c = sr.Peek();
			Ast.INode node;
			if(c=='{' || c=='[' || c=='(')
				node = ParseCompound(sr);
			else
				node = ParseSingle(sr);
			sr.SkipWhitespace();
			return node;
		}
		
		Ast.SequenceNode ParseCompound(SeekableStringReader sr)
		{
			// compound =  tuple | dict | list | set .
			switch(sr.Peek())
			{
				case '[':
					return ParseList(sr);
				case '{':
					return ParseSetOrDict(sr);
				case '(':
					return ParseTuple(sr);
				default:
					throw new ParseException("invalid sequencetype char");
			}
		}
		
		Ast.TupleNode ParseTuple(SeekableStringReader sr)
		{
			//tuple           = tuple_empty | tuple_one | tuple_more
			//tuple_empty     = '()' .
			//tuple_one       = '(' expr ',)' .
			//tuple_more      = '(' expr_list ')' .
			
			sr.Read();	// (
			Ast.TupleNode tuple = new Ast.TupleNode();
			if(sr.Peek() == ')')
			{
				sr.Read();
				return tuple;		// empty tuple
			}
			
			Ast.INode firstelement = ParseExpr(sr);
			if(sr.Peek() == ',')
			{
				sr.Read();
				if(sr.Read() == ')')
				{
					// tuple with just a single element
					tuple.Elements.Add(firstelement);
					return tuple;
				}
				sr.Rewind(1);   // undo the thing that wasn't a )
			}
			
			tuple.Elements = ParseExprList(sr);
			tuple.Elements.Insert(0, firstelement);
			if(!sr.HasMore())
				throw new ParseException("missing ')'");
			char closechar = sr.Read();
			if(closechar==',')
				closechar = sr.Read();
			if(closechar!=')')
				throw new ParseException("expected ')'");
			return tuple;			
		}
		
		protected List<Ast.INode> ParseExprList(SeekableStringReader sr)
		{
			//expr_list       = expr { ',' expr } .
			List<Ast.INode> exprList = new List<Ast.INode>();
			exprList.Add(ParseExpr(sr));
			while(sr.HasMore() && sr.Peek() == ',')
			{
				sr.Read();
				exprList.Add(ParseExpr(sr));
			}
			return exprList;
		}
		
		Ast.SequenceNode ParseSetOrDict(SeekableStringReader sr)
		{
			return ParseSet(sr);
		}
		
		Ast.SetNode ParseSet(SeekableStringReader sr)
		{
			// set = '{' expr_list '}' .
			sr.Read();	// {
			Ast.SetNode setnode = new Ast.SetNode();
			setnode.Elements = ParseExprList(sr);
			if(!sr.HasMore())
				throw new ParseException("missing '}'");
			char closechar = sr.Read();
			if(closechar!='}')
				throw new ParseException("expected '}'");
			return setnode;
		}
		
		Ast.ListNode ParseList(SeekableStringReader sr)
		{
			// list            = list_empty | list_nonempty .
			// list_empty      = '[]' .
			// list_nonempty   = '[' expr_list ']' .
			sr.Read();	// [
			Ast.ListNode list = new Ast.ListNode();
			if(sr.Peek() == ']')
			{
				sr.Read();
				return list;		// empty list
			}
			
			list.Elements = ParseExprList(sr);
			if(!sr.HasMore())
				throw new ParseException("missing ']'");
			char closechar = sr.Read();
			if(closechar!=']')
				throw new ParseException("expected ']'");
			return list;
		}
		
		protected Ast.INode ParseSingle(SeekableStringReader sr)
		{
			// single =  int | float | complex | string | bool | none .
			switch(sr.Peek())
			{
				case 'N':
					return ParseNone(sr);
				case 'T':
				case 'F':
					return ParseBool(sr);
				case '\'':
				case '"':
					return ParseString(sr);
			}
			// @todo int or float or complex.
			int bookmark = sr.Bookmark();
			try {
				return ParseComplex(sr);
			} catch (ParseException) {
				sr.FlipBack(bookmark);
				try {
					return ParseFloat(sr);
				} catch (ParseException) {
					sr.FlipBack(bookmark);
					return ParseInt(sr);
				}
			}
		}
		
		Ast.PrimitiveNode<int> ParseInt(SeekableStringReader sr)
		{
			// int =  ['-'] digitnonzero {digit} .
			string numberstr = sr.ReadWhile('-','0','1','2','3','4','5','6','7','8','9');
			if(numberstr.Length==0)
				throw new ParseException("invalid int character");
			return new Ast.PrimitiveNode<int>(int.Parse(numberstr));
		}

		Ast.PrimitiveNode<double> ParseFloat(SeekableStringReader sr)
		{
			string numberstr = sr.ReadWhile('-','+','.','e','E','0','1','2','3','4','5','6','7','8','9');
			if(numberstr.Length==0)
				throw new ParseException("invalid float character");
			
			// little bit of a hack:
			// if the number doesn't contain a decimal point and no 'e'/'E', it is an integer instead.
			// in that case, we need to reject it as a float.
			if(numberstr.IndexOfAny(new char[] {'.','e','E'}) < 0)
				throw new ParseException("number is not a valid float");

			return new Ast.PrimitiveNode<double>(double.Parse(numberstr, CultureInfo.InvariantCulture));
		}

		Ast.ComplexNumberNode ParseComplex(SeekableStringReader sr)
		{
			//complex         = complextuple | imaginary .
			//imaginary       = ['+' | '-' ] ( float | int ) 'j' .
			//complextuple    = '(' ( float | int ) imaginary ')' .
			if(sr.Peek()=='(')
			{
				// complextuple
				sr.Read();
				string numberstr = sr.ReadUntil(new char[] {'+', '-'});
				double realpart = double.Parse(numberstr, CultureInfo.InvariantCulture);
				double imaginarypart = ParseImaginaryPart(sr);
				if(sr.Peek()!=')')
					throw new ParseException("expected ) to end a complex number");
				return new Ast.ComplexNumberNode()
					{
						Real = realpart,
						Imaginary = imaginarypart
					};
			}
			else
			{
				// imaginary
				double imag = ParseImaginaryPart(sr);
				return new Ast.ComplexNumberNode()
					{
						Real=0,
						Imaginary=imag
					};
			}
		}
		
		double ParseImaginaryPart(SeekableStringReader sr)
		{
			//imaginary       = ['+' | '-' ] ( float | int ) 'j' .
			char signchr = sr.Peek();
			if(signchr!='+' && signchr!='-')
				throw new ParseException("expected +/- at start of imaginary part");
			
			string numberstr = sr.ReadUntil('j');
			return double.Parse(numberstr, CultureInfo.CurrentCulture);
		}
		
		Ast.PrimitiveNode<string> ParseString(SeekableStringReader sr)
		{
			char quotechar = sr.Read();   // ' or "
			StringBuilder sb = new StringBuilder(10);
			while(sr.HasMore())
			{
				char c = sr.Read();
				if(c=='\\')
				{
					// backslash unescape
					c = sr.Read();
					switch(c)
					{
						case '\\':
							sb.Append('\\');
							break;
						case '\'':
							sb.Append('\'');
							break;
						case '"':
							sb.Append('"');
							break;
						case 'a':
							sb.Append('\a');
							break;
						case 'b':
							sb.Append('\b');
							break;
						case 'f':
							sb.Append('\f');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 't':
							sb.Append('\t');
							break;
						case 'v':
							sb.Append('\v');
							break;
						default:
							sb.Append(c);
							break;
					}
				}
				else if(c==quotechar)
				{
					// end of string
					return new Ast.PrimitiveNode<string>(sb.ToString());
				}
				else
				{
					sb.Append(c);
				}
			}
			throw new ParseException("unclosed string");
		}
		
		Ast.PrimitiveNode<bool> ParseBool(SeekableStringReader sr)
		{
			// True,False
			string b = sr.ReadUntil('e');
			if(b=="Tru")
				return new Ast.PrimitiveNode<bool>(true);
			if(b=="Fals")
				return new Ast.PrimitiveNode<bool>(false);
			throw new ParseException("expected bool, True or False");
		}
		
		Ast.NoneNode ParseNone(SeekableStringReader sr)
		{
			// None
			string n = sr.ReadUntil('e');
			if(n=="Non")
				return Ast.NoneNode.Instance;
			throw new ParseException("expected None");
		}
	}
}

