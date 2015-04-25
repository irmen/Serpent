/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent;

import java.io.IOException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.Stack;
import net.razorvine.serpent.ast.*;

/**
 * Ast nodevisitor that turns the AST into actual Java objects (array, int, IDictionary, string, etc...)
 */
public class ObjectifyVisitor implements INodeVisitor
{
	Stack<Object> generated = new Stack<Object>();
	protected IDictToInstance dictConverter = null;
	
	public ObjectifyVisitor() {
	}
	
	public ObjectifyVisitor(IDictToInstance dictConverter) {
		this.dictConverter = dictConverter;
	}

	/**
	 * get the resulting object tree.
	 */
	public Object getObject()
	{
		return generated.pop();
	}

	public void visit(ComplexNumberNode complex)
	{
		generated.push(new ComplexNumber(complex.real, complex.imaginary));
	}

	public void visit(DictNode dict)
	{
		Map<Object, Object> obj = new HashMap<Object, Object>(dict.elements.size());
		for(INode e: dict.elements)
		{
			KeyValueNode kv = (KeyValueNode)e;
			kv.key.accept(this);
			Object key = generated.pop();
			kv.value.accept(this);
			Object value = generated.pop();
			obj.put(key, value);
		}

		if(dictConverter==null || !obj.containsKey("__class__"))
		{
			generated.push(obj);
		}
		else
		{
			Object result;
			try {
				result = dictConverter.convert(obj);
			} catch (IOException e) {
				throw new RuntimeException("problem converting dict to class", e);
			}
			if(result==null)
				generated.push(obj);
			else
				generated.push(result);
		}
	}

	public void visit(ListNode list)
	{
		List<Object> obj = new ArrayList<Object>(list.elements.size());
		for(INode node: list.elements)
		{
			node.accept(this);
			obj.add(generated.pop());
		}
		generated.push(obj);
	}

	public void visit(NoneNode none)
	{
		generated.push(null);
	}

	public void visit(IntegerNode value)
	{
		generated.push(value.value);
	}

	public void visit(LongNode value)
	{
		generated.push(value.value);
	}

	public void visit(DoubleNode value)
	{
		generated.push(value.value);
	}

	public void visit(BooleanNode value)
	{
		generated.push(value.value);
	}

	public void visit(StringNode value)
	{
		generated.push(value.value);
	}

	public void visit(BigIntNode value)
	{
		generated.push(value.value);
	}

	public void visit(SetNode setnode)
	{
		Set<Object> obj = new HashSet<Object>();
		for(INode node: setnode.elements)
		{
			node.accept(this);
			obj.add(generated.pop());
		}
		generated.push(obj);
	}

	public void visit(TupleNode tuple)
	{
		Object[] array = new Object[tuple.elements.size()];
		int index=0;
		for(INode node: tuple.elements)
		{
			node.accept(this);
			array[index++] = generated.pop();
		}
		generated.push(array);
	}
	
}
