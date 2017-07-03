/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent;

import java.io.IOException;
import java.io.Serializable;
import java.io.StringWriter;
import java.lang.reflect.Array;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.math.BigDecimal;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.*;
import java.util.Base64;
import java.util.Map.Entry;


/**
 * Serialize an object tree to a byte stream.
 * It is not thread-safe: make sure you're not making changes to the object tree that is being serialized.
 */
public class Serializer
{
	/**
	 * The maximum nesting level of the object graphs that you want to serialize.
	 * This limit has been set to avoid troublesome stack overflow errors.
	 * (If it is reached, an IllegalArgumentException is thrown instead with a clear message) 
	 */
	public int maximumLevel = 500;		// to avoid stack overflow errors
	
	/**
	 * Indent the resulting serpent serialization text?
	 */
	public boolean indent = false;
	
	/**
	 * Use set literals?
	 */
	public boolean setliterals = true;
	
	/**
	 * Include package name in class name, for classes that are serialized to dicts?
	 */
	public boolean packageInClassName = false;

	private static Map<Class<?>, IClassSerializer> classToDictRegistry = new HashMap<Class<?>, IClassSerializer>();
	
	/**
	 * Create a Serpent serializer with default options.
	 */
	public Serializer()
	{
	}
	
	/**
	 * Create a Serpent serializer with custom options.
	 * @param indent should the output be indented to make it more readable?
	 * @param setliterals should set literals be used (recommended if you use newer Python versions to parse this)
	 * @param packageInClassName should the package name be included with the class name for classes that are serialized to dict?
	 */
	public Serializer(boolean indent, boolean setliterals, boolean packageInClassName)
	{
		this.indent = indent;
		this.setliterals = setliterals;
		this.packageInClassName = packageInClassName;
	}
	
	/**
	 * Register a custom class serializer, if you want to tweak the serialization of classes that Serpent doesn't know about yet.
	 */
	public static void registerClass(Class<?> clazz, IClassSerializer converter)
	{
		classToDictRegistry.put(clazz, converter);
	} 
	
	/**
	 * Serialize an object graph to a serpent serialized form.
	 */
	public byte[] serialize(Object obj)
	{
		StringWriter sw = new StringWriter();

		if(this.setliterals)
			sw.write("# serpent utf-8 python3.2\n");  // set-literals require python 3.2+ to deserialize (ast.literal_eval limitation)
		else
			sw.write("# serpent utf-8 python2.6\n");
		serialize(obj, sw, 0);
		
		sw.flush();
		final String ser = sw.toString();
		try {
			sw.close();
			return ser.getBytes("utf-8");
		} catch (IOException x) {
			throw new IllegalArgumentException("error creating output bytes: "+x);
		}
	}
	
