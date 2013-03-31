package net.razorvine.serpent.ast;

import java.util.ArrayList;
import java.util.List;

abstract class SequenceNode implements INode
{
	public List<INode> Elements = new ArrayList<INode>();
	public abstract char getOpenChar();
	public abstract char getCloseChar();

	@Override
	public int hashCode()
	{
		int hashCode = 0;
		for(INode elt: Elements)
			hashCode += 1000000007 * elt.hashCode();
		return hashCode;
	}

	@Override
	public boolean equals(Object obj)
	{
		if(!(obj instanceof SequenceNode))
			return false;
		SequenceNode other = (SequenceNode)obj;
		return Elements.equals(other.Elements);
	}

	@Override
	public String toString()
	{
		StringBuilder sb=new StringBuilder();
		sb.append(getOpenChar());
		if(Elements != null)
		{
			for(INode elt: Elements)
			{
				sb.append(elt.toString());
				sb.append(',');
			}
		}
		if(Elements.size()>0)
			sb.deleteCharAt(sb.length()); // remove last comma
		sb.append(getCloseChar());
		return sb.toString();
	}
	
	public abstract void Accept(INodeVisitor visitor);
}
