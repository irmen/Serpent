package net.razorvine.serpent.ast;

class StringNode extends PrimitiveNode<String>
{
	public StringNode(String value)
	{
		super(value);
	}

	@Override
	public void Accept(INodeVisitor visitor)
	{
		visitor.Visit(this);
	}
}
