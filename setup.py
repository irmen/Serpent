"""
Serpent: ast.literal_eval() compatible object tree serialization.

Copyright 2013, Irmen de Jong (irmen@razorvine.net)
This code is open-source, but licensed under the "MIT software license".
See http://opensource.org/licenses/MIT
"""
from distutils.core import setup

import serpent

setup(
    name='serpent',
    version=serpent.__version__,
    py_modules = ["serpent"],
    license='MIT',
    author='Irmen de Jong',
    author_email='irmen@razorvine.net',
    description='Serialization based on ast.literal_eval',
    long_description="""Serpent is a simple serialization library based on ast.literal_eval.

    Because it only serializes literals and recreates the objects using ast.literal_eval(),
    the serialized data is safe to transport to other machines over the network for instance.

    API:

    - ``ser_bytes = serpent.serialize(obj, indent=False)``      # serialize obj tree to bytes
    - ``obj = serpent.deserialize(ser_bytes)``     # deserialize bytes back into object tree
    - You can use ``ast.literal_eval`` yourself to deserialize, but ``serpent.deserialize`` works around a few corner cases. See source for details.

    Serpent is more sophisticated than a simple repr() + literal_eval():

    - it serializes directly to bytes (utf-8 encoded), instead of a string
    - it encodes byte-types as base-64 instead of inefficient escaping notation (this does mean you have
      to base-64 decode these strings manually on the receiving side to get your bytes back)
    - it contains a few custom serializers for several additional Python types such as uuid, datetime, array and decimal
    - it tries to serialize all other types in a sensible manner into a dict (you can control this with __getstate__ on your own types)
    - it can create a pretty-printed (indented) output for readability purposes

    It works with Python 2.6+ (including 3.x), IronPython 2.7+, Jython 2.7+.
    """,
    keywords="serialization",
    platforms="any",
    classifiers= [
        "Development Status :: 4 - Beta",
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        "Natural Language :: English",
        "Operating System :: OS Independent",
        "Programming Language :: Python",
        "Programming Language :: Python :: 3",
        "Topic :: Software Development"
    ],

)
