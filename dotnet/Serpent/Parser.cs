/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright Irmen de Jong (irmen@razorvine.net)
/// Software license: "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;
#if !(SILVERLIGHT || WINDOWS_PHONE || PORTABLE)
using System.Collections;
#endif
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Razorvine.Serpent
{
	/// <summary>
	/// Parse a Python literal into an Ast (abstract syntax tree).
	/// </summary>
	public class Parser
	{
		
		/// <summary>
		/// Parse from a byte array (containing utf-8 encoded string with the Python literal expression in it)
		/// </summary>
		public Ast Parse(byte[] serialized)
		{
			return Parse(Encoding.UTF8.GetString(serialized, 0, serialized.Length));
		}
		
		/// <summary>
		/// Parse from a string with the Python literal expression
		/// </summary>
		public Ast Parse(string expression)
		{
			Ast ast=new Ast();
			if(string.IsNullOrEmpty(expression))
				return ast;
			
			SeekableStringReader sr = new SeekableStringReader(expression);
			if(sr.Peek()=='#')
				sr.ReadUntil('\n');  // skip comment line
			
			try {
				ast.Root = ParseExpr(sr);
				sr.SkipWhitespace();
				if(sr.HasMore())
					throw new ParseException("garbage at end of expression");
				return ast;
			} catch (ParseException x) {
				string faultLocation = ExtractFaultLocation(sr);
				throw new ParseException(x.Message + " (at position "+sr.Bookmark()+"; '"+faultLocation+"')", x);
			}
		}
		
		string ExtractFaultLocation(SeekableStringReader sr)
		{
			string left, right;
			sr.Context(-1, 20, out left, out right);
			return string.Format("...{0}>>><<<{1}...", left, right);
		}
		
		Ast.INode ParseExpr(SeekableStringReader sr)
		{
			// expr =  [ <whitespace> ] single | compound [ <whitespace> ] .
			sr.SkipWhitespace();
			if(!sr.HasMore())
				throw new ParseException("unexpected end of line, missing expression or close/open character");
			char c = sr.Peek();
			Ast.INode node;
			if(c=='{' || c=='[' || c=='(')
				node = ParseCompound(sr);
			else
				node = ParseSingle(sr);
			sr.SkipWhitespace();
			return node;
		}
		
		Ast.INode ParseCompound(SeekableStringReader sr)
		{
			// compound =  tuple | dict | list | set .
			sr.SkipWhitespace();
			switch(sr.Peek())
			{
				case '[':
					return ParseList(sr);
				case '{':
					{
						int bm = sr.Bookmark();
						try {
							return ParseSet(sr);
						} catch(ParseException) {
							sr.FlipBack(bm);
							return ParseDict(sr);
						}
					}
				case '(':
					// tricky case here, it can be a tuple but also a complex number.
					// try complex number first
					{
						int bm = sr.Bookmark();
						try {
							return ParseComplex(sr);
						} catch(ParseException) {
							sr.FlipBack(bm);
							return ParseTuple(sr);
						}
					}
				default:
					throw new ParseException("invalid sequencetype char");
			}
		}
		
		Ast.TupleNode ParseTuple(SeekableStringReader sr)
		{
			//tuple           = tuple_empty | tuple_one | tuple_more
			//tuple_empty     = '()' .
			//tuple_one       = '(' expr ',' <whitespace> ')' .
			//tuple_more      = '(' expr_list trailing_comma ')' .
			// trailing_comma  = '' | ',' .			

			sr.Read();	// (
			sr.SkipWhitespace();
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
				sr.SkipWhitespace();
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

			// handle trailing comma if present
			sr.SkipWhitespace();
			if(!sr.HasMore())
				throw new ParseException("missing ')'");
			if(sr.Peek() == ',')
				sr.Read();

			if(!sr.HasMore())
				throw new ParseException("missing ')'");
			char closechar = sr.Read();
			if(closechar==',')
				closechar = sr.Read();
			if(closechar!=')')
				throw new ParseException("expected ')'");
			return tuple;			
		}
		
		List<Ast.INode> ParseExprList(SeekableStringReader sr)
		{
			//expr_list       = expr { ',' expr } .
			List<Ast.INode> exprList = new List<Ast.INode>();
			exprList.Add(ParseExpr(sr));
			while(sr.HasMore() && sr.Peek() == ',')
			{
				sr.Read();
				try {
					exprList.Add(ParseExpr(sr));
				} catch (ParseException) {
					sr.Rewind(1);
					break;
				}
					
			}
			return exprList;
		}
		
		List<Ast.INode> ParseKeyValueList(SeekableStringReader sr)
		{
			//keyvalue_list   = keyvalue { ',' keyvalue } .
			List<Ast.INode> kvs = new List<Ast.INode>();
			kvs.Add(ParseKeyValue(sr));
			while(sr.HasMore() && sr.Peek()==',')
			{
				sr.Read();
				try {
					kvs.Add(ParseKeyValue(sr));
				} catch (ParseException) {
					sr.Rewind(1);
					break;
				}
			}
			return kvs;
		}		
		
		Ast.KeyValueNode ParseKeyValue(SeekableStringReader sr)
		{
			//keyvalue        = expr ':' expr .
			Ast.INode key = ParseExpr(sr);
			if(sr.HasMore() && sr.Peek()==':')
			{
				sr.Read(); // :
				Ast.INode value = ParseExpr(sr);
				return new Ast.KeyValueNode
					{
						Key = key,
						Value = value
					};
			}
			throw new ParseException("expected ':'");
		}
		
		Ast.SetNode ParseSet(SeekableStringReader sr)
		{
			// set = '{' expr_list trailing_comma '}' .
			// trailing_comma  = '' | ',' .			
			sr.Read();	// {
			sr.SkipWhitespace();
			Ast.SetNode setnode = new Ast.SetNode();
			List<Ast.INode> elts = ParseExprList(sr);

			// handle trailing comma if present
			sr.SkipWhitespace();
			if(!sr.HasMore())
				throw new ParseException("missing '}'");
			if(sr.Peek() == ',')
				sr.Read();

			if(!sr.HasMore())
				throw new ParseException("missing '}'");
			char closechar = sr.Read();
			if(closechar!='}')
				throw new ParseException("expected '}'");

			// make sure it has set semantics (remove duplicate elements)
			HashSet<Ast.INode> h = new HashSet<Ast.INode>(elts);
			setnode.Elements = new List<Ast.INode>(h);
			return setnode;
		}
		
		Ast.ListNode ParseList(SeekableStringReader sr)
		{
			// list            = list_empty | list_nonempty .
			// list_empty      = '[]' .
			// list_nonempty   = '[' expr_list trailing_comma ']' .
			// trailing_comma  = '' | ',' .
			sr.Read();	// [
			sr.SkipWhitespace();
			Ast.ListNode list = new Ast.ListNode();
			if(sr.Peek() == ']')
			{
				sr.Read();
				return list;		// empty list
			}
			
			list.Elements = ParseExprList(sr);

			// handle trailing comma if present
			sr.SkipWhitespace();
			if(!sr.HasMore())
				throw new ParseException("missing ']'");
			if(sr.Peek() == ',')
				sr.Read();

			if(!sr.HasMore())
				throw new ParseException("missing ']'");
			char closechar = sr.Read();
			if(closechar!=']')
				throw new ParseException("expected ']'");
			return list;
		}
		
		Ast.INode ParseDict(SeekableStringReader sr)
		{
			//dict            = '{' keyvalue_list trailing_comma '}' .
			//keyvalue_list   = keyvalue { ',' keyvalue } .
			//keyvalue        = expr ':' expr .
			// trailing_comma  = '' | ',' .			
			
			sr.Read();	// {
			sr.SkipWhitespace();
			Ast.DictNode dict = new Ast.DictNode();
			if(sr.Peek() == '}')
			{
				sr.Read();
				return dict;		// empty dict
			}
			
			List<Ast.INode> elts = ParseKeyValueList(sr);

			// handle trailing comma if present
			sr.SkipWhitespace();
			if(!sr.HasMore())
				throw new ParseException("missing '}'");
			if(sr.Peek() == ',')
				sr.Read();

			if(!sr.HasMore())
				throw new ParseException("missing '}'");
			char closechar = sr.Read();
			if(closechar!='}')
				throw new ParseException("expected '}'");
			
			// make sure it has dict semantics (remove duplicate keys)
			Dictionary<Ast.INode, Ast.INode> fixedDict = new Dictionary<Ast.INode, Ast.INode>(elts.Count);
			foreach(Ast.KeyValueNode kv in elts)
				fixedDict[kv.Key] = kv.Value;
			foreach(var kv in fixedDict)
			{
				dict.Elements.Add(new Ast.KeyValueNode()
				                  {
				                  	Key=kv.Key,
				                  	Value=kv.Value
				                  });
			}
			
			// SPECIAL CASE: {'__class__':'float','value':'nan'}  ---> Double.NaN
			if(dict.Elements.Count==2) {
				if(dict.Elements.Contains(new Ast.KeyValueNode(new Ast.StringNode("__class__"), new Ast.StringNode("float")))) {
					if(dict.Elements.Contains(new Ast.KeyValueNode(new Ast.StringNode("value"), new Ast.StringNode("nan")))) {
						return new Ast.DoubleNode(Double.NaN);
					}	
				}
			}

			return dict;
		}		
		
		public Ast.INode ParseSingle(SeekableStringReader sr)
		{
			// single =  int | float | complex | string | bool | none .
			sr.SkipWhitespace();
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
			// int or float or complex.
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
		
		
		const string IntegerChars = "-0123456789";
		const string FloatChars = "-+.eE0123456789";
		
		
		Ast.INode ParseInt(SeekableStringReader sr)
		{
			// int =  ['-'] digitnonzero {digit} .
			string numberstr = sr.ReadWhile(IntegerChars);
			if(numberstr.Length==0)
				throw new ParseException("invalid int character");
			try {
				try {
					return new Ast.IntegerNode(int.Parse(numberstr));
				} catch (OverflowException) {
					// try long
					try {
						return new Ast.LongNode(long.Parse(numberstr));
					} catch (OverflowException) {
						// try decimal, but it can still overflow because it's not arbitrary precision
						try {
							return new Ast.DecimalNode(decimal.Parse(numberstr));
						} catch (OverflowException) {
							throw new ParseException("number too large");
						}
					}
				}
			} catch (FormatException x) {
				throw new ParseException("invalid integer format", x);
			}
		}

		Ast.PrimitiveNode<double> ParseFloat(SeekableStringReader sr)
		{
			string numberstr = sr.ReadWhile(FloatChars);
			if(numberstr.Length==0)
				throw new ParseException("invalid float character");
			
			// little bit of a hack:
			// if the number doesn't contain a decimal point and no 'e'/'E', it is an integer instead.
			// in that case, we need to reject it as a float.
			if(numberstr.IndexOfAny(new char[] {'.','e','E'}) < 0)
				throw new ParseException("number is not a float (might be an integer though)");

			try {
				return new Ast.DoubleNode(this.ParseDouble(numberstr));
			} catch (FormatException x) {
				throw new ParseException("invalid float format", x);
			}
		}

		Ast.ComplexNumberNode ParseComplex(SeekableStringReader sr)
		{
			//complex         = complextuple | imaginary .
			//imaginary       = ['+' | '-' ] ( float | int ) 'j' .
			//complextuple    = '(' ( float | int ) imaginary ')' .
			if(sr.Peek()=='(')
			{
				// complextuple
				sr.Read();  // (
				string numberstr;
				if(sr.Peek()=='-' || sr.Peek()=='+')
				{
					// starts with a sign, read that first otherwise the readuntil will return immediately
					numberstr = sr.Read(1) + sr.ReadUntil(new char[] {'+', '-'});
				}
				else
					numberstr = sr.ReadUntil(new char[] {'+', '-'});
				double realpart;
				try {
					realpart = this.ParseDouble(numberstr);
				} catch (FormatException x) {
					throw new ParseException("invalid float format", x);
				}
				sr.Rewind(1); // rewind the +/-
				double imaginarypart = ParseImaginaryPart(sr);
				if(sr.Read()!=')')
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
//			string numberstr = sr.ReadUntil('j');
//			try {
//				return this.ParseDouble(numberstr);
//			} catch(FormatException x) {
//				throw new ParseException("invalid float format", x);
//			}

			if(!sr.HasMore())
				throw new ParseException("unexpected end of input string");
				
			char sign_or_digit = sr.Peek();
			if(sign_or_digit=='+')
				sr.Read();   // skip the '+'
			
			// now an int or float follows.
			double double_value;
			int bookmark = sr.Bookmark();
			try {
				var float_part = ParseFloat(sr);
				double_value = float_part.Value;
			} catch (ParseException) {
				sr.FlipBack(bookmark);
				var integer_part = ParseInt(sr);
				var integerNode = integer_part as Ast.IntegerNode;
				if (integerNode != null)
					double_value = integerNode.Value;
				else {
					var longNode = integer_part as Ast.LongNode;
					if (longNode != null)
						double_value = longNode.Value;
					else {
						var decimalNode = integer_part as Ast.DecimalNode;
						if (decimalNode != null)
							double_value = Convert.ToDouble(decimalNode.Value);
						else
							throw new ParseException("not an integer for the imaginary part");
					}
				}
			}
			
			// now a 'j' must follow!
			sr.SkipWhitespace();
			try {
				char j_char = sr.Read();
				if(j_char!='j')
					throw new ParseException("not an imaginary part");
			} catch (IndexOutOfRangeException) {
				throw new ParseException("not an imaginary part");
			}
			return double_value;

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
					return new Ast.StringNode(sb.ToString());
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
				return new Ast.BooleanNode(true);
			if(b=="Fals")
				return new Ast.BooleanNode(false);
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
		
		double ParseDouble(string numberstr)
		{
			// the number is possibly +Inf/-Inf, these are encoded as "1e30000" and "-1e30000"
			if(numberstr=="1e30000") return double.PositiveInfinity;
			if(numberstr=="-1e30000") return double.NegativeInfinity;
			return double.Parse(numberstr, CultureInfo.InvariantCulture);
		}


		/// <summary>
    	/// Utility function to convert obj back to actual bytes if it is a serpent-encoded bytes dictionary
    	/// (a IDictionary with base-64 encoded 'data' in it and 'encoding'='base64').
    	/// If obj is already a byte array, return obj unmodified.
    	/// If it is something else, throw an ArgumentException
		/// </summary>
		public static byte[] ToBytes(object obj) {
#if !(SILVERLIGHT || WINDOWS_PHONE || PORTABLE)
			Hashtable hashtable  = obj as Hashtable;
			if(hashtable!=null)
			{
				string data = null;
				string encoding = null;
				if(hashtable.Contains("data")) data = (string)hashtable["data"];
				if(hashtable.Contains("encoding")) encoding = (string)hashtable["encoding"];
				if(data==null || "base64"!=encoding)
				{
					throw new ArgumentException("argument is neither bytearray nor serpent base64 encoded bytes dict");
				}
				return Convert.FromBase64String(data);
			}
#endif
			
			IDictionary<string,string> dict = obj as IDictionary<string,string>;
			if(dict!=null)
			{
				string data;
				string encoding;
				bool hasData = dict.TryGetValue("data", out data);
				bool hasEncoding = dict.TryGetValue("encoding", out encoding);
				if(!hasData || !hasEncoding || encoding!="base64")
				{
					throw new ArgumentException("argument is neither bytearray nor serpent base64 encoded bytes dict");
				}
				return Convert.FromBase64String(data);
			}
			byte[] bytearray = obj as byte[];
			if(bytearray!=null)
			{
				return bytearray;
			}
			throw new ArgumentException("argument is neither bytearray nor serpent base64 encoded bytes dict");
		}
	}
}

