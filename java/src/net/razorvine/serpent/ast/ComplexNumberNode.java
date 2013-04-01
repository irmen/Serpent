package net.razorvine.serpent.ast;

public class ComplexNumberNode implements INode
{
	public double real;
	public double imaginary;
	
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
			return String.format("({0}+{1}j)", strReal, strImag);
		return String.format("({0}{1}j)", strReal, strImag);
	}
}
