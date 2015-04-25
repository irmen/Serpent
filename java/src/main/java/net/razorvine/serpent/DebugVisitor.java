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
	private int indentlevel=0;
	
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
	
	protected void indent()
	{
		for(int i=0; i<indentlevel; ++i)
			result.append("    ");
	}
	
	public void visit(ComplexNumberNode complex)
	{
		result.append(String.format("complexnumber (%sr,%si)", complex.real, complex.imaginary));
	}
	
	public void visit(DictNode dict)
	{
		result.append("(dict\n");
		indentlevel++;
		for(INode e: dict.elements)
		{
			indent();
			KeyValueNode kv = (KeyValueNode) e;
			kv.key.accept(this);
			result.append(" = ");
			kv.value.accept(this);
			result.append(",\n");
		}
		indentlevel--;
		indent();
		result.append(")");
	}
	
	public void visit(ListNode list)
	{
		result.append("(list\n");
		indentlevel++;
		for(INode node: list.elements)
		{
			indent();
			node.accept(this);
			result.append(",\n");
		}
		indentlevel--;
		indent();
		result.append(")");
	}
	
	public void visit(NoneNode none)
	{
		result.append("None");
	}
	
	public void visit(IntegerNode value)
	{
		result.append(String.format("int %s", value.value));
	}
	
	public void visit(BigIntNode value)
	{
		result.append(String.format("bigint %s", value.value));
	}

	public void visit(LongNode value)
	{
		result.append(String.format("long %s", value.value));
	}
	
	public void visit(DoubleNode value)
	{
		result.append(String.format("double %s", value.value));
	}
	
	public void visit(BooleanNode value)
	{
		result.append(String.format("bool %s", value.value));
	}
	
	public void visit(StringNode value)
	{
		result.append(String.format("string '%s'", value.value));
	}
	
	public void visit(SetNode setnode)
	{
		result.append("(set\n");
		indentlevel++;
		for(INode node: setnode.elements)
		{
			indent();
			node.accept(this);
			result.append(",\n");
		}
		indentlevel--;
		indent();
		result.append(")");
	}
	
	public void visit(TupleNode tuple)
	{
		result.append("(tuple\n");
		indentlevel++;
		for(INode node: tuple.elements)
		{
			indent();
			node.accept(this);
			result.append(",\n");
		}
		indentlevel--;
		indent();
		result.append(")");
	}
}
