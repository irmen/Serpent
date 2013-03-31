package net.razorvine.serpent.ast;

class BooleanNode extends PrimitiveNode<Boolean>
{
	public BooleanNode(boolean value)
	{
		super(value);
	}
	
	@Override
	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}
}
