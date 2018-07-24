# -*- coding: utf-8 -*-
# Serpent: ast.literal_eval() compatible object tree serialization.
# Copyright 2013, Irmen de Jong (irmen@razorvine.net)
# Software license: "MIT software license". See http://opensource.org/licenses/MIT

try:
    from setuptools import setup
    using_setuptools = True
except ImportError:
    from distutils.core import setup
    using_setuptools = False

import serpent

setup(
    name='serpent',
    version=serpent.__version__,
    py_modules=["serpent"],
    license='MIT',
    author='Irmen de Jong',
    author_email='irmen@razorvine.net',
    description='Serialization based on ast.literal_eval',
    long_description="""
Serpent is a simple serialization library based on ast.literal_eval.

Because it only serializes literals and recreates the objects using ast.literal_eval(),
the serialized data is safe to transport to other machines (over the network for instance)
and de-serialize it there.

*There is also a Java and a .NET (C#) implementation available. This allows for easy data transfer between the various ecosystems.
You can get the full source distribution, a Java .jar file, and a .NET assembly dll.*
The java library can be obtained from Maven central (groupid ``net.razorvine`` artifactid ``serpent``),
and the .NET assembly can be obtained from Nuget.org (package ``Razorvine.Serpent``).


**API**

- ``ser_bytes = serpent.dumps(obj, indent=False, set_literals=True, module_in_classname=False):``      # serialize obj tree to bytes
- ``obj = serpent.loads(ser_bytes)``     # deserialize bytes back into object tree
- You can use ``ast.literal_eval`` yourself to deserialize, but ``serpent.deserialize``
  works around a few corner cases. See source for details.

Serpent is more sophisticated than a simple repr() + literal_eval():

- it serializes directly to bytes (utf-8 encoded), instead of a string, so it can immediately be saved to a file or sent over a socket
- it encodes byte-types as base-64 instead of inefficient escaping notation that repr would use (this does mean you have
  to base-64 decode these strings manually on the receiving side to get your bytes back.
  You can use the serpent.tobytes utility function for this.)
- it contains a few custom serializers for several additional Python types such as uuid, datetime, array and decimal
- it tries to serialize unrecognised types as a dict (you can control this with __getstate__ on your own types)
- it can create a pretty-printed (indented) output for readability purposes
- it outputs the keys of sets and dicts in alphabetical order (when pretty-printing)
- it works around a few quirks of ast.literal_eval() on the various Python implementations

Serpent allows comments in the serialized data (because it is just Python source code).
Serpent can't serialize object graphs (when an object refers to itself); it will then crash with a ValueError pointing out the problem.

Works with Python 2.7+ (including 3.x), IronPython 2.7+, Jython 2.7+.

**FAQ**

- Why not use XML? Answer: because XML.
- Why not use JSON? Answer: because JSON is quite limited in the number of datatypes it supports, and you can't use comments in a JSON file.
- Why not use pickle? Answer: because pickle has security problems.
- Why not use ``repr()``/``ast.literal_eval()``? See above; serpent is a superset of this and provides more convenience.
  Serpent provides automatic serialization mappings for types other than the builtin primitive types.
  ``repr()`` can't serialize these to literals that ``ast.literal_eval()`` understands.
- Why not a binary format? Answer: because binary isn't readable by humans.
- But I don't care about readability. Answer: doesn't matter, ``ast.literal_eval()`` wants a literal string, so that is what we produce.
- But I want better performance. Answer: ok, maybe you shouldn't use serpent in this case. Find an efficient binary protocol (protobuf?)
- Why only Python, Java and C#/.NET, but no bindings for insert-favorite-language-here? Answer: I don't speak that language.
  Maybe you could port serpent yourself?
- Where is the source?  It's on Github: https://github.com/irmen/Serpent
- Can I use it everywhere?  Sure, as long as you keep the copyright and disclaimer somewhere. See the LICENSE file.

**Demo**

.. code:: python

 # This demo script is written for Python 3.2+
 # -*- coding: utf-8 -*-
 from __future__ import print_function
 import ast
 import uuid
 import datetime
 import pprint
 import serpent


 class DemoClass:
     def __init__(self):
         self.i=42
         self.b=False

 data = {
     "names": ["Harry", "Sally", "Peter"],
     "big": 2**200,
     "colorset": { "red", "green" },
     "id": uuid.uuid4(),
     "timestamp": datetime.datetime.now(),
     "class": DemoClass(),
     "unicode": "€"
 }

 # serialize it
 ser = serpent.dumps(data, indent=True)
 open("data.serpent", "wb").write(ser)

 print("Serialized form:")
 print(ser.decode("utf-8"))

 # read it back
 data = serpent.load(open("data.serpent", "rb"))
 print("Data:")
 pprint.pprint(data)

 # you can also use ast.literal_eval if you like
 ser_string = open("data.serpent", "r", encoding="utf-8").read()
 data2 = ast.literal_eval(ser_string)

 assert data2==data


When you run this (with python 3.2+) it prints:

.. code:: python

 Serialized form:
 # serpent utf-8 python3.2
 {
   'big': 1606938044258990275541962092341162602522202993782792835301376,
   'class': {
     '__class__': 'DemoClass',
     'b': False,
     'i': 42
   },
   'colorset': {
     'green',
     'red'
   },
   'id': 'e461378a-201d-4844-8119-7c1570d9d186',
   'names': [
     'Harry',
     'Sally',
     'Peter'
   ],
   'timestamp': '2013-04-02T00:23:00.924000',
   'unicode': '€'
 }
 Data:
 {'big': 1606938044258990275541962092341162602522202993782792835301376,
  'class': {'__class__': 'DemoClass', 'b': False, 'i': 42},
  'colorset': {'green', 'red'},
  'id': 'e461378a-201d-4844-8119-7c1570d9d186',
  'names': ['Harry', 'Sally', 'Peter'],
  'timestamp': '2013-04-02T00:23:00.924000',
  'unicode': '€'}
    """,

    keywords="serialization",
    platforms="any",
    classifiers=[
        "Development Status :: 5 - Production/Stable",
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        "Natural Language :: English",
        "Operating System :: OS Independent",
        "Programming Language :: Python",
        "Programming Language :: Python :: 2.7",
        "Programming Language :: Python :: 3.4",
        "Programming Language :: Python :: 3.5",
        "Programming Language :: Python :: 3.6",
        "Programming Language :: Python :: 3.7",
        "Topic :: Software Development"
    ],
    tests_require=['enum34; python_version < "3.4"']
)
