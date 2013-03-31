package net.razorvine.serpent.ast;

class NoneNode implements INode
{
	public static NoneNode Instance = new NoneNode();
	private NoneNode()
	{
	}
	
	public String toString()
	{
		return "None";
	}

	public void Accept(INodeVisitor visitor)
	{
		visitor.Visit(this);
	}			
}
