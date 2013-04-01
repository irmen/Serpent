package net.razorvine.serpent.ast;

public class KeyValueNode implements INode
{
	public INode key;
	public INode value;
	
	@Override
	public String toString()
	{
		return String.format("{0}:{1}", key, value);
	}
	
	public void accept(INodeVisitor visitor)
	{
		throw new NoSuchMethodError("don't visit a keyvaluenode");
	}
}
