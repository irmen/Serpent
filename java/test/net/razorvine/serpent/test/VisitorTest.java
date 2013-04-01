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
		File testdatafile = new File("test/testserpent.utf8.bin");
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
	}
}
