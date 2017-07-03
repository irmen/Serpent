/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent;

import java.io.UnsupportedEncodingException;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.Base64;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import net.razorvine.serpent.ast.*;


/**
 * Parse a Python literal into an Ast (abstract syntax tree).
 */
public class Parser
{
	/**
	 * Parse from a byte array (containing utf-8 encoded string with the Python literal expression in it)
	 */
	public Ast parse(byte[] serialized) throws ParseException
	{
		try {
			return parse(new String(serialized, "utf-8"));
		} catch (UnsupportedEncodingException e) {
			throw new ParseException(e.toString());
		}
	}
	
	/**
	 * Parse from a string with the Python literal expression
	 */
	public Ast parse(String expression)
	{
		Ast ast=new Ast();
		if(expression==null || expression.length()==0)
			return ast;
		
		SeekableStringReader sr = new SeekableStringReader(expression);
		if(sr.peek()=='#')
			sr.readUntil('\n');  // skip comment line
		
		try {
			ast.root = parseExpr(sr);
			sr.skipWhitespace();
			if(sr.hasMore())
				throw new ParseException("garbage at end of expression");
			return ast;
		} catch (ParseException x) {
			String faultLocation = extractFaultLocation(sr);
			throw new ParseException(x.getMessage() + " (at position "+sr.bookmark()+"; '"+faultLocation+"')", x);
		}
	}
	
	String extractFaultLocation(SeekableStringReader sr)
	{
		SeekableStringReader.StringContext ctx = sr.context(-1, 20);
		return String.format("...%s>>><<<%s...", ctx.left, ctx.right);
	}

	INode parseExpr(SeekableStringReader sr)
	{
		// expr =  [ <whitespace> ] single | compound [ <whitespace> ] .
		sr.skipWhitespace();
		if(!sr.hasMore())
			throw new ParseException("unexpected end of line, missing expression or close/open character");
		char c = sr.peek();
		INode node;
		if(c=='{' || c=='[' || c=='(')
			node = parseCompound(sr);
		else
			node = parseSingle(sr);
		sr.skipWhitespace();
		return node;
	}

	INode parseCompound(SeekableStringReader sr)
	{
		// compound =  tuple | dict | list | set .
		sr.skipWhitespace();
		switch(sr.peek())
		{
			case '[':
				return parseList(sr);
			case '{':
				{
					int bm = sr.bookmark();
					try {
						return parseSet(sr);
					} catch(ParseException x) {
						sr.flipBack(bm);
						return parseDict(sr);
					}
				}
			case '(':
				// tricky case here, it can be a tuple but also a complex number:
				// if the last character before the closing parenthesis is a 'j', it is a complex number
				{
					int bm = sr.bookmark();
					String betweenparens = sr.readUntil(")\n").trim();
					sr.flipBack(bm);
					return betweenparens.endsWith("j") ? parseComplex(sr) : parseTuple(sr);
				}
			default:
				throw new ParseException("invalid sequencetype char");
		}
	}

	TupleNode parseTuple(SeekableStringReader sr)
	{
		//tuple           = tuple_empty | tuple_one | tuple_more
		//tuple_empty     = '()' .
		//tuple_one       = '(' expr ',' <whitespace> ')' .
		//tuple_more      = '(' expr_list trailing_comma ')' .
		// trailing_comma  = '' | ',' .

		sr.read();	// (
		sr.skipWhitespace();
		TupleNode tuple = new TupleNode();
		if(sr.peek() == ')')
		{
			sr.read();
			return tuple;		// empty tuple
		}
		
		INode firstelement = parseExpr(sr);
		if(sr.peek() == ',')
		{
			sr.read();
			sr.skipWhitespace();
			if(sr.read() == ')')
			{
				// tuple with just a single element
				tuple.elements.add(firstelement);
				return tuple;
			}
			sr.rewind(1);   // undo the thing that wasn't a )
		}
		
		tuple.elements = parseExprList(sr);
		tuple.elements.add(0, firstelement);

		// handle trailing comma if present
		sr.skipWhitespace();
		if(!sr.hasMore())
			throw new ParseException("missing ')'");
		if(sr.peek() == ',')
			sr.read();

		if(!sr.hasMore())
			throw new ParseException("missing ')'");
		char closechar = sr.read();
		if(closechar==',')
			closechar = sr.read();
		if(closechar!=')')
			throw new ParseException("expected ')'");
		return tuple;			
	}
	
