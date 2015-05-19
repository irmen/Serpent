package net.razorvine.serpent.ast;

public class ComplexNumberNode implements INode
{
	public double real;
	public double imaginary;
	
	public ComplexNumberNode()
	{}
	
	public ComplexNumberNode(double real, double imaginary)
	{
		this.real=real;
		this.imaginary=imaginary;
	}

	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}

	@Override
	public String toString()
	{
		String strReal = ""+real;
		String strImag = ""+imaginary;
		if(imaginary>=0)
			return String.format("(%s+%sj)", strReal, strImag);
		return String.format("(%s%sj)", strReal, strImag);
	}
	
	@Override
	public boolean equals(Object obj)
	{
		if(!(obj instanceof ComplexNumberNode))
			return false;
		ComplexNumberNode other = (ComplexNumberNode) obj;
		return real==other.real && imaginary==other.imaginary;
	}
}
