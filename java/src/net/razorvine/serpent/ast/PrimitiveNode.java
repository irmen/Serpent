package net.razorvine.serpent.ast;

abstract class PrimitiveNode<T> implements INode, Comparable<T>
{
	public T Value;
	public PrimitiveNode(T value)
	{
		this.Value=value;
	}
	
	@Override
	public int hashCode()
	{
		return Value!=null? Value.hashCode() : 0;
	}
	
	@Override
	public boolean equals(Object obj)
	{
		return (obj instanceof PrimitiveNode<?>) &&	Value.equals(((PrimitiveNode<?>)obj).Value);
	}
	
	public int compareTo(T other)
	{
		return 0;
	}

	public boolean equals(PrimitiveNode<T> other)
	{
		return this.Value.equals(other.Value);
	}

	@Override
	public String toString()
	{
		if(Value instanceof String)
		{
			StringBuilder sb=new StringBuilder();
			sb.append("'");
			String strValue = (String)Value;
			for(char c: strValue.toCharArray())
			{
				switch(c)
				{
					case '\\':
						sb.append("\\\\");
						break;
					case '\'':
						sb.append("\\'");
						break;
					case '\b':
						sb.append("\\b");
						break;
					case '\f':
						sb.append("\\f");
						break;
					case '\n':
						sb.append("\\n");
						break;
					case '\r':
						sb.append("\\r");
						break;
					case '\t':
						sb.append("\\t");
						break;
					default:
						sb.append(c);
						break;
				}
			}
			sb.append("'");
			return sb.toString();
		}
		else if(Value instanceof Number)
		{
			String d = Value.toString();
			if(d.indexOf('.')<=0 && d.indexOf('e')<=0 && d.indexOf('E')<=0)
				d+=".0";
			return d;
		}
		else return Value.toString();
	}
	
	public abstract void Accept(INodeVisitor visitor);
}
