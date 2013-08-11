Serpent serialization library (Python/.NET/Java)
------------------------------------------------
Serpent provides ast.literal_eval() compatible object tree serialization.
It serializes an object tree into bytes (utf-8 encoded string) that can be decoded and then
passed as-is to ast.literal_eval() to rebuild it as the original object tree.
As such it is safe to send serpent data to other machines over the network for instance
(because only 'safe' literals are encoded).

More info on Pypi: https://pypi.python.org/pypi/serpent
Source code is on Github: https://github.com/irmen/Serpent

Copyright 2013, Irmen de Jong (irmen@razorvine.net)
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


