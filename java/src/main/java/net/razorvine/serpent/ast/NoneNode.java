package net.razorvine.serpent.ast;

public class NoneNode implements INode
{
	public static NoneNode Instance = new NoneNode();
	private NoneNode()
	{
	}
	
	public String toString()
	{
		return "None";
	}

	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}
}
