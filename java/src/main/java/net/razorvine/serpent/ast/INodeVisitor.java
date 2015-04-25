package net.razorvine.serpent.ast;

public interface INodeVisitor
{
	void visit(ComplexNumberNode complex);
	void visit(DictNode dict);
	void visit(ListNode list);
	void visit(NoneNode none);
	void visit(IntegerNode value);
	void visit(LongNode value);
	void visit(DoubleNode value);
	void visit(BooleanNode value);
	void visit(StringNode value);
	void visit(SetNode setnode);
	void visit(TupleNode tuple);
	void visit(BigIntNode value);
}