	protected void serialize(Object obj, StringWriter sw, int level)
	{
		if(level>maximumLevel)
			throw new IllegalArgumentException("Object graph nesting too deep. Increase serializer.maximumLevel if you think you need more.");

		if(obj!=null && obj.getClass().getName().startsWith("org.python."))
			obj = convertJythonObject(obj);

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
				serialize_bytes((byte[])obj, sw, level);
				return;
			}
			else
			{
				serialize_primitive_array(obj, sw, level);
			}
			return;
		}
		
		if(obj==null)
		{
			sw.write("None");
		}
		else if(obj instanceof String)
		{
			serialize_string((String)obj, sw, level);
		}
		else if(type.isPrimitive() || isBoxed(type))
		{
			serialize_primitive(obj, sw, level);
		}
		else if(obj instanceof Enum)
		{
			serialize_string(obj.toString(), sw, level);
		}
		else if(obj instanceof BigDecimal)
		{
			serialize_bigdecimal((BigDecimal)obj, sw, level);
		}
		else if(obj instanceof Number)
		{
			serialize_primitive(obj, sw, level);
		}
		else if(obj instanceof Date)
		{
			serialize_date((Date)obj, sw, level);
		}
		else if(obj instanceof Calendar)
		{
			serialize_calendar((Calendar)obj, sw, level);
		}
		else if(obj instanceof UUID)
		{
			serialize_uuid((UUID)obj, sw, level);
		}
		else if(obj instanceof Set<?>)
		{
			serialize_set((Set<?>)obj, sw, level);
		}
		else if(obj instanceof Map<?,?>)
		{
			serialize_dict((Map<?,?>)obj, sw, level);
		}
		else if(obj instanceof Collection<?>)
		{
			serialize_collection((Collection<?>)obj, sw, level);
		}
		else if(obj instanceof ComplexNumber)
		{
			serialize_complex((ComplexNumber)obj, sw, level);
		}
		else if(obj instanceof Exception)
		{
			serialize_exception((Exception)obj, sw, level);
		}
		else if(obj instanceof Serializable)
		{
			serialize_class(obj, sw, level);
		}
		else
		{
			throw new IllegalArgumentException("cannot serialize object of type "+type);
		}
	}
	
	/**
	 * When used from Jython directly, it sometimes passes some Jython specific
	 * classes to the serializer (such as org.python.core.PyComplex for a complex number).
	 * Due to the way these are constructed, Serpent is greatly confused, and will often
	 * end up in an endless loop eventually crashing with too deep nesting.
	 * For the know cases, we convert them here to appropriate representations.
	 */
	protected Object convertJythonObject(Object obj)
	{
		final Class<? extends Object> clazz = obj.getClass();
		final String classname = clazz.getName();
		
		try
		{
			// use reflection because I don't want to have a compiler dependency on Jython. 
			if(classname.equals("org.python.core.PyTuple")) {
				return clazz.getMethod("toArray").invoke(obj);
			}
			else if(classname.equals("org.python.core.PyComplex")) {
				Object pyImag = clazz.getMethod("getImag").invoke(obj);
				Object pyReal = clazz.getMethod("getReal").invoke(obj);
				Double imag = (Double) pyImag.getClass().getMethod("getValue").invoke(pyImag); 
				Double real = (Double) pyReal.getClass().getMethod("getValue").invoke(pyReal); 
				return new ComplexNumber(real, imag);
			}
			else if(classname.equals("org.python.core.PyByteArray")) {
				Object pyStr = clazz.getMethod("__str__").invoke(obj);
				return pyStr.getClass().getMethod("toBytes").invoke(pyStr);
			}
			else if(classname.equals("org.python.core.PyMemoryView")) {
				Object pyBytes = clazz.getMethod("tobytes").invoke(obj);
				return pyBytes.getClass().getMethod("toBytes").invoke(pyBytes);
			}
		} catch (ReflectiveOperationException e) {
			throw new IllegalArgumentException("cannot serialize Jython object of type "+clazz, e);
		} catch (IllegalArgumentException e) {
			throw new IllegalArgumentException("cannot serialize Jython object of type "+clazz, e);
		} catch (SecurityException e) {
			throw new IllegalArgumentException("cannot serialize Jython object of type "+clazz, e);
		}
		
		// instead of an endless nesting loop, report a proper exception
		throw new IllegalArgumentException("cannot serialize Jython object of type "+obj.getClass());
	}
	
	protected void serialize_collection(Collection<?> collection, StringWriter sw, int level)
	{
		// output a list
		sw.write("[");
		serialize_sequence_elements(collection, false, sw, level+1);
		if(this.indent && collection.size()>0)
		{
			for(int i=0; i<level; ++i)
				sw.write("  ");
		}
		sw.write("]");
	}

	protected void serialize_sequence_elements(Collection<?> elts, boolean trailingComma, StringWriter sw, int level)
	{
		if(elts.size()==0)
			return;
		int count=0;
		if(this.indent)
		{
			sw.write("\n");
			String innerindent = "";
			for(int i=0; i<level; ++i)
				innerindent += "  ";
			for(Object e: elts)
			{
				sw.write(innerindent);
				serialize(e, sw, level);
				count++;
				if(count<elts.size())
				{
					sw.write(",\n");
				}
			}
			if(trailingComma)
				sw.write(",");
			sw.write("\n");
		}
		else
		{
			for(Object e: elts)
			{
				serialize(e, sw, level);
				count++;
				if(count<elts.size())
					sw.write(",");
			}
			if(trailingComma)
				sw.write(",");
		}
	}

	protected void serialize_set(Set<?> set, StringWriter sw, int level)
	{
		if(!this.setliterals)
		{
			// output a tuple instead of a set-literal
			serialize_tuple(set, sw, level);
			return;
		}
		
		if(set.size()>0)
		{
			sw.write("{");
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
			serialize_sequence_elements(output, false, sw, level+1);
	
			if(this.indent)
			{
				for(int i=0; i<level; ++i)
					sw.write("  ");
			}
			sw.write("}");
		}
		else
		{
			// empty set literal doesn't exist, replace with empty tuple
			serialize_tuple(Collections.EMPTY_LIST, sw, level+1);
		}
	}

	protected void serialize_primitive_array(Object array, StringWriter sw, int level)
	{
		// output a tuple
		int length = Array.getLength(array);
		ArrayList<Object> items = new ArrayList<Object>(length);
		for(int i=0; i<length; ++i)
			items.add(Array.get(array, i));
		serialize_tuple(items, sw, level);
	}

	protected void serialize_tuple(Collection<?> items, StringWriter sw, int level)
	{
		sw.write("(");
		serialize_sequence_elements(items, items.size()==1, sw, level+1);
		if(this.indent && items.size()>0)
		{
			for(int i=0; i<level; ++i)
				sw.write("  ");
		}
		sw.write(")");
	}

	protected void serialize_bytes(byte[] obj, StringWriter sw, int level)
	{
		// base-64 struct output
		String str = Base64.getEncoder().encodeToString(obj);
		Map<String, String> dict = new HashMap<String, String>();
		dict.put("data", str);
		dict.put("encoding", "base64");
		serialize_dict(dict, sw, level);
	}

	protected void serialize_dict(Map<?, ?> dict, StringWriter sw,int level)
	{
		if(dict.size()==0)
		{
			sw.write("{}");
			return;
		}

		int counter=0;
		if(this.indent)
		{
			String innerindent = "  ";
			for(int i=0; i<level; ++i)
				innerindent += "  ";
			sw.write("{\n");
			
			// try to sort the dictionary keys
			Map<?,?> outputdict = dict;
			try {
				outputdict = new TreeMap<Object,Object>(dict);
			} catch (ClassCastException x) {
				// ignore unsortable keys
			}
			
			for(Map.Entry<?,?> e: outputdict.entrySet())
			{
				sw.write(innerindent);
				serialize(e.getKey(), sw, level+1);
				sw.write(": ");
				serialize(e.getValue(), sw, level+1);
				counter++;
				if(counter<dict.size())
					sw.write(",\n");
			}
			sw.write("\n");
			for(int i=0; i<level; ++i)
				sw.write("  ");
			sw.write("}");
		}
		else
		{
			sw.write("{");
			for(Map.Entry<?,?> e: dict.entrySet())
			{
				serialize(e.getKey(), sw, level+1);
				sw.write(":");
				serialize(e.getValue(), sw, level+1);
				counter++;
				if(counter<dict.size())
					sw.write(",");
			}
			sw.write("}");
		}
	}

	protected void serialize_calendar(Calendar cal, StringWriter sw, int level)
	{
		DateFormat df;
		String tzformat = "Z";

		if(cal.get(Calendar.ZONE_OFFSET)==0) {
			// UTC, GMT+0, output simple time zone string 'Z'
			tzformat = "'Z'";
		}
		
		if(cal.get(Calendar.MILLISECOND)>0) {
			// we have millis
			df = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS"+tzformat);
		} else {
			// no millis
			df = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss"+tzformat);
		}
		df.setTimeZone(cal.getTimeZone());
		serialize_string(df.format(cal.getTime()), sw, level);
	}

	protected void serialize_date(Date date, StringWriter sw, int level)
	{
		DateFormat df;
		
		if((date.getTime() % 1000) != 0) {
			// we have millis
			df = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'");
		} else {
			// no millis
			df = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss'Z'");
		}
		df.setTimeZone(TimeZone.getTimeZone("UTC"));
		serialize_string(df.format(date), sw, level);
	}

	protected void serialize_complex(ComplexNumber cplx, StringWriter sw, int level)
	{
		sw.write("(");
		serialize_primitive(cplx.real, sw, level);
		if(cplx.imaginary>=0)
			sw.write("+");
		serialize_primitive(cplx.imaginary, sw, level);
		sw.write("j)");
	}

	protected void serialize_uuid(UUID obj, StringWriter sw, int level)
	{
		serialize_string(obj.toString(), sw, level);
	}

	protected void serialize_bigdecimal(BigDecimal decimal, StringWriter sw, int level)
	{
		serialize_string(decimal.toEngineeringString(), sw, level);
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

	protected void serialize_class(Object obj, StringWriter sw, int level) 
	{
		Map<String,Object> map;
		IClassSerializer converter=getCustomConverter(obj.getClass());
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
		serialize_dict(map, sw, level);
	}

	protected IClassSerializer getCustomConverter(Class<?> type) {
		IClassSerializer converter = classToDictRegistry.get(type.getClass());
		if(converter!=null) {
			return converter; // exact match
		}
		
		// check if there's a custom pickler registered for an interface or abstract base class
		// that this object implements or inherits from.
		for(Entry<Class<?>, IClassSerializer> x: classToDictRegistry.entrySet()) {
			if(x.getKey().isAssignableFrom(type)) {
				return x.getValue();
			}
		}
		
		return null;
	}

	protected void serialize_primitive(Object obj, StringWriter sw, int level) 
	{
		if(obj instanceof Boolean || obj.getClass()==Boolean.TYPE)
		{
			sw.write(obj.equals(Boolean.TRUE)? "True": "False");
		}
		else if (obj instanceof Float || obj.getClass()==Float.TYPE)
		{
			Float f = (Float)obj;
			serialize_primitive(f.doubleValue(), sw, level);
		}
		else if (obj instanceof Double || obj.getClass()==Double.TYPE)
		{
			Double d = (Double) obj;
			if(d.isInfinite()) {
				// output a literal expression that overflows the float and results in +/-INF
				if(d>0.0) {
					sw.write("1e30000");
				} else {
					sw.write("-1e30000");
				}
			}
			else if(d.isNaN()) {
				// there's no literal expression for a float NaN...
				sw.write("{'__class__':'float','value':'nan'}");
			} else {
				sw.write(d.toString());
			}
		}
		else
		{
			sw.write(obj.toString());
		}
	}
	
	// the repr translation table for characters 0x00-0xff
	private final static String[] repr_255;
	static {
		repr_255=new String[256];
		for(int c=0; c<32; ++c) {
			repr_255[c] = String.format("\\x%02x",c);
		}
		for(char c=0x20; c<0x7f; ++c) {
			repr_255[c] = String.valueOf(c);
		}
		for(int c=0x7f; c<=0xa0; ++c) {
			repr_255[c] = String.format("\\x%02x", c);
		}
		for(char c=0xa1; c<=0xff; ++c) {
			repr_255[c] = String.valueOf(c);
		}
		// odd ones out:
		repr_255['\t'] = "\\t";
		repr_255['\n'] = "\\n";
		repr_255['\r'] = "\\r";
		repr_255['\\'] = "\\\\";
		repr_255[0xad] = "\\xad";
	}

	protected void serialize_string(String str, StringWriter sw, int level)
	{
		// create a 'repr' string representation following the same escaping rules as python 3.x repr() does.
		StringBuilder b=new StringBuilder(str.length()*2);
		boolean containsSingleQuote=false;
		boolean containsQuote=false;
		for(int i=0; i<str.length(); ++i)
		{
			final char c = str.charAt(i);
			containsSingleQuote |= c=='\'';
			containsQuote |= c=='"';
			
			if(c<256) {
				// characters 0..255 via quick lookup table
				b.append(repr_255[c]);
			} else {
				if(Character.isDefined(c) && !Character.isISOControl(c) && !Character.isSurrogate(c) && !Character.isWhitespace(c)) {
					b.append(c);
				} else {
					b.append(String.format("\\u%04x", (int)c));
				}
			}
		}

		if(!containsSingleQuote) {
			b.insert(0, '\'');
			b.append('\'');
			sw.write(b.toString());
		} else if (!containsQuote) {
			b.insert(0, '"');
			b.append('"');
			sw.write(b.toString());
		} else {
			String str2 = b.toString();
        	str2 = str2.replace("'", "\\'");
        	sw.write("'");
        	sw.write(str2);
        	sw.write("'");
		}
	}
	
	protected void serialize_exception(Exception ex, StringWriter sw, int level)
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
		serialize_dict(dict, sw, level);
	}
}
