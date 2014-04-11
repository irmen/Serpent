/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent;

import java.io.IOException;
import java.io.PrintWriter;
import java.io.Serializable;
import java.io.StringWriter;
import java.lang.reflect.Array;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
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
	public boolean packageInClassName = false;
	private static Map<Class<?>, IClassSerializer> classToDictRegistry = new HashMap<Class<?>, IClassSerializer>();
	
	public Serializer()
	{
	}
	
	public Serializer(boolean indent, boolean setliterals, boolean packageInClassName)
	{
		this.indent = indent;
		this.setliterals = setliterals;
		this.packageInClassName = packageInClassName;
	}
	
	public static void registerClass(Class<?> clazz, IClassSerializer converter)
	{
		classToDictRegistry.put(clazz, converter);
	} 
	
	public byte[] serialize(Object obj)
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
		try {
			sw.close();
			return ser.getBytes("utf-8");
		} catch (IOException x) {
			throw new IllegalArgumentException("error creating output bytes: "+x);
		}
	}
	
	protected void serialize(Object obj, PrintWriter p, int level)
	{
		// null -> None
		// hashtables/dictionaries -> dict
		// hashset -> set
		// array -> tuple
		// byte arrays --> base64
		// any other collection --> list
		// date//uuid/exception -> custom mapping
		// random class --> public javabean properties to dictionary
		// primitive types --> simple mapping
		
		Class<?> type = obj==null? null : obj.getClass();
		Class<?> componentType = type==null? null : type.getComponentType();
		
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
			// output a tuple instead of a set-literal
			serialize_tuple(set, p, level);
			return;
		}
		
		if(set.size()>0)
		{
			p.print("{");
			Collection<?> output = set;
			if(this.indent)
			{
				// try to sort the set
				Set<?> outputset = set;
				try {
					outputset = new TreeSet<Object>(set);
				} catch (ClassCastException x) {
					// ignore unsortable elements
				}
				output = outputset;
			}
			serialize_sequence_elements(output, false, p, level+1);
	
			if(this.indent)
			{
				for(int i=0; i<level; ++i)
					p.print("  ");
			}
			p.print("}");
		}
		else
		{
			// empty set literal doesn't exist, replace with empty tuple
			serialize_tuple(Collections.EMPTY_LIST, p, level+1);
		}
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
		if(dict.size()==0)
		{
			p.print("{}");
			return;
		}

		int counter=0;
		if(this.indent)
		{
			String innerindent = "  ";
			for(int i=0; i<level; ++i)
				innerindent += "  ";
			p.print("{\n");
			
			// try to sort the dictionary keys
			Map<?,?> outputdict = dict;
			try {
				outputdict = new TreeMap<Object,Object>(dict);
			} catch (ClassCastException x) {
				// ignore unsortable keys
			}
			
			for(Map.Entry<?,?> e: outputdict.entrySet())
			{
				p.print(innerindent);
				serialize(e.getKey(), p, level+1);
				p.print(": ");
				serialize(e.getValue(), p, level+1);
				counter++;
				if(counter<dict.size())
					p.print(",\n");
			}
			p.print("\n");
			for(int i=0; i<level; ++i)
				p.print("  ");
			p.print("}");
		}
		else
		{
			p.print("{");
			for(Map.Entry<?,?> e: dict.entrySet())
			{
				serialize(e.getKey(), p, level+1);
				p.print(":");
				serialize(e.getValue(), p, level+1);
				counter++;
				if(counter<dict.size())
					p.print(",");
			}
			p.print("}");
		}
	}

	protected void serialize_calendar(Calendar cal, PrintWriter p, int level)
	{
		// note: this doesn't output any Timezone information.
		SimpleDateFormat fmt;
		if(cal.get(Calendar.MILLISECOND)==0)
			fmt = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");
		else
			fmt = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS");
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

	private static final HashSet<Class<?>> boxedTypes;
	static {
		boxedTypes = new HashSet<Class<?>>();
		boxedTypes.add(Boolean.class);
		boxedTypes.add(Character.class);
		boxedTypes.add(Byte.class);
		boxedTypes.add(Short.class);
		boxedTypes.add(Integer.class);
		boxedTypes.add(Long.class);
		boxedTypes.add(Float.class);
		boxedTypes.add(Double.class);
	};

	protected boolean isBoxed(Class<?> type)
	{
		return boxedTypes.contains(type);
	}

	protected void serialize_class(Object obj, PrintWriter p, int level) 
	{
		Map<String,Object> map;
		IClassSerializer converter=classToDictRegistry.get(obj.getClass());
		if(null!=converter)
		{
			map = converter.convert(obj);
		}
		else
		{
			map=new HashMap<String,Object>();
			try {
				// note: don't use the java.bean api, because that is not available on Android.
				for(Method m: obj.getClass().getMethods()) {
					int modifiers = m.getModifiers();
					if((modifiers & Modifier.PUBLIC)!=0 && (modifiers & Modifier.STATIC)==0) {
						String methodname = m.getName();
						int prefixlen = 0;
						if(methodname.equals("getClass")) continue;
						if(methodname.startsWith("get")) prefixlen=3;
						else if(methodname.startsWith("is")) prefixlen=2;
						else continue;
						Object value = m.invoke(obj);
						String name = methodname.substring(prefixlen);
						if(name.length()==1) {
							name = name.toLowerCase();
						} else {
							if(!Character.isUpperCase(name.charAt(1))) {
								name = Character.toLowerCase(name.charAt(0)) + name.substring(1);
							}
						}
						map.put(name, value);
					}
				}
				if(this.packageInClassName)
					map.put("__class__", obj.getClass().getName());
				else
					map.put("__class__", obj.getClass().getSimpleName());
			} catch (IllegalAccessException e) {
				throw new IllegalArgumentException("couldn't introspect javabean: "+e);
			} catch (InvocationTargetException e) {
				throw new IllegalArgumentException("couldn't introspect javabean: "+e);
			}
		}
		serialize_dict(map, p, level);
	}

	protected void serialize_primitive(Object obj, PrintWriter p, int level) 
	{
		if(obj instanceof Boolean || obj.getClass()==Boolean.TYPE)
		{
			p.print(obj.equals(Boolean.TRUE)? "True": "False");
		}
		else
		{
			p.print(obj);
		}
	}

	protected void serialize_string(String str, PrintWriter p, int level)
	{
		// backslash-escaped string
		str = str.replace("\\", "\\\\");  // double-escape the backslashes
        str = str.replace("\b", "\\b");
        str = str.replace("\f", "\\f");
        str = str.replace("\n", "\\n");
        str = str.replace("\r", "\\r");
        str = str.replace("\t", "\\t");
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
		Map<String, Object> dict;
		IClassSerializer converter=classToDictRegistry.get(ex.getClass());
		if(null!=converter)
		{
			dict = converter.convert(ex);
		}
		else
		{
			dict = new HashMap<String,Object>();
			if(this.packageInClassName)
				dict.put("__class__", ex.getClass().getName());
			else
				dict.put("__class__", ex.getClass().getSimpleName());
			dict.put("__exception__", true);
			dict.put("args", new String[]{ex.getMessage()});
			dict.put("attributes", java.util.Collections.EMPTY_MAP);
		}
		serialize_dict(dict, p, level);
	}
}
