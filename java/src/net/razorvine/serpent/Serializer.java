/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in .NET)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent;

import java.beans.BeanInfo;
import java.beans.IntrospectionException;
import java.beans.Introspector;
import java.beans.PropertyDescriptor;
import java.io.IOException;
import java.io.PrintWriter;
import java.io.Serializable;
import java.io.StringWriter;
import java.lang.reflect.Array;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.math.BigDecimal;
import java.text.SimpleDateFormat;
import java.util.*;

import javax.xml.bind.DatatypeConverter;

/**
 * Serialize an object tree to a byte stream.
 * It is not thread-safe: make sure you're not making changes to the object tree that is being serialized.
 */
public class Serializer
{
	public boolean indent = false;
	public boolean setliterals = true;
	
	public Serializer()
	{
	}
	
	public Serializer(boolean indent, boolean setliterals)
	{
		this.indent = indent;
		this.setliterals = setliterals;
	}
	
	public byte[] serialize(Object obj) throws IOException
	{
		StringWriter sw = new StringWriter();
		PrintWriter pw = new PrintWriter(sw);

		String header = "# serpent utf-8 ";
		if(this.setliterals)
			header += "python3.2\n";  // set-literals require python 3.2+ to deserialize (ast.literal_eval limitation)
		else
			header += "python2.6\n";
		pw.print(header);
		serialize(obj, pw, 0);
		
		pw.flush();
		String ser = sw.toString();
		pw.close();
		sw.close();
		
		return ser.getBytes("utf-8");
	}
	
	protected void serialize(Object obj, PrintWriter p, int level)
	{
		// null -> None
		// hashtables/dictionaries -> dict
		// hashset -> set
		// array -> tuple
		// byte arrays --> base64
		// any other icollection --> list
		// date/timespan/uuid/exception -> custom mapping
		// random class --> public properties to dict
		// primitive types --> simple mapping
		
		Class type = obj==null? null : obj.getClass();
		Class componentType = type==null? null : type.getComponentType();
		
		// primitive array?
		if(componentType!=null)
		{
			// byte array? encode as base-64
			if(componentType==Byte.TYPE)
			{
				serialize_bytes((byte[])obj, p, level);
				return;
			}
			else
			{
				serialize_primitive_array(obj, p, level);
			}
			return;
		}
		
		if(obj==null)
		{
			p.print("None");
		}
		else if(obj instanceof String)
		{
			serialize_string((String)obj, p, level);
		}
		else if(type.isPrimitive() || isBoxed(type))
		{
			serialize_primitive(obj, p, level);
		}
		else if(obj instanceof Enum)
		{
			serialize_string(obj.toString(), p, level);
		}
		else if(obj instanceof BigDecimal)
		{
			serialize_bigdecimal((BigDecimal)obj, p, level);
		}
		else if(obj instanceof Number)
		{
			serialize_primitive(obj, p, level);
		}
		else if(obj instanceof Date)
		{
			// a java Date contains a date+time so map this on Calendar
			// which will be pickled as a datetime.
			java.util.Date date=(java.util.Date)obj;
			Calendar cal=GregorianCalendar.getInstance();
			cal.setTime(date);
			serialize_calendar(cal, p, level);
		}
		else if(obj instanceof Calendar)
		{
			serialize_calendar((Calendar)obj, p, level);
		}
		else if(obj instanceof UUID)
		{
			serialize_uuid((UUID)obj, p, level);
		}
		else if(obj instanceof Set<?>)
		{
			serialize_set((Set<?>)obj, p, level);
		}
		else if(obj instanceof Map<?,?>)
		{
			serialize_dict((Map<?,?>)obj, p, level);
		}
		else if(obj instanceof Collection<?>)
		{
			serialize_collection((Collection<?>)obj, p, level);
		}
		else if(obj instanceof ComplexNumber)
		{
			serialize_complex((ComplexNumber)obj, p, level);
		}
		else if(obj instanceof Exception)
		{
			serialize_exception((Exception)obj, p, level);
		}
		else if(obj instanceof Serializable)
		{
			serialize_class(obj, p, level);
		}
		else
		{
			throw new IllegalArgumentException("cannot serialize object of type "+type);
		}
	}
	
