package net.razorvine.serpent.ast;

public class KeyValueNode implements INode
{
	public INode key;
	public INode value;
	
	public KeyValueNode()
	{}
	
	public KeyValueNode(INode key, INode value)
	{
		this.key = key;
		this.value = value;
	}

	@Override
	public String toString()
	{
		return String.format("%s:%s", key, value);
	}
	
	public void accept(INodeVisitor visitor)
	{
		throw new NoSuchMethodError("don't visit a keyvaluenode");
	}
	
	@Override
	public boolean equals(Object obj)
	{
		if(!(obj instanceof KeyValueNode))
			return false;
		KeyValueNode other = (KeyValueNode) obj;
		return key.equals(other.key) && value.equals(other.value);
	}
	
	@Override
	public int hashCode()
	{
		return key.hashCode() ^ (1000000007 * value.hashCode());
	}
}
