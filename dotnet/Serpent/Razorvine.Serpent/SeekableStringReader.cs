using System;

namespace Razorvine.Serpent
{
	/// <summary>
	/// A special string reader that is suitable for the parser to read through
	/// the expression string. You can rewind it, set bookmarks to flip back to, etc.
	/// </summary>
	public class SeekableStringReader : IDisposable
	{
		// ReSharper disable RedundantDefaultMemberInitializer
		private string _str;
		private int _cursor = 0;
		private int _bookmark = -1;
	
		public SeekableStringReader(string str)
		{
			if(str==null)
				throw new ArgumentNullException(nameof(str));
	
			_str = str;	
		}
		
		/// <summary>
		/// Make a nested reader with its own cursor and bookmark.
		/// The cursor starts at the same position as the parent.
		/// </summary>
		/// <param name="parent"></param>
		public SeekableStringReader(SeekableStringReader parent)
		{
			_str = parent._str;
			_cursor = parent._cursor;
		}

		/// <summary>
		/// Is there more to read?
		/// </summary>
		public bool HasMore()
		{
			return _cursor<_str.Length;
		}
		
		/// <summary>
		/// What is the next character?
		/// </summary>
		public char Peek()
		{
			return _str[_cursor];
		}

		/// <summary>
		/// What are the next characters that will be read?
		/// </summary>
		public string Peek(int count)
		{
			return _str.Substring(_cursor, Math.Min(count, _str.Length-_cursor));
		}

		/// <summary>
		/// Read a single character.
		/// </summary>
		public char Read()
		{
			return _str[_cursor++];
		}
		
		/// <summary>
		/// Read a number of characters.
		/// </summary>
		public string Read(int count)
		{
			if(count<0)
				throw new ParseException("use Rewind to seek back");
			int safecount = Math.Min(count, _str.Length-_cursor);
			if(safecount==0 && count>0)
				throw new ParseException("no more data");
			
			string result = _str.Substring(_cursor, safecount);
			_cursor += safecount;
			return result;
		}
		
		/// <summary>
		/// Read everything until one of the sentinel(s), which must exist in the string.
		/// Sentinel char is read but not returned in the result.
		/// </summary>
		public string ReadUntil(params char[] sentinels)
		{
			int index = _str.IndexOfAny(sentinels, _cursor);
			if (index < 0) throw new ParseException("terminator not found");
			string result = _str.Substring(_cursor, index-_cursor);
			_cursor = index+1;
			return result;
		}
		
		/// <summary>
		/// Read everything as long as the char occurs in the accepted characters.
		/// </summary>
		public string ReadWhile(string accepted)
		{
			int start = _cursor;
			while(_cursor < _str.Length)
			{
				if(accepted.IndexOf(_str[_cursor])>=0)
					++_cursor;
				else
					break;
			}
			return _str.Substring(start, _cursor-start);
		}
		
		/// <summary>
		/// Read away any whitespace. 
		/// If a comment follows ('# bla bla') read away that as well
		/// </summary>
		public void SkipWhitespace()
		{
			while(HasMore())
			{
				char c=Read();
				if(c=='#')
				{
					ReadUntil('\n');
					return;
				}
				if(!char.IsWhiteSpace(c))
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
			if(_cursor>=_str.Length)
				throw new ParseException("no more data");
			string result=_str.Substring(_cursor);
			_cursor = _str.Length;
			return result;
		}
		
		/// <summary>
		/// Rewind a number of characters.
		/// </summary>
		public void Rewind(int count)
		{
			_cursor = Math.Max(0, _cursor-count);
		}

		/// <summary>
		/// Return a bookmark to rewind to later.
		/// </summary>
		public int Bookmark()
		{
			return _cursor;
		}
		
		/// <summary>
		/// Flip back to previously set bookmark.
		/// </summary>
		public void FlipBack(int towhichbookmark)
		{
			_cursor = towhichbookmark;
		}
		
		/// <summary>
		/// Sync the position and bookmark with the current position in another reader.
		/// </summary>
		public void Sync(SeekableStringReader inner)
		{
			_bookmark = inner._bookmark;
			_cursor = inner._cursor;
		}
		
		/// <summary>
		/// Extract a piece of context around the current cursor (if you set cursor to -1)
		/// or around a given position in the string (if you set cursor>=0).
		/// </summary>
		public void Context(int crsr, int width, out string left, out string right)
		{
			if(crsr<0)
				crsr=_cursor;
			int leftStrt = Math.Max(0, crsr-width);
			int leftLen = crsr-leftStrt;
			int rightLen = Math.Min(width, _str.Length-crsr);
			left = _str.Substring(leftStrt, leftLen);
			right = _str.Substring(crsr, rightLen);
		}
	
		public void Dispose()
		{
			_str = null;
		}
	}
}

