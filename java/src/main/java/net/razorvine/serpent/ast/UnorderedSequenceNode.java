package net.razorvine.serpent.ast;

import java.util.HashSet;
import java.util.Set;

abstract class UnorderedSequenceNode extends SequenceNode {

	@Override
	public boolean equals(Object obj)
	{
		if(!(obj instanceof UnorderedSequenceNode))
			return false;
		Set<INode> set1 = elementsAsSet();
		Set<INode> set2 = ((UnorderedSequenceNode)obj).elementsAsSet();
		return set1.equals(set2);
	}

	public Set<INode> elementsAsSet()
	{
		return new HashSet<INode>(elements);
	}
}