	protected void serialize_collection(Collection<?> collection, PrintWriter p, int level)
	{
		// output a list
		p.print("[");
		serialize_sequence_elements(collection, false, p, level+1);
		if(this.indent && collection.size()>0)
		{
			for(int i=0; i<level; ++i)
				p.print("  ");
		}
		p.print("]");
	}

	protected void serialize_sequence_elements(Collection<?> elts, boolean trailingComma, PrintWriter p, int level)
	{
		if(elts.size()==0)
			return;
		int count=0;
		if(this.indent)
		{
			p.print("\n");
			String innerindent = "";
			for(int i=0; i<level; ++i)
				innerindent += "  ";
			for(Object e: elts)
			{
				p.print(innerindent);
				serialize(e, p, level);
				count++;
				if(count<elts.size())
				{
					p.print(",\n");
				}
			}
			if(trailingComma)
				p.print(",");
			p.print("\n");
		}
		else
		{
			for(Object e: elts)
			{
				serialize(e, p, level);
				count++;
				if(count<elts.size())
					p.print(",");
			}
			if(trailingComma)
				p.print(",");
		}
	}

	protected void serialize_set(Set<?> set, PrintWriter p, int level)
	{
		if(!this.setliterals)
		{
			// output a tuple instead
			serialize_tuple(set, p, level);
			return;
		}
		
		throw new NoSuchMethodError();  // @TODO
	}

	protected void serialize_primitive_array(Object array, PrintWriter p, int level)
	{
		// output a tuple
		int length = Array.getLength(array);
		ArrayList<Object> items = new ArrayList<Object>(length);
		for(int i=0; i<length; ++i)
			items.add(Array.get(array, i));
		serialize_tuple(items, p, level);
	}

	protected void serialize_tuple(Collection<?> items, PrintWriter p, int level)
	{
		p.print("(");
		serialize_sequence_elements(items, items.size()==1, p, level+1);
		if(this.indent && items.size()>0)
		{
			for(int i=0; i<level; ++i)
				p.print("  ");
		}
		p.print(")");
	}

	protected void serialize_bytes(byte[] obj, PrintWriter p, int level)
	{
		// base-64 struct output
		String str = DatatypeConverter.printBase64Binary(obj);
		Map<String, String> dict = new HashMap<String, String>();
		dict.put("data", str);
		dict.put("encoding", "base64");
		serialize_dict(dict, p, level);
	}

	protected void serialize_dict(Map<?, ?> dict, PrintWriter p,int level)
	{
		for(Map.Entry<?,?> e: dict.entrySet())
		{
			p.print("key=");
			p.print(e.getKey());
			p.print(" value=");
			p.print(e.getValue());
			p.print("\n");
			// @TODO
		}
	}

	protected void serialize_calendar(Calendar cal, PrintWriter p, int level)
	{
		// note: this doesn't output any Timezone information.
		SimpleDateFormat fmt;
		if(cal.get(Calendar.MILLISECOND)==0)
			fmt = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");
		else
			fmt = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSSSSS");
		fmt.setCalendar(cal);
		serialize_string(fmt.format(cal.getTime()), p, level);
	}

	protected void serialize_complex(ComplexNumber cplx, PrintWriter p, int level)
	{
		p.print("(");
		serialize_primitive(cplx.real, p, level);
		if(cplx.imaginary>=0)
			p.print("+");
		serialize_primitive(cplx.imaginary, p, level);
		p.print("j)");
	}

	protected void serialize_uuid(UUID obj, PrintWriter p, int level)
	{
		serialize_string(obj.toString(), p, level);
	}

	protected void serialize_bigdecimal(BigDecimal decimal, PrintWriter p, int level)
	{
		serialize_string(decimal.toEngineeringString(), p, level);
	}

	private static final HashSet<Class> boxedTypes = new HashSet<Class>() {{
		add(Boolean.class);
		add(Character.class);
		add(Byte.class);
		add(Short.class);
		add(Integer.class);
		add(Long.class);
		add(Float.class);
		add(Double.class);
	}};

	protected boolean isBoxed(Class type)
	{
		return boxedTypes.contains(type);
	}

