/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent;

import net.razorvine.serpent.ast.*;


/**
 * Ast nodevisitor that prints out the Ast as a string for debugging purposes
 */
public class DebugVisitor implements INodeVisitor
{
	private StringBuilder result = new StringBuilder();
	private int indent=0;
	
	public DebugVisitor()
	{
	}
	
	/**
	 * Get the debug string representation result.
	 */
	@Override
	public String toString()
	{
		return result.toString();
	}
	
	protected void Indent()
	{
		for(int i=0; i<indent; ++i)
			result.append("    ");
	}
	
	public void visit(ComplexNumberNode complex)
	{
		result.append(String.format("complexnumber {0}r {1}i\n", complex.realpart, complex.imaginary));
	}
	
	public void visit(DictNode dict)
	{
		result.append("(dict\n");
		indent++;
		for(INode e: dict.elements)
		{
			Indent();
			KeyValueNode kv = (KeyValueNode) e;
			kv.key.accept(this);
			result.append(" = ");
			kv.value.accept(this);
			result.append(",\n");
		}
		indent--;
		Indent();
		result.append(")");
	}
	
	public void visit(ListNode list)
	{
		result.append("(list\n");
		indent++;
		for(INode node: list.elements)
		{
			Indent();
			node.accept(this);
		}
		indent--;
		Indent();
		result.append(")");
	}
	
	public void visit(NoneNode none)
	{
		result.append("None");
	}
	
	public void visit(IntegerNode value)
	{
		result.append(String.format("int {0}", value.value));
	}
	
	public void visit(LongNode value)
	{
		result.append(String.format("long {0}", value.value));
	}
	
	public void visit(DoubleNode value)
	{
		result.append(String.format("double {0}", value.value));
	}
	
	public void visit(BooleanNode value)
	{
		result.append(String.format("bool {0}", value.value));
	}
	
	public void visit(StringNode value)
	{
		result.append(String.format("string '{0}'", value.value));
	}
	
	public void visit(SetNode setnode)
	{
		result.append("(set\n");
		indent++;
		for(INode node: setnode.elements)
		{
			Indent();
			node.accept(this);
			result.append(",\n");
		}
		indent--;
		Indent();
		result.append(")");
	}
	
	public void visit(TupleNode tuple)
	{
		result.append("(tuple\n");
		indent++;
		for(INode node: tuple.elements)
		{
			Indent();
			node.accept(this);
			result.append(",\n");
		}
		indent--;
		Indent();
		result.append(")");
	}
}
