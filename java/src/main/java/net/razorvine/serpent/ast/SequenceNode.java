package net.razorvine.serpent.ast;

import java.util.ArrayList;
import java.util.List;

public abstract class SequenceNode implements INode
{
	public List<INode> elements = new ArrayList<INode>();
	public abstract char getOpenChar();
	public abstract char getCloseChar();

	@Override
	public int hashCode()
	{
		int hashCode = 0;
		for(INode elt: elements)
			hashCode += 1000000007 * elt.hashCode();
		return hashCode;
	}

	@Override
	public boolean equals(Object obj)
	{
		if(!(obj instanceof SequenceNode))
			return false;
		SequenceNode other = (SequenceNode)obj;
		return elements.equals(other.elements);
	}

	@Override
	public String toString()
	{
		StringBuilder sb=new StringBuilder();
		sb.append(getOpenChar());
		if(elements != null)
		{
			for(INode elt: elements)
			{
				sb.append(elt.toString());
				sb.append(',');
			}
		}
		if(elements.size()>0)
			sb.deleteCharAt(sb.length()-1); // remove last comma
		sb.append(getCloseChar());
		return sb.toString();
	}
	
	public abstract void accept(INodeVisitor visitor);
}
