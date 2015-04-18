Serpent serialization library (Python/.NET/Java)
------------------------------------------------
Serpent provides ast.literal_eval() compatible object tree serialization.
It serializes an object tree into bytes (utf-8 encoded string) that can be decoded and then
passed as-is to ast.literal_eval() to rebuild it as the original object tree.
As such it is safe to send serpent data to other machines over the network for instance
(because only 'safe' literals are encoded).

More info on Pypi: https://pypi.python.org/pypi/serpent
Source code is on Github: https://github.com/irmen/Serpent

Copyright 2013, 2014 by Irmen de Jong (irmen@razorvine.net)
Software license: "MIT software license". See http://opensource.org/licenses/MIT


PYTHON
------
Package can be found on Pypi as 'serpent': https://pypi.python.org/pypi/serpent
Example usage can be found in ./example.py


C#/.NET
-------
Full source code can be found in ./dotnet/ directory.
Example usage can be found in ./dotnet/Serpent.Test/Example.cs


JAVA
----
Full source code can be found in ./java/ directory.
Example usage can be found in ./java/test/SerpentExample.java


SOME MORE DETAILS
-----------------
Compatible with Python 2.6+ (including 3.x), IronPython 2.7+, Jython 2.7+.

Serpent handles several special Python types to make life easier:

 - str  --> promoted to unicode (see below why this is)
 - bytes, bytearrays, memoryview, buffer  --> string, base-64
   (you'll have to manually un-base64 them though)
 - uuid.UUID, datetime.{datetime, time, timespan}  --> appropriate string/number
 - decimal.Decimal  --> string (to not lose precision)
 - array.array typecode 'c'/'u' --> string/unicode
 - array.array other typecode --> list
 - Exception  --> dict with some fields of the exception (message, args)
 - collections module types  --> mostly equivalent primitive types or dict
 - all other types  --> dict with the __getstate__ or vars() of the object

Notes:

All str will be promoted to unicode. This is done because it is the
default anyway for Python 3.x, and it solves the problem of the str/unicode
difference between different Python versions. Also it means the serialized
output doesn't have those problematic 'u' prefixes on strings.

The serializer is not thread-safe. Make sure you're not making changes
to the object tree that is being serialized, and don't use the same
serializer in different threads.

Python 2.6 cannot deserialize complex numbers at all (limitation of
ast.literal_eval in 2.6).

Because the serialized format is just valid Python source code, it can
contain comments. Serpent does not add comments by itself apart from the
single header line.

Set literals are not supported on python <3.2 (ast.literal_eval
limitation). If you need Python < 3.2 compatibility, you'll have to use
set_literals=False when serializing. Since version 1.6 serpent chooses
this wisely for you by default, but you can still override it if needed.
