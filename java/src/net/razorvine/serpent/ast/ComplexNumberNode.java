package net.razorvine.serpent.ast;

class ComplexNumberNode implements INode
{
	public double realpart;
	public double imaginary;
	
	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}

	@Override
	public String toString()
	{
		String strReal = ""+realpart;
		String strImag = ""+imaginary;
		if(imaginary>=0)
			return String.format("({0}+{1}j)", strReal, strImag);
		return String.format("({0}{1}j)", strReal, strImag);
	}
}
