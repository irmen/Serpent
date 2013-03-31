package net.razorvine.serpent.ast;

class ComplexNumberNode implements INode
{
	public double Real;
	public double Imaginary;
	
	public void Accept(INodeVisitor visitor)
	{
		visitor.Visit(this);
	}

	@Override
	public String toString()
	{
		String strReal = ""+Real;
		String strImag = ""+Imaginary;
		if(Imaginary>=0)
			return String.format("({0}+{1}j)", strReal, strImag);
		return String.format("({0}{1}j)", strReal, strImag);
	}
}
