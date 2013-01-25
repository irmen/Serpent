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
			Assert.AreEqual(new Ast.PrimitiveNode<bool>(false), p.Parse("False").Root);
			Assert.AreEqual(Ast.NoneNode.Instance, p.Parse("None").Root);
		}
		
		[Test]
		public void TestStrings()
		{
			Parser p = new Parser();
			Assert.AreEqual(new Ast.PrimitiveNode<string>("hello"), p.Parse("'hello'").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("hello"), p.Parse("\"hello\"").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("\\"), p.Parse("'\\\\'").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("\\"), p.Parse("\"\\\\\"").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("'"), p.Parse("\"'\"").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("\""), p.Parse("'\"'").Root);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("tab\tnewline\n."), p.Parse("'tab\\tnewline\\n.'").Root);
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
			Assert.AreEqual(new Ast.PrimitiveNode<string>("   string value    "), p.Parse("  '   string value    '   ").Root);
			Ast ast = p.Parse("     (  42  ,  ( 'x',   'y'  )   ");
			Ast.TupleNode tuple = (Ast.TupleNode) ast.Root;
			Assert.AreEqual(new Ast.PrimitiveNode<int>(42), tuple.Elements[0]);
			tuple = (Ast.TupleNode) tuple.Elements[1];
			Assert.AreEqual(new Ast.PrimitiveNode<string>("x"), tuple.Elements[0]);
			Assert.AreEqual(new Ast.PrimitiveNode<string>("y"), tuple.Elements[1]);
		}
		
		[Test]
		public void TestTuple()
		{
			Parser p = new Parser();
			Ast.TupleNode tuple = new Ast.TupleNode();
			Ast.TupleNode tuple2 = new Ast.TupleNode();
			Assert.AreEqual(tuple, tuple2);
			
			tuple.Elements.Add(new Ast.PrimitiveNode<int>(42));
			tuple2.Elements.Add(new Ast.PrimitiveNode<int>(42));
			Assert.AreEqual(tuple, tuple2);
			tuple2.Elements.Add(new Ast.PrimitiveNode<int>(43));
			tuple2.Elements.Add(new Ast.PrimitiveNode<int>(44));
			Assert.AreNotEqual(tuple, tuple2);
			
			Assert.AreEqual(new Ast.TupleNode(), p.Parse("()").Root);
			Assert.AreEqual(tuple, p.Parse("(42,)").Root);
			Assert.AreEqual(tuple2, p.Parse("( 42,43, 44 )").Root);
		}
		
		[Test]
		public void TestList()
		{
			Parser p = new Parser();
			Ast.ListNode list = new Ast.ListNode();
			Ast.ListNode list2 = new Ast.ListNode();
			Assert.AreEqual(list, list2);
			
			list.Elements.Add(new Ast.PrimitiveNode<int>(42));
			list2.Elements.Add(new Ast.PrimitiveNode<int>(42));
			Assert.AreEqual(list, list2);
			list2.Elements.Add(new Ast.PrimitiveNode<int>(43));
			list2.Elements.Add(new Ast.PrimitiveNode<int>(44));
			Assert.AreNotEqual(list, list2);
			
			Assert.AreEqual(new Ast.ListNode(), p.Parse("[]").Root);
			Assert.AreEqual(list, p.Parse("[42]").Root);
			Assert.Throws<ParseException>(()=>p.Parse("[42,]"));
			Assert.AreEqual(list2, p.Parse("[ 42,43, 44 ]").Root);
		}		
	}
}