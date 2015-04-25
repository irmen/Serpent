package net.razorvine.serpent.ast;

public class StringNode extends PrimitiveNode<String>
{
	public StringNode(String value)
	{
		super(value);
	}

	@Override
	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}
}
