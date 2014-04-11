/// <summary>
/// Serpent, a Python literal expression serializer/deserializer
/// (a.k.a. Python's ast.literal_eval in .NET)
///
/// Copyright 2014, Irmen de Jong (irmen@razorvine.net)
/// Software license: "MIT software license". See http://opensource.org/licenses/MIT
/// </summary>

using System;

namespace Razorvine.Serpent
{
	/// <summary>
	/// A problem occurred during parsing.
	/// </summary>
	public class ParseException : Exception
	{
		public ParseException()
		{
		}

	 	public ParseException(string message) : base(message)
		{
		}

		public ParseException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}