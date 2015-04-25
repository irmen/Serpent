package net.razorvine.serpent.ast;

import java.math.BigInteger;


public class BigIntNode extends PrimitiveNode<BigInteger> {

	public BigIntNode(BigInteger value) {
		super(value);
	}

	@Override
	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}
}
