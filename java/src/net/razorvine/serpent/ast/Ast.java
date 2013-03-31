/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent.ast;

/// <summary>
/// Abstract syntax tree for the literal expression. This is what the parser returns.
/// </summary>
public class Ast
{
	public INode Root;
	
	@Override
	public String toString()
	{
		return "# serpent utf-8 .net\n" + Root.toString();
	}

	public void Accept(INodeVisitor visitor)
	{
		Root.Accept(visitor);
	}
}
