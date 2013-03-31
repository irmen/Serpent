package net.razorvine.serpent.ast;

class DoubleNode extends PrimitiveNode<Double>
{
	public DoubleNode(double value)
	{
		super(value);
	}

	@Override
	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}
}
