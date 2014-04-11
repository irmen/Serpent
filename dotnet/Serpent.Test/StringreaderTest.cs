/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright 2014, Irmen de Jong (irmen@razorvine.net)
/// Software license: "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Razorvine.Serpent.Test
{
	[TestFixture]
	public class StringreaderTest
	{
		[Test]
		public void TestStuff()
		{
			using(SeekableStringReader s=new SeekableStringReader("hello"))
			{
				Assert.AreEqual('h', s.Peek());
				Assert.AreEqual("hel", s.Peek(3));
				Assert.AreEqual('h', s.Read());
				Assert.AreEqual("ell", s.Read(3));
				Assert.AreEqual("o", s.Peek(999));
				Assert.AreEqual("o", s.Read(999));
			}
			
			using(SeekableStringReader s2 = new SeekableStringReader("    skip.\t\n\rwhitespace.  "))
			{
				s2.SkipWhitespace();
				Assert.AreEqual("skip", s2.ReadUntil('.'));
				s2.SkipWhitespace();
				Assert.AreEqual("whitespace", s2.ReadUntil('.'));
				s2.SkipWhitespace();
				Assert.IsFalse(s2.HasMore());
				Assert.Throws<IndexOutOfRangeException>(()=>s2.Peek());
			}
		}
		
		[Test]
		public void TestRead()
		{
			SeekableStringReader s = new SeekableStringReader("hello");
			Assert.AreEqual('h', s.Read());
			Assert.AreEqual('e', s.Read());
			Assert.AreEqual("l", s.Read(1));
			Assert.AreEqual("lo", s.Read(2));
			Assert.Throws<IndexOutOfRangeException>(()=>s.Read());
		}

		[Test]
		public void TestRanges()
		{
			SeekableStringReader s = new SeekableStringReader("hello");
			Assert.Throws<ParseException>(()=>s.Read(-1));
			Assert.AreEqual("hello", s.Read(999));
			Assert.Throws<ParseException>(()=>s.Read(1));
			s.Rewind(int.MaxValue);
			Assert.IsTrue(s.HasMore());
			Assert.AreEqual("hello", s.Peek(999));
			Assert.IsTrue(s.HasMore());
		}
		
		[Test]
		public void TestReadUntil()
		{
			SeekableStringReader s = new SeekableStringReader("hello there");
			s.Read();
			Assert.AreEqual("ello", s.ReadUntil(' '));
			Assert.AreEqual('t', s.Peek());
			Assert.Throws<ParseException>(()=>s.ReadUntil('x'));
			
			Assert.AreEqual("there", s.Rest());
			Assert.Throws<ParseException>(()=>s.Rest());
			
			s.Rewind(int.MaxValue);
			Assert.AreEqual("hell", s.ReadUntil('x', 'y', 'z', ' ', 'o'));
			Assert.Throws<ParseException>(()=>s.ReadUntil('x', 'y', '@'));
		}

		[Test]
		public void TestReadWhile()
		{
			SeekableStringReader s = new SeekableStringReader("123.456 foo");
			Assert.AreEqual("123.456", s.ReadWhile('0','1','2','3','4','5','6','7','8','9','.'));
			Assert.AreEqual("", s.ReadWhile('@'));
			Assert.AreEqual(" ", s.ReadWhile(' '));
			Assert.AreEqual("foo", s.Rest());
		}
		
		[Test]
		public void TestBookmark()
		{
			SeekableStringReader s = new SeekableStringReader("hello");
			s.Read(2);
			int bookmark = s.Bookmark();
			Assert.AreEqual("ll", s.Read(2));
			s.FlipBack(bookmark);
			Assert.AreEqual("ll", s.Read(2));
			Assert.AreEqual("o", s.Read(999));
		}
		
		[Test]
		public void TestNesting()
		{
			SeekableStringReader outer = new SeekableStringReader("hello!");
			outer.Read(1);
			SeekableStringReader inner1 = new SeekableStringReader(outer);
			SeekableStringReader inner2 = new SeekableStringReader(outer);
			
			Assert.AreEqual("ell", inner1.Read(3));
			Assert.AreEqual("el", inner2.Read(2));
			Assert.AreEqual("o", inner1.Read(1));
			Assert.AreEqual("l", inner2.Read(1));
			Assert.AreEqual("e", outer.Read(1));
			Assert.AreEqual("o", inner2.Read(1));
			Assert.AreEqual("l", outer.Read(1));
			outer.Sync(inner2);
			Assert.AreEqual("!", outer.Read(1));
		}

		[Test]
		public void TestContext()
		{
			SeekableStringReader s = new SeekableStringReader("abcdefghijklmnopqrstuvwxyz");
			s.Read(10);
			string left, right;
			s.Context(-1, 5, out left, out right);
			Assert.AreEqual("fghij", left);
			Assert.AreEqual("klmno", right);
			s.Context(-1, 12, out left, out right);
			Assert.AreEqual("abcdefghij", left);
			Assert.AreEqual("klmnopqrstuv", right);
			s.Read(13);
			s.Context(-1, 6, out left, out right);
			Assert.AreEqual("rstuvw", left);
			Assert.AreEqual("xyz", right);
			
			s.Context(5,4, out left, out right);
			Assert.AreEqual("bcde", left);
			Assert.AreEqual("fghi", right);
		}
	}
}