	List<INode> parseExprList(SeekableStringReader sr)
	{
		//expr_list       = expr { ',' expr } .
		List<INode> exprList = new ArrayList<INode>();
		exprList.add(parseExpr(sr));
		while(sr.hasMore() && sr.peek() == ',')
		{
			sr.read();
			try {
				exprList.add(parseExpr(sr));
			} catch (ParseException x) {
				sr.rewind(1);
				break;
			}
		}
		return exprList;
	}

	List<INode> parseKeyValueList(SeekableStringReader sr)
	{
		//keyvalue_list   = keyvalue { ',' keyvalue } .
		List<INode> kvs = new ArrayList<INode>();
		kvs.add(parseKeyValue(sr));
		while(sr.hasMore() && sr.peek()==',')
		{
			sr.read();
			try {
				kvs.add(parseKeyValue(sr));
			} catch (ParseException x) {
				sr.rewind(1);
				break;
			}
		}
		return kvs;
	}		

	KeyValueNode parseKeyValue(SeekableStringReader sr)
	{
		//keyvalue        = expr ':' expr .
		INode key = parseExpr(sr);
		if(sr.hasMore() && sr.peek()==':')
		{
			sr.read(); // :
			INode value = parseExpr(sr);
			KeyValueNode kv = new KeyValueNode();
			kv.key = key;
			kv.value = value;
			return kv;
		}
		throw new ParseException("expected ':'");
	}
	
	
	SetNode parseSet(SeekableStringReader sr)
	{
		// set = '{' expr_list trailing_comma '}' .
		// trailing_comma  = '' | ',' .
		sr.read();	// {
		sr.skipWhitespace();
		SetNode setnode = new SetNode();
		List<INode> elts = parseExprList(sr);

		// handle trailing comma if present
		sr.skipWhitespace();
		if(!sr.hasMore())
			throw new ParseException("missing '}'");
		if(sr.peek() == ',')
			sr.read();

		if(!sr.hasMore())
			throw new ParseException("missing '}'");
		char closechar = sr.read();
		if(closechar!='}')
			throw new ParseException("expected '}'");

		// make sure it has set semantics (remove duplicate elements)
		Set<INode> h = new HashSet<INode>(elts);
		setnode.elements = new ArrayList<INode>(h);
		return setnode;
	}
	
	ListNode parseList(SeekableStringReader sr)
	{
		// list            = list_empty | list_nonempty .
		// list_empty      = '[]' .
		// list_nonempty   = '[' expr_list trailing_comma ']' .
		// trailing_comma  = '' | ',' .
		sr.read();	// [
		sr.skipWhitespace();
		ListNode list = new ListNode();
		if(sr.peek() == ']')
		{
			sr.read();
			return list;		// empty list
		}
		
		list.elements = parseExprList(sr);

		// handle trailing comma if present
		sr.skipWhitespace();
		if(!sr.hasMore())
			throw new ParseException("missing ']'");
		if(sr.peek() == ',')
			sr.read();

		if(!sr.hasMore())
			throw new ParseException("missing ']'");
		char closechar = sr.read();
		if(closechar!=']')
			throw new ParseException("expected ']'");
		return list;
	}
	
	INode parseDict(SeekableStringReader sr)
	{
		//dict            = '{' keyvalue_list trailing_comma '}' .
		//keyvalue_list   = keyvalue { ',' keyvalue } .
		//keyvalue        = expr ':' expr .
		// trailing_comma  = '' | ',' .
		
		sr.read();	// {
		sr.skipWhitespace();
		DictNode dict = new DictNode();
		if(sr.peek() == '}')
		{
			sr.read();
			return dict;		// empty dict
		}
		
		List<INode> elts = parseKeyValueList(sr);

		// handle trailing comma if present
		sr.skipWhitespace();
		if(!sr.hasMore())
			throw new ParseException("missing '}'");
		if(sr.peek() == ',')
			sr.read();

		if(!sr.hasMore())
			throw new ParseException("missing '}'");
		char closechar = sr.read();
		if(closechar!='}')
			throw new ParseException("expected '}'");
		
		// make sure it has dict semantics (remove duplicate keys)
		Map<INode, INode> fixedDict = new HashMap<INode, INode>(elts.size());
		for(INode e: elts)
		{
			KeyValueNode kv = (KeyValueNode)e;
			fixedDict.put(kv.key, kv.value);
		}
		for(Map.Entry<INode, INode> e: fixedDict.entrySet())
		{
			KeyValueNode kvnode = new KeyValueNode();
			kvnode.key = e.getKey();
			kvnode.value = e.getValue();
			dict.elements.add(kvnode);
		}
		
		// SPECIAL CASE: {'__class__':'float','value':'nan'}  ---> Double.NaN
		if(dict.elements.size()==2) {
			if(dict.elements.contains(new KeyValueNode(new StringNode("__class__"), new StringNode("float")))) {
				if(dict.elements.contains(new KeyValueNode(new StringNode("value"), new StringNode("nan")))) {
					return new DoubleNode(Double.NaN);
				}	
			}
		}
		return dict;
	}		
	
