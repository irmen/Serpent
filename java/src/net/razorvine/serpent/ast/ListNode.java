package net.razorvine.serpent.ast;

class ListNode extends SequenceNode
{
	@Override
	public void Accept(INodeVisitor visitor)
	{
		visitor.Visit(this);
	}

	@Override
	public char getOpenChar() {
		return '[';
	}

	@Override
	public char getCloseChar() {
		return ']';
	}
}
