package net.razorvine.serpent.ast;

class LongNode extends PrimitiveNode<Long>
{
	public LongNode(long value)
	{
		super(value);
	}
	
	@Override
	public void Accept(INodeVisitor visitor)
	{
		visitor.Visit(this);
	}
}
