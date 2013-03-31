package net.razorvine.serpent.ast;

class SetNode extends SequenceNode
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
