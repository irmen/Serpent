package net.razorvine.serpent.ast;

class TupleNode extends SequenceNode
{
	@Override
	public String toString()
	{
		StringBuilder sb=new StringBuilder();
		sb.append('(');
		if(Elements != null)
		{
			for(INode elt: Elements)
			{
				sb.append(elt.toString());
				sb.append(",");
			}
		}
		if(Elements.size()>1)
			sb.deleteCharAt(sb.length());  // remove last comma
		sb.append(')');
		return sb.toString();
	}
	
	@Override
	public void Accept(INodeVisitor visitor)
	{
		visitor.Visit(this);
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
