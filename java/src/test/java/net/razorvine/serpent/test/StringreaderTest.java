/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent.test;

import static org.junit.Assert.*;
import net.razorvine.serpent.ParseException;
import net.razorvine.serpent.SeekableStringReader;

import org.junit.Test;

public class StringreaderTest
{
	@Test
	public void testStuff()
	{
		SeekableStringReader s=new SeekableStringReader("hello");
		assertEquals('h', s.peek());
		assertEquals("hel", s.peek(3));
		assertEquals('h', s.read());
		assertEquals("ell", s.read(3));
		assertEquals("o", s.peek(999));
		assertEquals("o", s.read(999));
		s.close();
		
		SeekableStringReader s2 = new SeekableStringReader("    skip.\t\n\rwhitespace.  ");
		s2.skipWhitespace();
		assertEquals("skip", s2.readUntil('.'));
		s2.skipWhitespace();
		assertTrue(s2.hasMore());
		assertEquals("whit", s2.peek(4));
		assertEquals("whitespace", s2.readUntil('.'));
		s2.skipWhitespace();
		assertFalse(s2.hasMore());
		try {
			s2.peek();
			fail("expected out of bounds error");
		} catch (StringIndexOutOfBoundsException x) {
			// ok
		}
		s.close();
	}
	
	@Test
	public void testRanges()
	{
		SeekableStringReader s = new SeekableStringReader("hello");
		try {
			s.read(-1);
			fail("expected parse error");
		} catch (ParseException x) {
			// ok
		}
		assertEquals("hello", s.read(999));
		
		try {
			s.read(1);
			fail("expected parse error");
		} catch (ParseException x) {
			// ok
		}
		s.rewind(Integer.MAX_VALUE);
		assertTrue(s.hasMore());
		assertEquals("hello", s.peek(999));
		assertTrue(s.hasMore());
	}
	
	@Test
	public void testReadUntil()
	{
		SeekableStringReader s = new SeekableStringReader("hello there");
		s.read();
		assertEquals("ello", s.readUntil(' '));
		assertEquals('t', s.peek());
		
		try {
			s.readUntil('x');
			fail("expected parse error");
		} catch (ParseException x) {
			// ok
		}
		
		assertEquals("there", s.rest());
		
		try {
			s.rest();
			fail("expected parse error");
		} catch (ParseException x) {
			// ok
		}
		
		s.rewind(Integer.MAX_VALUE);
		assertEquals("hell", s.readUntil("xyz o"));

		try {
			s.readUntil("xy@");
			fail("expected parse error");
		} catch (ParseException x) {
			// ok
		}
	}

	@Test
	public void testReadWhile()
	{
		SeekableStringReader s = new SeekableStringReader("123.456 foo");
		assertEquals("123.456", s.readWhile("0123456789."));
		assertEquals("", s.readWhile("@"));
		assertEquals(" ", s.readWhile(" "));
		assertEquals("foo", s.rest());
	}
	
	@Test
	public void testRead()
	{
		SeekableStringReader s = new SeekableStringReader("hello");
		assertEquals('h', s.read());
		assertEquals('e', s.read());
		assertEquals("l", s.read(1));
		assertEquals("lo", s.read(2));
		try {
			s.read();
			fail("expected bounds error");
		} catch (StringIndexOutOfBoundsException x) {
			//ok
		}
	}

	@Test
	public void testBookmark()
	{
		SeekableStringReader s = new SeekableStringReader("hello");
		s.read(2);
		int bookmark = s.bookmark();
		assertEquals("ll", s.read(2));
		s.flipBack(bookmark);
		assertEquals("ll", s.read(2));
		assertEquals("o", s.read(999));
	}
	
	@Test
	public void testNesting()
	{
		SeekableStringReader outer = new SeekableStringReader("hello!");
		outer.read(1);
		SeekableStringReader inner1 = new SeekableStringReader(outer);
		SeekableStringReader inner2 = new SeekableStringReader(outer);
		
		assertEquals("ell", inner1.read(3));
		assertEquals("el", inner2.read(2));
		assertEquals("o", inner1.read(1));
		assertEquals("l", inner2.read(1));
		assertEquals("e", outer.read(1));
		assertEquals("o", inner2.read(1));
		assertEquals("l", outer.read(1));
		outer.sync(inner2);
		assertEquals("!", outer.read(1));
	}

	@Test
	public void testContext()
	{
		SeekableStringReader s = new SeekableStringReader("abcdefghijklmnopqrstuvwxyz");
		s.read(10);
		SeekableStringReader.StringContext ctx = s.context(-1, 5);
		assertEquals("fghij", ctx.left);
		assertEquals("klmno", ctx.right);
		ctx = s.context(-1, 12);
		assertEquals("abcdefghij", ctx.left);
		assertEquals("klmnopqrstuv", ctx.right);
		s.read(13);
		ctx = s.context(-1, 6);
		assertEquals("rstuvw", ctx.left);
		assertEquals("xyz", ctx.right);
		
		ctx=s.context(5,4);
		assertEquals("bcde", ctx.left);
		assertEquals("fghi", ctx.right);
	}
}
