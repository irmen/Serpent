/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent;

	
/**
 * A special string reader that is suitable for the parser to read through
 * the expression string. You can rewind it, set bookmarks to flip back to, etc.
 */
public class SeekableStringReader
{
	private String str;
	private int cursor = 0;
	private int bookmark = -1;

	public SeekableStringReader(String str)
	{
		if(str==null)
			throw new IllegalArgumentException("str may not be null");

		this.str = str;	
	}
	
	/**
	 * Make a nested reader with its own cursor and bookmark.
	 * The cursor starts at the same position as the parent.
	 */
	public SeekableStringReader(SeekableStringReader parent)
	{
		str = parent.str;
		cursor = parent.cursor;
	}

	/**
	 * Is tehre more to read?
	 */
	public boolean hasMore()
	{
		return cursor<str.length();
	}
	
	/**
	 * What is the next character?
	 */
	public char peek()
	{
		return str.charAt(cursor);
	}

	/**
	 * What are the next characters that will be read?
	 */
	public String peek(int count)
	{
		return str.substring(cursor, cursor+Math.min(count, str.length()-cursor));
	}

	/**
	 * Read a single character.
	 */
	public char read()
	{
		return str.charAt(cursor++);
	}
	
	/**
	 * Read a number of characters.
	 */
	public String read(int count)
	{
		if(count<0)
			throw new ParseException("use Rewind to seek back");
		int safecount = Math.min(count, str.length()-cursor);
		if(safecount==0 && count>0)
			throw new ParseException("no more data");
		
		String result = str.substring(cursor, cursor+safecount);
		cursor += safecount;
		return result;
	}
	
	/**
	 * Read everything until one the sentinel, which must exist in the string.
	 * Sentinel char is read but not returned in the result.
	 */
	public String readUntil(char sentinel)
	{
		int i = str.indexOf(sentinel, cursor);
		if(i>=0) {
			int from = cursor;
			cursor = i+1;
			return str.substring(from, i);
		}
		throw new ParseException("terminator not found");
	}

	/**
	 * Read everything until one of the sentinel(s), which must exist in the string.
	 * Sentinel char is read but not returned in the result.
	 */
	public String readUntil(String sentinels)
	{
		int index=Integer.MAX_VALUE;
		for(char s: sentinels.toCharArray())
		{
			int i = str.indexOf(s, cursor);
			if(i>=0)
				index = Math.min(i, index);
		}
			
		if(index>=0 && index<Integer.MAX_VALUE)
		{
			String result = str.substring(cursor, index);
			cursor = index+1;
			return result;
		}
		throw new ParseException("terminator not found");
	}
	
	/**
	 * Read everything as long as the char occurs in the accepted characters.
	 */
	public String readWhile(String accepted)
	{
		int start = cursor;
		while(cursor < str.length())
		{
			if(accepted.indexOf(str.charAt(cursor))>=0)
				++cursor;
			else
				break;
		}
		return str.substring(start, cursor);
	}
	
	/**
	 * Read away any whitespace.
	 * If a comment follows ('# bla bla') read away that as well
	 */
	public void skipWhitespace()
	{
		while(hasMore())
		{
			char c=read();
			if(c=='#')
			{
				readUntil('\n');
				return;
			}
			if(!Character.isWhitespace(c))
			{
				rewind(1);
				return;
			}
		}
	}

	/**
	 * Returns the rest of the data until the end.
	 */
	public String rest()
	{
		if(cursor>=str.length())
			throw new ParseException("no more data");
		String result=str.substring(cursor);
		cursor = str.length();
		return result;
	}
	
	/**
	 * Rewind a number of characters.
	 */
	public void rewind(int count)
	{
		cursor = Math.max(0, cursor-count);
	}

	/**
	 * Return a bookmark to rewind to later.
	 */
	public int bookmark()
	{
		return cursor;
	}
	
	/**
	 * Flip back to previously set bookmark.
	 */
	public void flipBack(int bookmark)
	{
		cursor = bookmark;
	}
	
	/**
	 * Sync the position and bookmark with the current position in another reader.
	 */
	public void sync(SeekableStringReader inner)
	{
		bookmark = inner.bookmark;
		cursor = inner.cursor;
	}
	
	/**
	 * Extract a piece of context around the current cursor (if you set cursor to -1)
	 * or around a given position in the string (if you set cursor>=0).
	 */
	public class StringContext
	{
		public String left;
		public String right;
	}

	public StringContext context(int crsr, int width)
	{
		if(crsr<0)
			crsr=this.cursor;
		int leftStrt = Math.max(0, crsr-width);
		int leftLen = crsr-leftStrt;
		int rightLen = Math.min(width, str.length()-crsr);
		StringContext result = new StringContext();
		result.left = str.substring(leftStrt, leftStrt+leftLen);
		result.right = str.substring(crsr, crsr+rightLen);
		return result;
	}
	
	public void close()
	{
		this.str = null;
	}
}

