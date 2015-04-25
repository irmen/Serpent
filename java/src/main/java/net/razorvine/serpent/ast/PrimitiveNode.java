package net.razorvine.serpent.ast;


public abstract class PrimitiveNode<T> implements INode, Comparable<T>
{
	public T value;
	public PrimitiveNode(T value)
	{
		this.value=value;
	}
	
	@Override
	public int hashCode()
	{
		return value!=null? value.hashCode() : 0;
	}
	
	@Override
	public boolean equals(Object obj)
	{
		return (obj instanceof PrimitiveNode<?>) &&	value.equals(((PrimitiveNode<?>)obj).value);
	}
	
	public int compareTo(T other)
	{
		return 0;
	}

	public boolean equals(PrimitiveNode<T> other)
	{
		return this.value.equals(other.value);
	}

	@Override
	public String toString()
	{
		if(value instanceof String)
		{
			StringBuilder sb=new StringBuilder();
			sb.append("'");
			String strValue = (String)value;
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
		else if(value instanceof Boolean)
		{
			return value.equals(Boolean.TRUE)? "True": "False";
		}
		else if(value instanceof Float || value instanceof Double)
		{
			String d = value.toString();
			if(d.indexOf('.')<=0 && d.indexOf('e')<=0 && d.indexOf('E')<=0)
				d+=".0";
			return d;
		}
		else return value.toString();
	}
	
	public abstract void accept(INodeVisitor visitor);
}
