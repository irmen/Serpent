from distutils.core import setup

setup(
    name='Serpent',
    version='0.1',
    py_modules = ["serpent"],
    url='http://packages.python.org/Serpent',
    license='MIT',
    author='Irmen de Jong',
    author_email='irmen@razorvine.net',
    description='Serialization based on ast.literal_eval',
    long_description="""Serpent is a simple serialization library based on ast.literal_eval.

    Because it only serializes literals and recreates the objects using ast.literal_eval,
    the serialized data is safe to transport to other machines over the network for instance.

    Serpent is more sophisticated than a simple repr()/literal_eval(); it contains
    a few custom serializers for some additional Python types and also tries to
    serialize all other types in a sensible manner into a dict.
    """,
    keywords="serialization",
    platforms="any",
    classifiers= [
        "Development Status :: 3 - Alpha",
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        "Natural Language :: English",
        "Operating System :: OS Independent",
        "Programming Language :: Python",
        "Programming Language :: Python :: 3",
    ],

)
