﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Razorvine.Serpent
{
	/// <summary>
	/// Serialize an object tree to a byte stream.
	/// It is not thread-safe: make sure you're not making changes to the object tree that is being serialized.
	/// </summary>
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedParameter.Global")]
	[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
	[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
	public class Serializer
	{
		/// <summary>
		/// indent output?
		/// </summary>
		public bool Indent;
		
		/// <summary>
		/// include namespace prefix for classes that are serialized to dict?
		/// </summary>
		public bool NamespaceInClassName;

		/// <summary>
		/// Use bytes literal representation instead of base-64 encoding?
		/// </summary>
		public bool BytesRepr;
		
		
		/// <summary>
		/// The maximum nesting level of the object graphs that you want to serialize.
		/// This limit has been set to avoid troublesome stack overflow errors.
		/// (If it is reached, an ArgumentException is thrown instead with a clear message) 
		/// </summary>
		public int MaximumLevel = 500;     // avoids stackoverflow errors
		
		private static readonly IDictionary<Type, Func<object, IDictionary>> ClassToDictRegistry 
			= new Dictionary<Type, Func<object, IDictionary>>();
		

		/// <summary>
		/// Initialize the serializer.
		/// </summary>
		/// <param name="indent">indent the output over multiple lines (default=false)</param>
		/// <param name="namespaceInClassName">include namespace prefix for class names or only use the class name itself</param>
		/// <param name="bytesRepr">use bytes literal representation instead of base-64 encoding for bytes types? (default=false)</param>
		public Serializer(bool indent=false, bool namespaceInClassName=false, bool bytesRepr=false)
		{
			Indent = indent;
			NamespaceInClassName = namespaceInClassName;
			BytesRepr = bytesRepr;
		}
		
		/// <summary>
		/// Register a custom class-to-dict converter.
		/// </summary>
		public static void RegisterClass(Type clazz, Func<object, IDictionary> converter)
		{
			ClassToDictRegistry[clazz] = converter;
		}

		/// <summary>
		/// Serialize the object tree to bytes.
		/// </summary>
		public byte[] Serialize(object obj)
		{
			using var tw = new StringWriter();
			tw.Write("# serpent utf-8 python3.2\n");
			Serialize(obj, tw, 0);
			tw.Flush();
			return Encoding.UTF8.GetBytes(tw.ToString());
		}
		
		protected void Serialize(object obj, TextWriter tw, int level)
		{
			if(level>MaximumLevel)
				throw new ArgumentException("Object graph nesting too deep. Increase serializer.MaximumLevel if you think you need more.");

			// null -> None
			// hashtables/dictionaries -> dict
			// hashset -> set
			// array -> tuple
			// byte arrays --> base64
			// any other icollection --> list
			// date/timespan/uuid/exception -> custom mapping
			// random class --> public properties to dict
			// primitive types --> simple mapping
			
			var t = obj?.GetType();
			
			switch (obj)
			{
				case null:
					tw.Write("None");
					break;
				case string str:
					Serialize_string(str, tw, level);
					break;
				default:
				{
					if(t.IsPrimitive)
					{
						Serialize_primitive(obj, tw, level);
					}
					else switch (obj)
					{
						case decimal dec:
							Serialize_decimal(dec, tw, level);
							break;
						case Enum:
							Serialize_string(obj.ToString(), tw, level);
							break;
						case IDictionary dict:
							Serialize_dict(dict, tw, level);
							break;
						default:
						{
							if(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>))
							{
								var x = (IEnumerable) obj;
								object[] setValues = x.Cast<object>().ToArray();
								Serialize_set(setValues, tw, level);
							}
							else switch (obj)
							{
								case byte[] b:
									Serialize_bytes(b, tw, level);
									break;
								case Array a:
									Serialize_tuple(a, tw, level);
									break;
								case ICollection c:
									Serialize_list(c, tw, level);
									break;
								case DateTimeOffset dto:
									Serialize_datetimeoffset(dto, tw, level);
									break;
								case DateTime dt:
									Serialize_datetime(dt, tw, level);
									break;
								case TimeSpan ts:
									Serialize_timespan(ts, tw, level);
									break;
								case Exception ex:
									Serialize_exception(ex, tw, level);
									break;
								case Guid guid:
									Serialize_guid(guid, tw, level);
									break;
								case ComplexNumber cn:
									Serialize_complex(cn, tw, level);
									break;
								default:
									Serialize_class(obj, tw, level);
									break;
							}

							break;
						}
					}

					break;
				}
			}
		}
			
			
		protected void Serialize_tuple(ICollection array, TextWriter tw, int level)
		{
			tw.Write("(");
			Serialize_sequence_elements(array, array.Count==1, tw, level+1);
			if(Indent && array.Count>0)
				tw.Write(string.Join("  ", new string[level+1]));
			tw.Write(")");
		}

		protected void Serialize_list(ICollection list, TextWriter tw, int level)
		{
			tw.Write("[");
			Serialize_sequence_elements(list, false, tw, level+1);
			if(Indent && list.Count>0)
				tw.Write(string.Join("  ", new string[level+1]));
			tw.Write("]");
		}
		
		protected int DictentryCompare(DictionaryEntry d1, DictionaryEntry d2)
		{
			var c1 = d1.Key as IComparable;
			var c2 = d2.Key as IComparable;
			
			return c1?.CompareTo(c2) ?? 0;
		}

		protected void Serialize_dict(IDictionary dict, TextWriter tw, int level)
		{
			if(dict.Count==0)
			{
				tw.Write("{}");
				return;
			}
			int counter=0;
			if(Indent)
			{
				string innerIndent = string.Join("  ", new string[level+2]);
				tw.Write("{\n");
				var entries = new DictionaryEntry[dict.Count];
				dict.CopyTo(entries, 0);
				try {
					Array.Sort(entries, DictentryCompare);
				} catch (InvalidOperationException) {
					// ignore sorting of incomparable elements
				} catch (ArgumentException) {
					// ignore sorting of incomparable elements
				}
				foreach(var x in entries)
				{
					tw.Write(innerIndent);
					Serialize(x.Key, tw, level+1);
					tw.Write(": ");
					Serialize(x.Value, tw, level+1);
					counter++;
					if(counter<dict.Count)
						tw.Write(",\n");
				}
				tw.Write("\n");
				tw.Write(string.Join("  ", new string[level+1]));
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
			}

			tw.Write("}");
		}
		
		protected void Serialize_set(object[] set, TextWriter tw, int level)
		{
			if(set.Length>0)
			{
				tw.Write("{");
				if(Indent)
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
				if(Indent)
					tw.Write(string.Join("  ", new string[level+1]));
				tw.Write("}");
			}
			else
			{
				// empty set literal doesn't exist, replace with empty tuple
				Serialize_tuple(Array.Empty<object>(), tw, level+1);
			}
		}
		
		protected void Serialize_sequence_elements(ICollection elements, bool trailingComma, TextWriter tw, int level)
		{
			if(elements.Count==0)
				return;
			int count=0;
			if(Indent)
			{
				tw.Write("\n");
				string innerIndent = string.Join("  ", new string[level+1]);
				foreach(object e in elements)
				{
					tw.Write(innerIndent);
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
			if (BytesRepr)
			{
				// create a 'repr' bytes representation following the same escaping rules as python 3.x repr() does.
				var b=new StringBuilder(data.Length*2);
				bool containsSingleQuote=false;
				bool containsQuote=false;
				foreach(byte bb in data)
				{
					containsSingleQuote |= bb=='\'';
					containsQuote |= bb=='"';
				
					b.Append(BytesRepr255[bb]);
				}
				HandleQuotes(tw, containsSingleQuote, b, containsQuote, true);
			}
			else
			{
				// base-64 struct output
				string str = Convert.ToBase64String(data);
				var dict = new Dictionary<string, string>
				{
					{"data", str},
					{"encoding", "base64"}
				};
				Serialize_dict(dict, tw, level);
			}
		}

		private static void HandleQuotes(TextWriter tw, bool containsSingleQuote, StringBuilder b, bool containsQuote, bool isBytes)
		{
			if (!containsSingleQuote)
			{
				b.Insert(0, '\'');
				b.Append('\'');
				if(isBytes)
					tw.Write('b');
				tw.Write(b.ToString());
			}
			else if (!containsQuote)
			{
				b.Insert(0, '"');
				b.Append('"');
				if(isBytes)
					tw.Write('b');
				tw.Write(b.ToString());
			}
			else
			{
				string str2 = b.ToString();
				str2 = str2.Replace("'", "\\'");
				if(isBytes)
					tw.Write('b');
				tw.Write("'");
				tw.Write(str2);
				tw.Write("'");
			}
		}


		// the repr translation table for characters 0x00-0xff
		private static readonly string[] Repr255;
		private static readonly string[] BytesRepr255;
		static Serializer() {
			Repr255=new string[256];
			BytesRepr255=new string[256];
			for(int c=0; c<32; ++c) {
				Repr255[c] = "\\x"+c.ToString("x2");
				BytesRepr255[c] = "\\x"+c.ToString("x2");
			}
			for(int c=0x20; c<0x7f; ++c) {
				Repr255[c] = Convert.ToString((char)c);
				BytesRepr255[c] = Convert.ToString((char)c);
			}
			for(int c=0x7f; c<=0xa0; ++c) {
				Repr255[c] = "\\x"+c.ToString("x2");
			}
			for(int c=0xa1; c<=0xff; ++c) {
				Repr255[c] = Convert.ToString((char)c);
			}
			for(int c=0x7f; c<=0xff; ++c) {
				BytesRepr255[c] = "\\x"+c.ToString("x2");
			}
			// odd ones out:
			Repr255['\t'] = "\\t";
			Repr255['\n'] = "\\n";
			Repr255['\r'] = "\\r";
			Repr255['\\'] = @"\\";
			Repr255[0xad] = "\\xad";
			BytesRepr255['\t'] = "\\t";
			BytesRepr255['\n'] = "\\n";
			BytesRepr255['\r'] = "\\r";
			BytesRepr255['\\'] = @"\\";
		}
	
		// ReSharper disable once UnusedParameter.Global
		// ReSharper disable once MemberCanBePrivate.Global
		protected void Serialize_string(string str, TextWriter tw, int level)
		{
			// create a 'repr' string representation following the same escaping rules as python 3.x repr() does.
			var b=new StringBuilder(str.Length*2);
			bool containsSingleQuote=false;
			bool containsQuote=false;
			foreach(char c in str)
			{
				containsSingleQuote |= c=='\'';
				containsQuote |= c=='"';
				
				if(c<256) {
					// characters 0..255 via quick lookup table
					b.Append(Repr255[c]);
				} else {
					if(char.IsLetterOrDigit(c) || char.IsNumber(c) || char.IsPunctuation(c) || char.IsSymbol(c)) {
						b.Append(c);
					} else {
						b.Append("\\u");
						b.Append(((int)c).ToString("x4"));
					}
				}
			}
			HandleQuotes(tw, containsSingleQuote, b, containsQuote, false);
		}

		protected void Serialize_datetime(DateTime dt, TextWriter tw, int level)
		{
			string s = dt.Millisecond == 0 ? XmlConvert.ToString(dt, "yyyy-MM-ddTHH:mm:ss") : XmlConvert.ToString(dt, "yyyy-MM-ddTHH:mm:ss.fff");
			Serialize_string(s, tw, level);
		}

		protected void Serialize_datetimeoffset(DateTimeOffset dto, TextWriter tw, int level)
		{
			string s = XmlConvert.ToString(dto);
			Serialize_string(s, tw, level);
		}

		protected void Serialize_timespan(TimeSpan span, TextWriter tw, int level)
		{
			Serialize_primitive(span.TotalSeconds, tw, level);
		}

		protected void Serialize_exception(Exception exc, TextWriter tw, int level)
		{
			IDictionary dict;
			ClassToDictRegistry.TryGetValue(exc.GetType(), out var converter);
			
			if(converter!=null)
			{
				// build a custom property dict from the object.
				dict = converter(exc);
			}
			else
			{
				string className = NamespaceInClassName ? exc.GetType().FullName : exc.GetType().Name;
				dict = new Dictionary<string, object> {
					{"__class__", className},
					{"__exception__", true},
					{"args", new []{exc.Message} },
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
			switch (obj)
			{
				case float f:
				{
					double d = f;
					// ReSharper disable once TailRecursiveCall
					Serialize_primitive(d, tw, level);
					break;
				}
				case double d when double.IsPositiveInfinity(d):
					// output a literal expression that overflows the float and results in +/-INF
					tw.Write("1e30000");
					break;
				case double d when double.IsNegativeInfinity(d):
					tw.Write("-1e30000");
					break;
				case double.NaN:
					// there's no literal expression for a float NaN...
					tw.Write("{'__class__':'float','value':'nan'}");
					break;
				case double d:
					tw.Write(Convert.ToString(d, CultureInfo.InvariantCulture));
					break;
				default:
					tw.Write(Convert.ToString(obj, CultureInfo.InvariantCulture));
					break;
			}
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
			var objType = obj.GetType();
			
			IDictionary dict;
			var converter = GetCustomConverter(objType);
			
			if(converter!=null)
			{
				// build a custom property dict from the object.
				dict = converter(obj);
			}
			else
			{
				bool isAnonymousClass = objType.Name.StartsWith("<>");
				dict = new Dictionary<string,object>();
				if(!isAnonymousClass) {
					// only provide the class name when it is not an anonymous class
					if(NamespaceInClassName)
						dict["__class__"] = objType.FullName;
					else
						dict["__class__"] = objType.Name;
				}
				var properties=objType.GetProperties();
				foreach(var propinfo in properties) {
					if (!propinfo.CanRead) continue;
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

		// ReSharper disable once MemberCanBePrivate.Global
		// ReSharper disable once MemberCanBeMadeStatic.Global
		// ReSharper disable once InconsistentNaming
		protected Func<object, IDictionary> GetCustomConverter(Type obj_type)
		{
			if(ClassToDictRegistry.TryGetValue(obj_type, out var converter))
				return converter;  // exact match
			
			// check if there's a custom converter registered for an interface or abstract base class
			// that this object implements or inherits from.

			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach(var x in ClassToDictRegistry) {
				if(x.Key.IsAssignableFrom(obj_type)) {
					return x.Value;
				}
			}
			
			return null;
		}
	}
}
