/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent.ast;

import net.razorvine.serpent.IDictToInstance;
import net.razorvine.serpent.ObjectifyVisitor;

/**
 * Abstract syntax tree for the literal expression. This is what the parser returns.
 */
public class Ast
{
	public INode root;
	
	@Override
	public String toString()
	{
		return "# serpent utf-8 .net\n" + root.toString();
	}

	/**
	 * get the actual data as Java objects.
	 */
	public Object getData()
	{
		ObjectifyVisitor v = new ObjectifyVisitor();
		this.accept(v);
		return v.getObject();
	}

	/**
	 * get the actual data as Java objects.
	 * @param dictConverter object to convert dicts to actual instances for a class,
	 *   instead of leaving them as dictionaries. Requires the __class__ key to be present
	 *   in the dict node. If it returns null, the normal processing is done.
	 */
	public Object getData(IDictToInstance dictConverter)
	{
		ObjectifyVisitor v = new ObjectifyVisitor(dictConverter);
		this.accept(v);
		return v.getObject();
	}

	public void accept(INodeVisitor visitor)
	{
		root.accept(visitor);
	}
}
