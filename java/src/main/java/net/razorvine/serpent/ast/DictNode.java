package net.razorvine.serpent.ast;

public class DictNode extends UnorderedSequenceNode
{
	@Override
	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}

	@Override
	public char getOpenChar() {
		return '{';
	}

	@Override
	public char getCloseChar() {
		return '}';
	}
}
