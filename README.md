Serpent serialization library (Python/.NET/Java)
================================================

[![Latest Version](https://img.shields.io/pypi/v/Serpent.svg)](https://pypi.python.org/pypi/Serpent/)
[![Maven Central](https://img.shields.io/maven-central/v/net.razorvine/serpent.svg)](http://search.maven.org/#search|ga|1|g%3A%22net.razorvine%22%20AND%20a%3A%22serpent%22)
[![NuGet](https://img.shields.io/nuget/v/Razorvine.Serpent.svg)](https://www.nuget.org/packages/Razorvine.Serpent/)
[![Anaconda-Server Badge](https://anaconda.org/conda-forge/serpent/badges/version.svg)](https://anaconda.org/conda-forge/serpent)

Serpent provides ast.literal_eval() compatible object tree serialization.
It serializes an object tree into bytes (utf-8 encoded string) that can be decoded and then
passed as-is to ast.literal_eval() to rebuild it as the original object tree.
As such it is safe to send serpent data to other machines over the network for instance
(because only 'safe' literals are encoded).

More info on Pypi: https://pypi.python.org/pypi/serpent
Source code is on Github: https://github.com/irmen/Serpent

Copyright by Irmen de Jong (irmen@razorvine.net)
This software is released under the MIT software license.
This license, including disclaimer, is available in the 'LICENSE' file.


PYTHON
------
Compatible with Python 3.7+ (use a serpent version before 1.30 for Python 2.7 support)
It can be found on Pypi as 'serpent': https://pypi.python.org/pypi/serpent
Example usage can be found in ./tests/example.py


C#/.NET
-------
Package is available on www.nuget.org as 'Razorvine.Serpent'.
Full source code can be found in ./dotnet/ directory.
Example usage can be found in ./dotnet/Serpent.Test/Example.cs
The project is a dotnet core project targeting NetStandard 2.0.


JAVA
----
Maven-artefact is available on maven central, groupid 'net.razorvine' artifactid 'serpent'.
Full source code can be found in ./java/ directory.
Example usage can be found in ./java/test/SerpentExample.java
Versions before 1.23 require Java 7 or Java 8 (JDK 1.7 or 1.8) to compile and run.
Version 1.23 and later require Java 8 (JDK 1.8) at a minimum to compile and run.


SOME MORE DETAILS
-----------------
Serpent handles several special Python types to make life easier:

 - bytes, bytearrays, memoryview --> string, base-64 (or bytes-literal if selected)
   (you'll have to manually un-base64. Can use serpent.tobytes function for that.)
 - uuid.UUID, datetime.{datetime, date, time, timespan}  --> appropriate string/number
 - decimal.Decimal  --> string (to not lose precision)
 - array.array typecode 'u' --> string
 - array.array other typecode --> list
 - Exception  --> dict with some fields of the exception (message, args)
 - collections module types  --> mostly equivalent primitive types or dict
 - enums --> the value of the enum
 - namedtuple --> treated as just a tuple
 - attr dataclasses and python 3.7 native dataclasses: treated as just a class, so will become a dict
 - all other types  --> dict with the ``__getstate__`` or ``vars()`` of the object, and a ``__class__`` element with the name of the class

Notes:

The serializer is not thread-safe. Make sure you're not making changes
to the object tree that is being serialized, and don't use the same
serializer in different threads.

Because the serialized format is just valid Python source code, it can
contain comments. Serpent does not add comments by itself apart from the
single header line.

Floats +inf and -inf are handled via a trick, Float 'nan' cannot be handled
and is represented by the special value:  ``{'__class__':'float','value':'nan'}``
We chose not to encode it as just the string 'NaN' because that could cause
memory issues when used in multiplications.
