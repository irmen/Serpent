using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Razorvine.Serpent.Test
{
	[TestFixture]
	public class ParserTest
	{
		[Test]
		public void TestBasic()
		{
			Parser p = new Parser();
			Assert.IsNull(p.Parse((string)null).Root);
			Assert.IsNull(p.Parse("").Root);
		}
		
		[Test]
		public void TestPrimitives()
		{
			Parser p = new Parser();
			Assert.AreEqual(new Ast.PrimitiveNode<int>(42), p.Parse("42").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<int>(-42), p.Parse("-42").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<double>(42.331), p.Parse("42.331").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<double>(-42.331), p.Parse("-42.331").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<double>(-1.2e19), p.Parse("-1.2e+19").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<double>(0.0004), p.Parse("4e-4").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<double>(40000), p.Parse("4e4").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<bool>(true), p.Parse("True").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<bool>(true), p.Parse("False").Root);
		}
		
		[Test]
		public void TestStrings()
		{
			Parser p = new Parser();
			Assert.AreEqual(new Ast.PrimitiveNode<string>("hello"), p.Parse("'hello'").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("hello"), p.Parse("\"hello\"").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("\\"), p.Parse("'\\'").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("\\"), p.Parse("\"\\\"").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("'"), p.Parse("\"'\"").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("\""), p.Parse("'\"'").Root);
		}
		
		[Test]
		public void TestUnicode()
		{
			Parser p = new Parser();
			string str = "'\u20ac\u2603'";
			Assert.AreEqual(0x20ac, str[1]);
			Assert.AreEqual(0x2603, str[2]);
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			
			string value = "\u20ac\u2603";
			Assert.AreEqual(new Ast.PrimitiveNode<string>(value), p.Parse(str).Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>(value), p.Parse(bytes).Root);
		}
		
		[Test]
		public void TestWhitespace()
		{
			Parser p = new Parser();
			Assert.AreEqual(new Ast.PrimitiveNode<int>(42), p.Parse(" 42 ").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<int>(42), p.Parse("  42  ").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<int>(42), p.Parse("\t42\r\n").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<int>(42), p.Parse(" \t 42 \r \n ").Root);
			Assert.AreEqual("blerp", p.Parse("  [  42 ,   43  ]   "));  // @todo will fail now
		}
	}
}