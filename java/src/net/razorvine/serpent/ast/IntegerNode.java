package net.razorvine.serpent.ast;

class IntegerNode extends PrimitiveNode<Integer>
{
	public IntegerNode(int value)
	{
		super(value);
	}

	@Override
	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}
}
