/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent;

import java.io.IOException;
import java.util.Map;

/**
 * Customization interface for turning dicts back into specific objects.
 */
public interface IDictToInstance {
	/**
	 * Convert the given dictionary to a specific object.
	 * Can return null to use the default behavior.
	 */
	public Object convert(Map<Object,Object> dict) throws IOException;
}
