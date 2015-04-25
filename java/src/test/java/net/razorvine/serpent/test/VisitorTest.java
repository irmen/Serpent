package net.razorvine.serpent.test;

import static org.junit.Assert.assertEquals;
import org.junit.Test;

import java.io.DataInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.util.List;
import java.util.Map;

import net.razorvine.serpent.ComplexNumber;
import net.razorvine.serpent.IDictToInstance;
import net.razorvine.serpent.ObjectifyVisitor;
import net.razorvine.serpent.Parser;
import net.razorvine.serpent.ast.Ast;


public class VisitorTest
{
	@SuppressWarnings("unchecked")
	@Test
	public void testObjectify() throws IOException
	{
		Parser p = new Parser();
		File testdatafile = new File("src/test/java/testserpent.utf8.bin");
		byte[] ser = new byte[(int) testdatafile.length()];
		FileInputStream fis=new FileInputStream(testdatafile);
		DataInputStream dis = new DataInputStream(fis);
		dis.readFully(ser);
		dis.close();
		fis.close();
		
		Ast ast = p.parse(ser);
		ObjectifyVisitor visitor = new ObjectifyVisitor();
		ast.accept(visitor);
		Object thing = visitor.getObject();
		
		Map<Object,Object> dict = (Map<Object,Object>) thing;
		assertEquals(11, dict.size());
		List<Object> list = (List<Object>) dict.get("numbers");
		assertEquals(4, list.size());
		assertEquals(999.1234, list.get(1));
		assertEquals(new ComplexNumber(-3, 8), list.get(3));
		String euro = (String) dict.get("unicode");
		assertEquals("\u20ac", euro);
		
		Map<String, Object> exc = (Map<String, Object>) dict.get("exc");
		Object[] args = (Object[]) exc.get("args");
		assertEquals("fault", args[0]);
		assertEquals("ZeroDivisionError", exc.get("__class__"));
	}

	
	class ArithmeticExcFromDict implements IDictToInstance
	{
		public Object convert(Map<Object, Object> dict)
		{
			String classname = (String) dict.get("__class__");
			if("ZeroDivisionError".equals(classname))
			{
				Object[] args = (Object[]) dict.get("args");
				return new ArithmeticException((String)args[0]);
			}
			return null;
		}
	}

	@SuppressWarnings("unchecked")
	@Test
	public void testObjectifyDictToClass() throws IOException
	{
		Parser p = new Parser();
		File testdatafile = new File("src/test/java/testserpent.utf8.bin");
		byte[] ser = new byte[(int) testdatafile.length()];
		FileInputStream fis=new FileInputStream(testdatafile);
		DataInputStream dis = new DataInputStream(fis);
		dis.readFully(ser);
		dis.close();
		fis.close();
		
		Ast ast = p.parse(ser);
		
		ObjectifyVisitor visitor = new ObjectifyVisitor(new ArithmeticExcFromDict());
		ast.accept(visitor);
		Object thing = visitor.getObject();
		
		Map<Object,Object> dict = (Map<Object,Object>) thing;
		assertEquals(11, dict.size());
		
		ArithmeticException exc = (ArithmeticException) dict.get("exc");
		assertEquals("fault", exc.getMessage());
	}
}
