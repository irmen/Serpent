package net.razorvine.serpent.ast;

interface INode
{
	String toString();
	boolean equals(Object obj);
	void Accept(INodeVisitor visitor);
}