	public INode parseSingle(SeekableStringReader sr)
	{
		// single =  int | float | complex | string | bool | none .
		sr.skipWhitespace();
		switch(sr.peek())
		{
			case 'N':
				return parseNone(sr);
			case 'T':
			case 'F':
				return parseBool(sr);
			case '\'':
			case '"':
				return parseString(sr);
		}
		// int or float or complex.
		int bookmark = sr.bookmark();
		try {
			return parseComplex(sr);
		} catch (ParseException x1) {
			sr.flipBack(bookmark);
			try {
				return parseFloat(sr);
			} catch (ParseException x2) {
				sr.flipBack(bookmark);
				return parseInt(sr);
			}
		}
	}
	
	final String FloatCharacters = "-+.eE0123456789";
	final String IntCharacters = "-0123456789";

	INode parseInt(SeekableStringReader sr)
	{
		// int =  ['-'] digitnonzero {digit} .
		String numberstr = sr.readWhile(IntCharacters);
		if(numberstr.length()==0)
			throw new ParseException("invalid int character");
		try {
			try {
				return new IntegerNode(Integer.parseInt(numberstr));
			} catch (NumberFormatException x1) {
				// try long
				try {
					return new LongNode(Long.parseLong(numberstr));
				} catch (NumberFormatException x2) {
					// try bigint, but it can still overflow because it's not arbitrary precision
					try {
						return new BigIntNode(new BigInteger(numberstr));
					} catch (NumberFormatException x3) {
						throw new ParseException("number too large or invalid");
					}
				}
			}
		} catch (NumberFormatException x) {
			throw new ParseException("invalid integer format", x);
		}
	}

	PrimitiveNode<Double> parseFloat(SeekableStringReader sr)
	{
		String numberstr = sr.readWhile(FloatCharacters);
		if(numberstr.length()==0)
			throw new ParseException("invalid float character");
		
		// little bit of a hack:
		// if the number doesn't contain a decimal point and no 'e'/'E', it is an integer instead.
		// in that case, we need to reject it as a float.
		if(numberstr.indexOf('.')<0 && numberstr.indexOf('e')<0 && numberstr.indexOf('E')<0)
			throw new ParseException("number is not a float (might be an integer though)");

		try {
			return new DoubleNode(Double.parseDouble(numberstr));
		} catch (NumberFormatException x) {
			throw new ParseException("invalid float format", x);
		}
	}

	ComplexNumberNode parseComplex(SeekableStringReader sr)
	{
		//complex         = complextuple | imaginary .
		//imaginary       = ['+' | '-' ] ( float | int ) 'j' .
		//complextuple    = '(' ( float | int ) imaginary ')' .
		if(sr.peek()=='(')
		{
			// complextuple
			sr.read();  // (
			String numberstr;
			if(sr.peek()=='-' || sr.peek()=='+')
			{
				// starts with a sign, read that first otherwise the readuntil will return immediately
				numberstr = sr.read(1) + sr.readUntil("+-");
			}
			else
			{
				numberstr = sr.readUntil("+-");
			}
			sr.rewind(1); // rewind the +/-
			
			// because we're a bit more cautious here with reading chars than in the float parser,
			// it can be that the parser now stopped directly after the 'e' in a number like "3.14e+20".
			// ("3.14e20" is fine) So, check if the last char is 'e' and if so, continue reading 0..9.
			if(numberstr.endsWith("e")||numberstr.endsWith("E")) {
				// if the next symbol is + or -, accept it, then read the exponent integer
				if(sr.peek()=='-' || sr.peek()=='+')
					numberstr+=sr.read(1);
				numberstr += sr.readWhile("0123456789");
			}
			
			sr.skipWhitespace();
			double real;
			try {
				real = Double.parseDouble(numberstr);
			} catch (NumberFormatException x) {
				throw new ParseException("invalid float format", x);
			}
			double imaginarypart = parseImaginaryPart(sr);
			if(sr.read()!=')')
				throw new ParseException("expected ) to end a complex number");
			
			ComplexNumberNode c = new ComplexNumberNode();
			c.real = real;
			c.imaginary = imaginarypart;
			return c;
		}
		else
		{
			// imaginary
			double imag = parseImaginaryPart(sr);
			ComplexNumberNode c = new ComplexNumberNode();
			c.real = 0;
			c.imaginary = imag;
			return c;
		}
	}
	
