package net.razorvine.serpent.ast;

public interface INodeVisitor
{
	void Visit(ComplexNumberNode complex);
	void Visit(DictNode dict);
	void Visit(ListNode list);
	void Visit(NoneNode none);
	void Visit(IntegerNode value);
	void Visit(LongNode value);
	void Visit(DoubleNode value);
	void Visit(BooleanNode value);
	void Visit(StringNode value);
	void Visit(SetNode setnode);
	void Visit(TupleNode tuple);
}
