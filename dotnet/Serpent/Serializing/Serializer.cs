/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright 2013, Irmen de Jong (irmen@razorvine.net)
/// This code is open-source, but licensed under the "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
			using(TextWriter tw = new StreamWriter(ms, Encoding.UTF8))
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
			// hashtables/dictionaries -> dict
			// hashset -> set
			// array -> tuple
			// byte arrays --> base64
			// any other enumerable --> list
			// date/timespan/uuid/exception -> custom mapping
			// random class --> public properties to dict
			// primitive types --> simple mapping
			if(obj is string)
			{
				Serialize_string((string)obj, tw, level);
			}
			else if(obj.GetType().IsPrimitive)
			{
				Serialize_primitive(obj, tw, level);
			}
			else if(obj is decimal)
			{
				Serialize_decimal((decimal)obj, tw, level);
			}
			else if(obj is System.Collections.IDictionary)
			{
				Serialize_dict((System.Collections.IDictionary)obj, tw, level);
			}
			else if(obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition().Equals(typeof(HashSet<>)))
			{
				Serialize_set((System.Collections.IEnumerable) obj, tw, level);
			}
			else if(obj is byte[])
			{
				Serialize_bytes((byte[])obj, tw, level);
			}
			else if(obj is Array)
			{
				Serialize_tuple((System.Collections.IEnumerable) obj, tw, level);
			}
			else if(obj is System.Collections.IEnumerable)
			{
				Serialize_list((System.Collections.IEnumerable) obj, tw, level);
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
			else
			{
				Serialize_class(obj, tw, level);
			}
		}
			
			
		protected void Serialize_tuple(System.Collections.IEnumerable array, TextWriter tw, int level)
		{
			
		}

		protected void Serialize_list(System.Collections.IEnumerable list, TextWriter tw, int level)
		{
			
		}

		protected void Serialize_dict(System.Collections.IDictionary dict, TextWriter tw, int level)
		{
			
		}
		
		protected void Serialize_set(System.Collections.IEnumerable elements, TextWriter tw, int level)
		{
			
		}
		
		protected void Serialize_bytes(byte[] data, TextWriter tw, int level)
		{
			// base-64 struct output
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
			// iso datetime notation 
		}

		protected void Serialize_timespan(TimeSpan span, TextWriter tw, int level)
		{
			// total seconds
		}

		protected void Serialize_exception(Exception exc, TextWriter tw, int level)
		{
			// struct 
		}

		protected void Serialize_guid(Guid guid, TextWriter tw, int level)
		{
			// simple string representation of the guid
			tw.Write("'"+guid.ToString()+"'");
		}

		protected void Serialize_decimal(decimal dec, TextWriter tw, int level)
		{
			tw.Write(dec.ToString(CultureInfo.InvariantCulture));
		}

		protected void Serialize_primitive(object obj, TextWriter tw, int level)
		{
			tw.Write(Convert.ToString(obj, CultureInfo.InvariantCulture));
		}

		protected void Serialize_class(object obj, TextWriter tw, int level)
		{
			// only if class has serializableattribute (or implements iserializable)
			// do something special when it implements iserializable.
		}
	}
}
