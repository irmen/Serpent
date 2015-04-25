/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent;

import java.util.Map;

/**
 * Customization interface for serializing objects into dicts.
 */
public interface IClassSerializer {
	public Map<String,Object> convert(Object obj);
}
