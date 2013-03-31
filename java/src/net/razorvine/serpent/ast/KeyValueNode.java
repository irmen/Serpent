package net.razorvine.serpent.ast;

class KeyValueNode implements INode
{
	public INode Key;
	public INode Value;
	
	@Override
	public String toString()
	{
		return String.format("{0}:{1}", Key, Value);
	}
	
	public void Accept(INodeVisitor visitor)
	{
		throw new NoSuchMethodError("don't visit a keyvaluenode");
	}
}