	double parseImaginaryPart(SeekableStringReader sr)
	{
		//imaginary       = ['+' | '-' ] ( float | int ) 'j' .
		if(!sr.hasMore())
			throw new ParseException("unexpected end of input string");
		char sign_or_digit = sr.peek();
		
		if(sign_or_digit=='+')
			sr.read();   // skip the '+'
		
		// now an int or float follows.
		double double_value;
		int bookmark = sr.bookmark();
		try {
			PrimitiveNode<Double> float_part = parseFloat(sr);
			double_value = float_part.value;
		} catch (ParseException x1) {
			sr.flipBack(bookmark);
			INode integer_part = parseInt(sr);
			if(integer_part instanceof IntegerNode) {
				double_value = ((IntegerNode)integer_part).value;
			}
			else if(integer_part instanceof LongNode) {
				double_value = ((LongNode)integer_part).value;
			}
			else if(integer_part instanceof BigIntNode) {
				double_value = ((BigIntNode)integer_part).value.doubleValue();
			}
			else {
				throw new ParseException("not an integer for the imaginary part");
			}
		}
		
		// now a 'j' must follow!
		sr.skipWhitespace();
		try {
			char j_char = sr.read();
			if(j_char!='j')
				throw new ParseException("not an imaginary part");
		} catch (IndexOutOfBoundsException x) {
			throw new ParseException("not an imaginary part");
		}
		return double_value;
	}
	
	PrimitiveNode<String> parseString(SeekableStringReader sr)
	{
		char quotechar = sr.read();   // ' or "
		StringBuilder sb = new StringBuilder(10);
		while(sr.hasMore())
		{
			char c = sr.read();
			if(c=='\\')
			{
				// backslash unescape
				c = sr.read();
				switch(c)
				{
					case '\\':
						sb.append('\\');
						break;
					case '\'':
						sb.append('\'');
						break;
					case '"':
						sb.append('"');
						break;
					case 'b':
						sb.append('\b');
						break;
					case 'f':
						sb.append('\f');
						break;
					case 'n':
						sb.append('\n');
						break;
					case 'r':
						sb.append('\r');
						break;
					case 't':
						sb.append('\t');
						break;
					case 'x':		//  "\x00"
						sb.append((char)Integer.parseInt(sr.read(2), 16));
						break;
					case 'u':		//  "\u0000"
						sb.append((char)Integer.parseInt(sr.read(4), 16));
						break;
					default:
						sb.append(c);
						break;
				}
			}
			else if(c==quotechar)
			{
				// end of string
				return new StringNode(sb.toString());
			}
			else
			{
				sb.append(c);
			}
		}
		throw new ParseException("unclosed string");
	}
	
	PrimitiveNode<Boolean> parseBool(SeekableStringReader sr)
	{
		// True,False
		String b = sr.readUntil('e');
		if(b.equals("Tru"))
			return new BooleanNode(true);
		if(b.equals("Fals"))
			return new BooleanNode(false);
		throw new ParseException("expected bool, True or False");
	}
	
	NoneNode parseNone(SeekableStringReader sr)
	{
		// None
		String n = sr.readUntil('e');
		if(n.equals("Non"))
			return NoneNode.Instance;
		throw new ParseException("expected None");
	}

	/**
	 * Utility function to convert obj back to actual bytes if it is a serpent-encoded bytes dictionary
	 * (a IDictionary with base-64 encoded 'data' in it and 'encoding'='base64').
	 * If obj is already a byte array, return obj unmodified.
	 * If it is something else, throw an IllegalArgumentException
	 */
	public static byte[] toBytes(Object obj) {
		if(obj instanceof Map<?,?>)
		{
			@SuppressWarnings("unchecked")
			Map<String,String> dict = (Map<String,String>)obj;
			String data = dict.get("data");
			String encoding = dict.get("encoding");
			if(data==null || encoding==null || !encoding.equals("base64"))
			{
				throw new IllegalArgumentException("argument is neither bytearray nor serpent base64 encoded bytes dict");
			}
			return Base64.getDecoder().decode(data);
		}
		if(obj instanceof byte[])
		{
			return (byte[]) obj;
		}
		throw new IllegalArgumentException("argument is neither bytearray nor serpent base64 encoded bytes dict");
	}
}
