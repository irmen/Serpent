package net.razorvine.serpent.ast;

public class DoubleNode extends PrimitiveNode<Double>
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
