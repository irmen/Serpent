package net.razorvine.serpent.ast;

public class TupleNode extends SequenceNode
{
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
				sb.append(",");
			}
		}
		if(elements.size()>1)
			sb.deleteCharAt(sb.length()-1);  // remove last comma
		sb.append(getCloseChar());
		return sb.toString();
	}
	
	@Override
	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}

	@Override
	public char getOpenChar() {
		return '(';
	}

	@Override
	public char getCloseChar() {
		return ')';
	}
}
