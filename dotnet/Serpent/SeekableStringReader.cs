using System;
using System.Collections.Generic;
using System.IO;

namespace Razorvine.Serpent
{
	public class SeekableStringReader : IDisposable
	{
		private string str;
		private int cursor = 0;
		private int bookmark = -1;
	
		
		public class Error: Exception
		{
			public Error(string message) : base(message)
			{
			}
		}

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
				throw new Error("use Rewind to seek back");
			int safecount = Math.Min(count, str.Length-cursor);
			if(safecount==0 && count>0)
				throw new Error("no more data");
			
			string result = str.Substring(cursor, safecount);
			cursor += safecount;
			return result;
		}
		
		/// <summary>
		/// Read everything until the sentinel.
		/// Sentinel char is read but not returned in the result.
		/// </summary>
		public string ReadUntil(char sentinel)
		{
			int index = str.IndexOf(sentinel, cursor);
			if(index>=0)
			{
				string result = str.Substring(cursor, index-cursor);
				cursor = index+1;
				return result;
			}
			throw new Error("sentinel not found");
		}
		
		/// <summary>
		/// Returns the rest of the data until the end.
		/// </summary>
		public string Rest()
		{
			if(cursor>=str.Length)
				throw new Error("no more data");
			string result=str.Substring(cursor);
			cursor = str.Length;
			return result;
		}
		
		/// <summary>
		/// Rewind a number of characters.
		/// </summary>
		public void Rewind(int count=1)
		{
			cursor = Math.Max(0, cursor-count);
		}

		/// <summary>
		/// Set a bookmark to rewind to later.
		/// </summary>
		public void Bookmark()
		{
			bookmark = cursor;
		}
		
		/// <summary>
		/// Flip back to previously set bookmark.
		/// </summary>
		public void FlipBack()
		{
			if(bookmark>=0)
				cursor = bookmark;
			else
				throw new Error("no bookmark set");
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