	protected void serialize_class(Object obj, PrintWriter p, int level) 
	{
		Map<String,Object> map=new HashMap<String,Object>();
		try {
			BeanInfo info=Introspector.getBeanInfo(obj.getClass(), Object.class);
			for(PropertyDescriptor pd: info.getPropertyDescriptors()) {
				String name=pd.getName();
				Method readmethod=pd.getReadMethod();
				if(readmethod==null) {
					throw new IllegalArgumentException("can't find public read method for bean property '"+name+"' in class "+obj.getClass());
				}
				Object value=readmethod.invoke(obj);
				map.put(name, value);
			}
			map.put("__class__", obj.getClass().getSimpleName());
			serialize_dict(map, p, level);
		} catch (IntrospectionException e) {
			throw new IllegalArgumentException("couldn't introspect javabean: "+e);
		} catch (IllegalAccessException e) {
			throw new IllegalArgumentException("couldn't introspect javabean: "+e);
		} catch (InvocationTargetException e) {
			throw new IllegalArgumentException("couldn't introspect javabean: "+e);
		}
	}

	protected void serialize_primitive(Object obj, PrintWriter p, int level) 
	{
		p.print(obj);
	}

	protected void serialize_string(String str, PrintWriter p, int level)
	{
		// backslash-escaped string
		str = str.replace("\\", "\\\\");  // double-escape the backslashes
		if(!str.contains("'"))
        	str = "'" + str + "'";
		else if(!str.contains("\""))
			str = '"' + str + '"';
    	else
    	{
        	str = str.replace("'", "\\'");
        	str = "'" + str + "'";
    	}
		p.print(str);
	}
	
	protected void serialize_exception(Exception ex, PrintWriter p, int level)
	{
		Map<String, Object> dict = new HashMap<String,Object>();
		dict.put("__class__", ex.getClass().getSimpleName());
		dict.put("__exception__", true);
		dict.put("args", null);
		dict.put("message", ex.getMessage());
		serialize_dict(dict, p, level);
	}
}

/***
			
		protected void Serialize_tuple(ICollection array, TextWriter tw, int level)
		{

		}

		
		protected int DictentryCompare(DictionaryEntry d1, DictionaryEntry d2)
		{
			IComparable c1 = d1.Key as IComparable;
			IComparable c2 = d2.Key as IComparable;
			
			if(c1==null) return 0;
			return c1.CompareTo(c2);
		}

		protected void Serialize_dict(IDictionary dict, TextWriter tw, int level)
		{
			if(dict.Count==0)
			{
				tw.Write("{}");
				return;
			}
			int counter=0;
			if(this.Indent)
			{
				string innerindent = string.Join("  ", new string[level+2]);
				tw.Write("{\n");
				DictionaryEntry[] entries = new DictionaryEntry[dict.Count];
				dict.CopyTo(entries, 0);
				try {
					Array.Sort(entries, DictentryCompare);
				} catch (InvalidOperationException) {
					// ignore sorting of incomparable elements
				} catch (ArgumentException) {
					// ignore sorting of incomparable elements
				}
				foreach(DictionaryEntry x in entries)
				{
					tw.Write(innerindent);
					Serialize(x.Key, tw, level+1);
					tw.Write(": ");
					Serialize(x.Value, tw, level+1);
					counter++;
					if(counter<dict.Count)
						tw.Write(",\n");
				}
				tw.Write("\n");
				tw.Write(string.Join("  ", new string[level+1]));
				tw.Write("}");
			}
			else
			{
				tw.Write("{");
				foreach(DictionaryEntry x in dict)
				{
					Serialize(x.Key, tw, level+1);
					tw.Write(":");
					Serialize(x.Value, tw, level+1);
					counter++;
					if(counter<dict.Count)
						tw.Write(",");
				}
				tw.Write("}");
			}
		}
		
		protected void Serialize_set(object[] set, TextWriter tw, int level)
		{
			if(set.Length>0)
			{
				tw.Write("{");
				if(this.Indent)
				{
					try {
						Array.Sort(set);
					} catch (InvalidOperationException) {
						// ignore sorting of incomparable elements.
					} catch (ArgumentException) {
						// ignore sorting of incomparable elements.
					}
				}
				Serialize_sequence_elements(set, tw, level+1);
				if(this.Indent)
					tw.Write(string.Join("  ", new string[level+1]));
				tw.Write("}");
			}
			else
			{
				// empty set literal doesn't exist, replace with empty tuple
				Serialize_tuple(new object[0], tw, level+1);
			}
		}
		
	}
}
***/