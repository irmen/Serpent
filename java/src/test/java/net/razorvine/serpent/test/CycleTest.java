/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */
package net.razorvine.serpent.test;

import java.util.HashMap;
import java.util.LinkedList;

import net.razorvine.serpent.Parser;
import net.razorvine.serpent.Serializer;

import org.junit.Test;
import static org.junit.Assert.*;


public class CycleTest
{
	@Test
	public void testTupleOk()
	{
		Serializer ser = new Serializer();
		int[] t = new int[] {1,2,3};
		Object[] d = new Object[] {t,t,t};
		byte[] data = ser.serialize(d);
		Parser parser = new Parser();
        parser.parse(data);
	}

	@Test
	public void testListOk()
	{
		Serializer ser = new Serializer();
		LinkedList<Integer> t = new LinkedList<Integer>();
		t.add(1);
		t.add(2);
		t.add(3);
		LinkedList<Object> d = new LinkedList<Object>();
		d.add(t);
		d.add(t);
		d.add(t);
		byte[] data = ser.serialize(d);
		Parser parser = new Parser();
        parser.parse(data);
	}

	@Test
	public void testDictOk()
	{
		Serializer ser = new Serializer();
		HashMap<String, Integer> t = new HashMap<String, Integer>();
		t.put("a", 1);
		HashMap<String, Object> d = new HashMap<String, Object>();
		d.put("x", t);
		d.put("y", t);
		d.put("z", t);
		byte[] data = ser.serialize(d);
		Parser parser = new Parser();
        parser.parse(data);
	}

	@Test(expected=IllegalArgumentException.class)
	public void testListCycle()
	{
		Serializer ser = new Serializer();
		LinkedList<Object> d = new LinkedList<Object>();
		d.add(1);
		d.add(2);
		d.add(d);
		ser.serialize(d);
	}
	
	@Test(expected=IllegalArgumentException.class)
	public void testDictCycle()
	{
		Serializer ser = new Serializer();
		HashMap<String, Object> d = new HashMap<String, Object>();
		d.put("x", 1);
		d.put("y", 2);
		d.put("z", d);
		ser.serialize(d);
	}

	@Test(expected=IllegalArgumentException.class)
	public void testClassCycle()
	{
		Serializer ser = new Serializer();
		SerializationHelperClass thing = new SerializationHelperClass();
		thing.i = 42;
		thing.s = "hello";
		thing.x = 99;
		thing.obj = thing;
		ser.serialize(thing);
	}
	
	@Test
	public void testMaxLevel()
	{
		Serializer ser = new Serializer();
		assertEquals(500, ser.maximumLevel);
		
		Object[] array = new Object[] {
			"level1",
			new Object[] {
				"level2",
				new Object[] {
					"level3",
					new Object[] {
						"level 4"
					}
				}
			}
		};

		ser.maximumLevel = 4;
		ser.serialize(array);		// should work
		
		ser.maximumLevel = 3;
		try {
			ser.serialize(array);
			fail("should fail");
		} catch(IllegalArgumentException x) {
			assertTrue(x.getMessage().contains("too deep"));
		}
	}
}
