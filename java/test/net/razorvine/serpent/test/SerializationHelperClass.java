package net.razorvine.serpent.test;

import java.io.Serializable;


public class SerializationHelperClass implements Serializable
{
	private static final long serialVersionUID = 5151254868567404093L;
	public int x;
	public String s;
	public int i;
	public Object obj;
	
	public String getTheString() { return s; }
	public int getTheInteger() { return i; }
	public boolean isThingy() { return true; }
	public int getNUMBER() { return 42; }
	public String getX() { return "X"; }
	public Object getObject() { return obj; }
}