package net.razorvine.serpent.ast;

import java.util.List;

public class BytesNode extends PrimitiveNode<List<Byte>>
{
	public BytesNode(List<Byte> value)
	{
		super(value);
	}

	@Override
	public void accept(INodeVisitor visitor)
	{
		visitor.visit(this);
	}

	public byte[] toByteArray() {
		byte[] bytes = new byte[value.size()];
		for(int i=0; i<value.size(); ++i) {
			bytes[i] = value.get(i);
		}
		return bytes;
	}
}
