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
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Razorvine.Serpent
{
	/// <summary>
	/// Serialize an object tree to a byte stream.
	/// It is not thread-safe: make sure you're not making changes to the object tree that is being serialized.
	/// </summary>
	public class Serializer
	{
		public bool Indent;
		public bool SetLiterals;
		public bool NamespaceInClassName;
		private static IDictionary<Type, Func<object, IDictionary>> classToDictRegistry = new Dictionary<Type, Func<object, IDictionary>>();
		

		/// <summary>
		/// Initialize the serializer.
		/// </summary>
		/// <param name="indent">indent the output over multiple lines (default=false)</param>
		/// <param name="setLiterals">use set-literals or not (set to False if you need compatibility with Python < 3.2)</param>
		/// <param name="namespaceInClassName">include namespace prefix for class names or only use the class name itself</param>
		public Serializer(bool indent=false, bool setLiterals=true, bool namespaceInClassName=false)
		{
			this.Indent = indent;
			this.SetLiterals = setLiterals;
			this.NamespaceInClassName = namespaceInClassName;
		}
		
		/// <summary>
		/// Register a custom class-to-dict converter.
		/// </summary>
		public static void RegisterClass(Type clazz, Func<object, IDictionary> converter)
		{
			classToDictRegistry[clazz] = converter;
		}

		/// <summary>
		/// Serialize the object tree to bytes.
		/// </summary>
		public byte[] Serialize(object obj)
		{
			using(MemoryStream ms = new MemoryStream())
			using(TextWriter tw = new StreamWriter(ms, new UTF8Encoding(false)))			// don't write BOM
			{
				string header = "# serpent utf-8 ";
				if(this.SetLiterals)
					header += "python3.2\n";  //set-literals require python 3.2+ to deserialize (ast.literal_eval limitation)
				else
					header += "python2.6\n";
				tw.Write(header);
				Serialize(obj, tw, 0);
				tw.Flush();
				return ms.ToArray();
			}
		}
		
		protected void Serialize(object obj, TextWriter tw, int level)
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
			
			Type t = obj==null? null : obj.GetType();
			
			if(obj==null)
			{
				tw.Write("None");
			}
			else if(obj is string)
			{
				Serialize_string((string)obj, tw, level);
			}
			else if(t.IsPrimitive)
			{
				Serialize_primitive(obj, tw, level);
			}
			else if(obj is decimal)
			{
				Serialize_decimal((decimal)obj, tw, level);
			}
			else if(obj is Enum)
			{
				Serialize_string(obj.ToString(), tw, level);
			}
			else if(obj is IDictionary)
			{
				Serialize_dict((IDictionary)obj, tw, level);
			}
			else if(t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(HashSet<>)))
			{
				IEnumerable x = (IEnumerable) obj;
				ArrayList list = new ArrayList();
				foreach(var elt in x)
					list.Add(elt);
				object[] setvalues = list.ToArray();
				Serialize_set(setvalues, tw, level);
			}
			else if(obj is byte[])
			{
				Serialize_bytes((byte[])obj, tw, level);
			}
			else if(obj is Array)
			{
				Serialize_tuple((ICollection) obj, tw, level);
			}
			else if(obj is ICollection)
			{
				Serialize_list((ICollection) obj, tw, level);
			}
			else if(obj is DateTime)
			{
				Serialize_datetime((DateTime)obj, tw, level);
			}
			else if(obj is TimeSpan)
			{
				Serialize_timespan((TimeSpan)obj, tw, level);
			}
			else if(obj is Exception)
			{
				Serialize_exception((Exception)obj, tw, level);
			}
			else if(obj is Guid)
			{
				Serialize_guid((Guid) obj, tw, level);
			}
			else if(obj is ComplexNumber)
			{
				Serialize_complex((ComplexNumber) obj, tw, level);
			}
			else
			{
				Serialize_class(obj, tw, level);
			}
		}
			
			
		protected void Serialize_tuple(ICollection array, TextWriter tw, int level)
		{
			tw.Write("(");
			Serialize_sequence_elements(array, array.Count==1, tw, level+1);
			if(this.Indent && array.Count>0)
				tw.Write(string.Join("  ", new string[level+1]));
			tw.Write(")");
		}

		protected void Serialize_list(ICollection list, TextWriter tw, int level)
		{
			tw.Write("[");
			Serialize_sequence_elements(list, false, tw, level+1);
			if(this.Indent && list.Count>0)
				tw.Write(string.Join("  ", new string[level+1]));
			tw.Write("]");
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
			if(!this.SetLiterals)
			{
				// output a tuple instead of a set-literal
				Serialize_tuple(set, tw, level);
				return;
			}

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
				Serialize_sequence_elements(set, false, tw, level+1);
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
		
		protected void Serialize_sequence_elements(ICollection elements, bool trailingComma, TextWriter tw, int level)
		{
			if(elements.Count==0)
				return;
			int count=0;
			if(this.Indent)
			{
				tw.Write("\n");
				string innerindent = string.Join("  ", new string[level+1]);
				foreach(object e in elements)
				{
					tw.Write(innerindent);
					Serialize(e, tw, level);
					count++;
					if(count<elements.Count)
					{
						tw.Write(",\n");
					}
				}
				if(trailingComma)
					tw.Write(",");
				tw.Write("\n");
			}
			else
			{
				foreach(object e in elements)
				{
					Serialize(e, tw, level);
					count++;
					if(count<elements.Count)
						tw.Write(",");
				}
				if(trailingComma)
					tw.Write(",");
			}
		}
		
		protected void Serialize_bytes(byte[] data, TextWriter tw, int level)
		{
			// base-64 struct output
			string str = Convert.ToBase64String(data);
			var dict = new Hashtable() {
				{"data", str},	
				{"encoding", "base64"}
			};
			Serialize_dict(dict, tw, level);
		}
		
		protected void Serialize_string(string str, TextWriter tw, int level)
		{
			// backslash-escaped string
			str = str.Replace("\\", "\\\\");  // double-escape the backslashes
			str = str.Replace("\a", "\\a");
			str = str.Replace("\b", "\\b");
			str = str.Replace("\f", "\\f");
			str = str.Replace("\n", "\\n");
			str = str.Replace("\r", "\\r");
			str = str.Replace("\t", "\\t");
			str = str.Replace("\v", "\\v");
			if(!str.Contains("'"))
				str = "'" + str + "'";
			else if(!str.Contains("\""))
				str = '"' + str + '"';
			else
			{
				str = str.Replace("'", "\\'");
				str = "'" + str + "'";
			}
			tw.Write(str);
		}

		protected void Serialize_datetime(DateTime dt, TextWriter tw, int level)
		{
			if(dt.Millisecond>0)
				Serialize_string(dt.ToString("yyyy-MM-ddTHH:mm:ss.ffffff", CultureInfo.InvariantCulture), tw, level);
			else
				Serialize_string(dt.ToString("s"), tw, level);
		}

		protected void Serialize_timespan(TimeSpan span, TextWriter tw, int level)
		{
			Serialize_primitive(span.TotalSeconds, tw, level);
		}

		protected void Serialize_exception(Exception exc, TextWriter tw, int level)
		{
			IDictionary dict;
			Func<object, IDictionary> converter = null;
			classToDictRegistry.TryGetValue(exc.GetType(), out converter);
			
			if(converter!=null)
			{
				// build a custom property dict from the object.
				dict = converter(exc);
			}
			else
			{
				string className;
				if(this.NamespaceInClassName)
					className = exc.GetType().FullName;
				else
					className = exc.GetType().Name;
				dict = new Hashtable() {
					{"__class__", className},
					{"__exception__", true},
					{"args", new string[]{exc.Message} },
					{"attributes", exc.Data}
				};
			}
			Serialize_dict(dict, tw, level);
		}

		protected void Serialize_guid(Guid guid, TextWriter tw, int level)
		{
			// simple string representation of the guid
			Serialize_string(guid.ToString(), tw, level);
		}

		protected void Serialize_decimal(decimal dec, TextWriter tw, int level)
		{
			Serialize_string(dec.ToString(CultureInfo.InvariantCulture), tw, level);
		}

		protected void Serialize_primitive(object obj, TextWriter tw, int level)
		{
			tw.Write(Convert.ToString(obj, CultureInfo.InvariantCulture));
		}

		protected void Serialize_complex(ComplexNumber cplx, TextWriter tw, int level)
		{
			tw.Write("(");
			Serialize_primitive(cplx.Real, tw, level);
			if(cplx.Imaginary>=0)
				tw.Write("+");
			Serialize_primitive(cplx.Imaginary, tw, level);
			tw.Write("j)");
		}

		protected void Serialize_class(object obj, TextWriter tw, int level)
		{
			Type obj_type = obj.GetType();
			
			IDictionary dict;
			Func<object, IDictionary> converter = null;
			classToDictRegistry.TryGetValue(obj_type, out converter);
			
			if(converter!=null)
			{
				// build a custom property dict from the object.
				dict = converter(obj);
			}
			else
			{
				// if it is an anonymous class type, accept it.
				// any other class needs to have [Serializable] attribute
				bool isAnonymousClass = obj_type.Name.StartsWith("<>");
				if(!isAnonymousClass && !obj_type.IsSerializable)
				{
					throw new SerializationException("object of type "+obj_type.Name+" is not serializable");
				}
				
				dict = new Hashtable();
				if(!isAnonymousClass) {
					// only provide the class name when it is not an anonymous class
					if(this.NamespaceInClassName)
						dict["__class__"] = obj_type.FullName;
					else
						dict["__class__"] = obj_type.Name;
				}
				PropertyInfo[] properties=obj_type.GetProperties();
				foreach(var propinfo in properties) {
					if(propinfo.CanRead) {
						string name=propinfo.Name;
						try {
							dict[name]=propinfo.GetValue(obj, null);
						} catch (Exception x) {
							throw new SerializationException("cannot serialize a property:",x);
						}
					}
				}
			}
			
			Serialize_dict(dict, tw, level);
		}
	}
}
