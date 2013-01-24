using System;
using System.Collections.Generic;

namespace Razorvine.Serpent
{
	public class SeekableStringReader : IDisposable
	{
		private string str;
		private int cursor = 0;
		private int bookmark = -1;
	
		public SeekableStringReader(string str)
		{
			if(str==null)
				throw new ArgumentNullException("str");
	
			this.str = str;	
		}
		
		/// <summary>
		/// Make a nested reader with its own cursor and bookmark.
		/// The cursor starts at the same position as the parent.
		/// </summary>
		/// <param name="parent"></param>
		public SeekableStringReader(SeekableStringReader parent)
		{
			str = parent.str;
			cursor = parent.cursor;
		}

		/// <summary>
		/// Is there more to read?
		/// </summary>
		public bool HasMore()
		{
			return cursor<str.Length;
		}
		
		/// <summary>
		/// What is the next character?
		/// </summary>
		public char Peek()
		{
			return str[cursor];
		}

		/// <summary>
		/// What are the next characters that will be read?
		/// </summary>
		public string Peek(int count)
		{
			return str.Substring(cursor, Math.Min(count, str.Length-cursor));
		}

		/// <summary>
		/// Read a single character.
		/// </summary>
		public char Read()
		{
			return str[cursor++];
		}
		
		/// <summary>
		/// Read a number of characters.
		/// </summary>
		public string Read(int count)
		{
			if(count<0)
				throw new ParseException("use Rewind to seek back");
			int safecount = Math.Min(count, str.Length-cursor);
			if(safecount==0 && count>0)
				throw new ParseException("no more data");
			
			string result = str.Substring(cursor, safecount);
			cursor += safecount;
			return result;
		}
		
		/// <summary>
		/// Read everything until one of the sentinel(s), which must exist in the string.
		/// Sentinel char is read but not returned in the result.
		/// </summary>
		public string ReadUntil(params char[] sentinels)
		{
			int index = str.IndexOfAny(sentinels, cursor);
			if(index>=0)
			{
				string result = str.Substring(cursor, index-cursor);
				cursor = index+1;
				return result;
			}
			throw new ParseException("terminator not found");
		}
		
		/// <summary>
		/// Read everything as long as the char occurs in the accepted characters.
		/// </summary>
		/// <param name="characters"></param>
		/// <returns></returns>
		public string ReadWhile(params char[] accepted)
		{
			int start = cursor;
			while(cursor < str.Length)
			{
				if(Array.IndexOf(accepted, str[cursor])>=0)
					++cursor;
				else
					break;
			}
			return str.Substring(start, cursor-start);
		}
		
		/// <summary>
		/// Read away any whitespace.
		/// </summary>
		public void SkipWhitespace()
		{
			while(HasMore())
			{
				char c=Read();
				if(!Char.IsWhiteSpace(c))
				{
					Rewind(1);
					return;
				}
			}
		}

		/// <summary>
		/// Returns the rest of the data until the end.
		/// </summary>
		public string Rest()
		{
			if(cursor>=str.Length)
				throw new ParseException("no more data");
			string result=str.Substring(cursor);
			cursor = str.Length;
			return result;
		}
		
		/// <summary>
		/// Rewind a number of characters.
		/// </summary>
		public void Rewind(int count)
		{
			cursor = Math.Max(0, cursor-count);
		}

		/// <summary>
		/// Return a bookmark to rewind to later.
		/// </summary>
		public int Bookmark()
		{
			return cursor;
		}
		
		/// <summary>
		/// Flip back to previously set bookmark.
		/// </summary>
		public void FlipBack(int bookmark)
		{
			cursor = bookmark;
		}
		
		/// <summary>
		/// Sync the position and bookmark with the current position in another reader.
		/// </summary>
		public void Sync(SeekableStringReader inner)
		{
			bookmark = inner.bookmark;
			cursor = inner.cursor;
		}
	
		public void Dispose()
		{
			this.str = null;
		}
	}
}

