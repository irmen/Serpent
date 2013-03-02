/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright 2013, Irmen de Jong (irmen@razorvine.net)
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

namespace Razorvine.Serpent.Serializing
{
	/// <summary>
	/// Serialize an object tree to a byte stream.
    /// It is not thread-safe: make sure you're not making changes to the object tree that is being serialized.
	/// </summary>
	public class Serializer
	{
		public bool Indent;
		
		/// <summary>
		/// Initialize the serializer.
		/// </summary>
		/// <param name="indent">indent the output over multiple lines (default=false)</param>
		public Serializer(bool indent=false)
		{
			this.Indent = indent;
		}

		/// <summary>
		/// Serialize the object tree to bytes.
		/// </summary>
		public byte[] Serialize(object obj)
		{
			using(MemoryStream ms = new MemoryStream())
			using(TextWriter tw = new StreamWriter(ms, new UTF8Encoding(false)))			// don't write BOM
			{
				string header = string.Format("# serpent utf-8 dotnet-cli{0}\n", Environment.Version.ToString(2));
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
			else if(obj is IDictionary)
			{
				Serialize_dict((IDictionary)obj, tw, level);
			}
			else if(t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(HashSet<>)))
			{
				Serialize_set((ICollection) obj, tw, level);
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
			Serialize_sequence_elements(array, tw, level+1);
			if(array.Count==1)
			{
				// tuple with 1 elements need special treatment (require a trailing comma)
				tw.Write(",");
			}
			tw.Write(")");
		}

		protected void Serialize_list(ICollection list, TextWriter tw, int level)
		{
			tw.Write("[");
			Serialize_sequence_elements(list, tw, level+1);
			tw.Write("]");
		}

		protected void Serialize_dict(IDictionary dict, TextWriter tw, int level)
		{
			tw.Write("{");
			foreach(DictionaryEntry x in dict)
			{
				Serialize(x.Key, tw, level);
				tw.Write(":");
				Serialize(x.Value, tw, level);
				tw.Write(",");
			}
			tw.Write("}");
		}
		
		protected void Serialize_set(ICollection set, TextWriter tw, int level)
		{
			if(set.Count>0)
			{
				tw.Write("{");
				Serialize_sequence_elements(set, tw, level+1);
				tw.Write("}");
			}
			else
			{
				// empty set literal doesn't exist, replace with empty tuple
				Serialize_tuple(new object[0], tw, level+1);
			}
		}
		
		protected void Serialize_sequence_elements(ICollection elements, TextWriter tw, int level)
		{
			foreach(object e in elements)
			{
				Serialize(e, tw, level);
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
			Serialize_string(dt.ToString("s"), tw, level);
		}

		protected void Serialize_timespan(TimeSpan span, TextWriter tw, int level)
		{
			Serialize_primitive(span.TotalSeconds, tw, level);
		}

		protected void Serialize_exception(Exception exc, TextWriter tw, int level)
		{
			var dict = new Hashtable() {
				{"__class__", exc.GetType().Name},
				{"__exception__", true},
				{"args", null},
				{"message", exc.Message}
			};
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
			// only if class has serializableattribute
			if(!obj.GetType().IsSerializable)
			{
				throw new SerializationException("object of type "+obj.GetType().Name+" is not serializable");
			}
			
			var dict = new Hashtable();
			dict["__class__"] = obj.GetType().Name;
			PropertyInfo[] properties=obj.GetType().GetProperties();
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
			
			Serialize_dict(dict, tw, level);
		}
	}
}
