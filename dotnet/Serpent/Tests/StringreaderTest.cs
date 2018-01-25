using System;
using Xunit;
// ReSharper disable CheckNamespace

namespace Razorvine.Serpent.Test
{
	public class StringreaderTest
	{
		[Fact]
		public void TestStuff()
		{
			using(SeekableStringReader s=new SeekableStringReader("hello"))
			{
				Assert.Equal('h', s.Peek());
				Assert.Equal("hel", s.Peek(3));
				Assert.Equal('h', s.Read());
				Assert.Equal("ell", s.Read(3));
				Assert.Equal("o", s.Peek(999));
				Assert.Equal("o", s.Read(999));
			}
			
			using(SeekableStringReader s2 = new SeekableStringReader("    skip.\t\n\rwhitespace.  "))
			{
				s2.SkipWhitespace();
				Assert.Equal("skip", s2.ReadUntil('.'));
				s2.SkipWhitespace();
				Assert.Equal("whitespace", s2.ReadUntil('.'));
				s2.SkipWhitespace();
				Assert.False(s2.HasMore());
				Assert.Throws<IndexOutOfRangeException>(()=>s2.Peek());
			}
		}
		
		[Fact]
		public void TestRead()
		{
			SeekableStringReader s = new SeekableStringReader("hello");
			Assert.Equal('h', s.Read());
			Assert.Equal('e', s.Read());
			Assert.Equal("l", s.Read(1));
			Assert.Equal("lo", s.Read(2));
			Assert.Throws<IndexOutOfRangeException>(()=>s.Read());
		}

		[Fact]
		public void TestRanges()
		{
			SeekableStringReader s = new SeekableStringReader("hello");
			Assert.Throws<ParseException>(()=>s.Read(-1));
			Assert.Equal("hello", s.Read(999));
			Assert.Throws<ParseException>(()=>s.Read(1));
			s.Rewind(int.MaxValue);
			Assert.True(s.HasMore());
			Assert.Equal("hello", s.Peek(999));
			Assert.True(s.HasMore());
		}
		
		[Fact]
		public void TestReadUntil()
		{
			SeekableStringReader s = new SeekableStringReader("hello there");
			s.Read();
			Assert.Equal("ello", s.ReadUntil(' '));
			Assert.Equal('t', s.Peek());
			Assert.Throws<ParseException>(()=>s.ReadUntil('x'));
			
			Assert.Equal("there", s.Rest());
			Assert.Throws<ParseException>(()=>s.Rest());
			
			s.Rewind(int.MaxValue);
			Assert.Equal("hell", s.ReadUntil('x', 'y', 'z', ' ', 'o'));
			Assert.Throws<ParseException>(()=>s.ReadUntil('x', 'y', '@'));
		}

		[Fact]
		public void TestReadWhile()
		{
			SeekableStringReader s = new SeekableStringReader("123.456 foo");
			Assert.Equal("123.456", s.ReadWhile("0123456789."));
			Assert.Equal("", s.ReadWhile("@"));
			Assert.Equal(" ", s.ReadWhile(" "));
			Assert.Equal("foo", s.Rest());
		}
		
		[Fact]
		public void TestBookmark()
		{
			SeekableStringReader s = new SeekableStringReader("hello");
			s.Read(2);
			int bookmark = s.Bookmark();
			Assert.Equal("ll", s.Read(2));
			s.FlipBack(bookmark);
			Assert.Equal("ll", s.Read(2));
			Assert.Equal("o", s.Read(999));
		}
		
		[Fact]
		public void TestNesting()
		{
			SeekableStringReader outer = new SeekableStringReader("hello!");
			outer.Read(1);
			SeekableStringReader inner1 = new SeekableStringReader(outer);
			SeekableStringReader inner2 = new SeekableStringReader(outer);
			
			Assert.Equal("ell", inner1.Read(3));
			Assert.Equal("el", inner2.Read(2));
			Assert.Equal("o", inner1.Read(1));
			Assert.Equal("l", inner2.Read(1));
			Assert.Equal("e", outer.Read(1));
			Assert.Equal("o", inner2.Read(1));
			Assert.Equal("l", outer.Read(1));
			outer.Sync(inner2);
			Assert.Equal("!", outer.Read(1));
		}

		[Fact]
		public void TestContext()
		{
			SeekableStringReader s = new SeekableStringReader("abcdefghijklmnopqrstuvwxyz");
			s.Read(10);
			string left, right;
			s.Context(-1, 5, out left, out right);
			Assert.Equal("fghij", left);
			Assert.Equal("klmno", right);
			s.Context(-1, 12, out left, out right);
			Assert.Equal("abcdefghij", left);
			Assert.Equal("klmnopqrstuv", right);
			s.Read(13);
			s.Context(-1, 6, out left, out right);
			Assert.Equal("rstuvw", left);
			Assert.Equal("xyz", right);
			
			s.Context(5,4, out left, out right);
			Assert.Equal("bcde", left);
			Assert.Equal("fghi", right);
		}
	}
}