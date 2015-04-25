package net.razorvine.serpent.ast;

public interface INode
{
	String toString();
	boolean equals(Object obj);
	void accept(INodeVisitor visitor);
}
