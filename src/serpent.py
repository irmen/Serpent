"""
Serpent: ast.literal_eval() compatible object tree serialization.
Serializes an object tree into bytes (utf-8 encoded string) that can be decoded and then
passed as-is to ast.literal_eval() to turn them back into the original object tree.
Compatible with Python 2.6-3.x.

BIG GOTCHA:
For python 2.x it will add a 'u' in front of the unicode literals.
Without the 'u', python 2.x is unable to parse these strings correctly into unicode objects.
This is a problem because it will break Python 3.0 to 3.2 (these don't understand the 'u').
The other way round is a similar problem: Python 3.x will encode all strings as unicode,
without a 'u' prefix. Python 2.x can't correctly parse these strings.

For now, the deserializer checks the python version used to serialize the data and
decides if it can reliably deserialize it. It will raise an error if it can't.
A possible improvement is to ast.walk() the tree and monkeypatch the string literals
if the deserializer detects an invalid version combination....?
OR...... compile with compiler flag unicode_literal and treat ALL strings as unicode...?

@TODO: IronPython has str==unicode; strs are not encoded properly.
@TODO: test.
@TODO: decide if decimal should rather be encoded as string, to avoid losing precision?
@TODO: java and C# implementations.

Copyright 2013, Irmen de Jong (irmen@razorvine.net)
This code is open-source, but licensed under the "MIT software license".
"""
import ast
import base64
import sys
import types
if sys.platform=="cli":
    from io import BytesIO   # IronPython
else:
    try:
        from cStringIO import StringIO as BytesIO   # python 2.x
    except ImportError:
        from io import BytesIO   # python 3.x

__all__ = ["serialize", "deserialize", "Bytes"]


def serialize(obj, indent=False):
    out = BytesIO()
    StreamSerializer(out, indent).serialize(obj)
    return out.getvalue()


def deserialize(serialized_bytes):
    string = serialized_bytes.decode("utf-8")
    if string.startswith("# serpent "):
        # version check
        header = string[:30]
        ser_version = header[header.index("python")+6:].split()[0].split(".")
        ser_version = (int(ser_version[0]), int(ser_version[1]))
        my_version = sys.version_info[:2]
        if ser_version[0] != my_version[0]:
            # tackle possible version problem: major python versions are different
            if my_version[0] == 2:
                # python 2.x is reading a python 3.x structure, not yet supported
                raise ValueError("serpent version mismatch, python-2.x cannot parse python-3.x serpent data yet")
                # XXX ... ast.walk() to monkeypatch string literals when version mismatch? or compiler flags? (see GOTCHA at top of file)
            if ser_version[0] == 2:
                if my_version < (3, 3):
                   # python 3.0-3.2 cannot parse strings with 'u' prefixes
                    raise RuntimeError("upgrade to python-3.3 or later to be able to parse python-2.x serpent data")
    return ast.literal_eval(string)


class Bytes(object):
    """Wrapper for bytes, bytearray etc. to make them appear as base-64 encoded data."""
    def __init__(self, data):
        self.data = data
    def __getstate__(self):
        b64 = base64.b64encode(self.data)
        return {
            "data": b64 if type(b64) is str else b64.decode("ascii"),
            "encoding": "base64"
        }
    @staticmethod
    def from_bytes(data):
        return Bytes(data)
    @staticmethod
    def from_bytearray(data):
        return Bytes(data)
    @staticmethod
    def from_memoryview(data):
        return Bytes(data.tobytes())
    @staticmethod
    def from_buffer(data):
        return Bytes(data)


class StreamSerializer(object):
    """Serpent stream serializer. Serialize an object tree to a byte stream."""
    repr_types = {
        str,
        int,
        float,
        complex,
        bool,
        type(None)
    }

    translate_types = {
        bytes: Bytes.from_bytes,
        bytearray: Bytes.from_bytearray,
        memoryview: Bytes.from_memoryview,
        }

    if bytes is str:
        del translate_types[bytes]
    if sys.version_info < (3, 0):
        # fix some Python 2.x types
        if hasattr(types, "BufferType"):
            translate_types[types.BufferType] = Bytes.from_buffer

    def __init__(self, out, indent=False):
        """
        Create the serializer.
        out=bytestream that the output should be written to,
        indent=indent the output over multiple lines (default=false)
        """
        self.out = out
        self.indent = indent

    def serialize(self, obj):
        """Serialize the object tree to the output stream."""
        header = "# serpent utf-8 python{0}.{1}\n".format(*sys.version_info)
        self.out.write(header.encode("utf-8"))
        self._serialize(obj, self.out, 0)

    def _serialize(self, obj, out, level):
        t = type(obj)
        if t in self.translate_types:
            obj = self.translate_types[t](obj)
            t = type(obj)
        if t in self.repr_types:
            out.write(repr(obj).encode("utf-8"))    # just a simple repr() is enough for these objects
        elif isinstance(obj, BaseException):
            self.ser_exception_class(obj, out, level)
        else:
            module = t.__module__
            if module == "__builtin__":
                module = "builtins"  # python 2.x compatibility
            method = "ser_{0}_{1}".format(module, t.__name__)
            getattr(self, method, self.ser_default_class)(obj, out, level)

    def ser_builtins_unicode(self, unicode_obj, out, level):
        # for python 2.x.
        # Note: adds 'u' in front of the literal. SEE NOTE AT TOP OF FILE FOR DISCUSSION.
        z = unicode_obj.encode("utf-8")
        z = z.replace(b"\\", b"\\\\")  # double-escape the backslashes
        if b"'" not in z:
            z = b"u'" + z + b"'"
        elif '"' not in z:
            z = b'u"' + z + b'"'
        else:
            z = z.replace(b"'", b"\\'")
            z = b"u'" + z + b"'"
        out.write(z)
    
    def ser_builtins_long(self, long_obj, out, level):
        out.write(str(long_obj).encode("utf-8"))        # for python 2.x

    def ser_builtins_tuple(self, tuple_obj, out, level):
        if self.indent and tuple_obj:
            indent_chars = b"  "*level
            out.write(b"(\n")
            for elt in tuple_obj:
                out.write(indent_chars + b"  ")
                self._serialize(elt, out, level+1)
                out.write(b",\n")
            out.seek(-1, 1)  # undo the last \n
            if len(tuple_obj) > 1:
                out.seek(-1, 1)  # undo the last ,
            out.write(b"\n" + indent_chars + b")")
        else:
            out.write(b"(")
            for elt in tuple_obj:
                self._serialize(elt, out, level+1)
                out.write(b",")
            if len(tuple_obj) > 1:
                out.seek(-1, 1)  # undo the last ,
            out.write(b")")

    def ser_builtins_list(self, list_obj, out, level):
        if self.indent and list_obj:
            indent_chars = b"  "*level
            out.write(b"[\n")
            for elt in list_obj:
                out.write(indent_chars + b"  ")
                self._serialize(elt, out, level+1)
                out.write(b",\n")
            out.seek(-2, 1)  # undo the last ,\n
            out.write(b"\n" + indent_chars + b"]")
        else:
            out.write(b"[")
            for elt in list_obj:
                self._serialize(elt, out, level+1)
                out.write(b",")
            if list_obj:
                out.seek(-1, 1)  # undo the last ,
            out.write(b"]")

    def ser_builtins_dict(self, dict_obj, out, level):
        if self.indent and dict_obj:
            indent_chars = b"  "*level
            out.write(b"{\n")
            for k, v in dict_obj.items():
                out.write(indent_chars + b"  ")
                self._serialize(k, out, level+1)
                out.write(b": ")
                self._serialize(v, out, level+1)
                out.write(b",\n")
            out.seek(-2, 1)  # undo the last ,\n
            out.write(b"\n" + indent_chars + b"}")
        else:
            out.write(b"{")
            for k, v in dict_obj.items():
                self._serialize(k, out, level+1)
                out.write(b":")
                self._serialize(v, out, level+1)
                out.write(b",")
            if dict_obj:
                out.seek(-1, 1)  # undo the last ,
            out.write(b"}")

    def ser_builtins_set(self, set_obj, out, level):
        if self.indent and set_obj:
            indent_chars = b"  "*level
            out.write(b"{\n")
            for elt in set_obj:
                out.write(indent_chars + b"  ")
                self._serialize(elt, out, level+1)
                out.write(b",\n")
            out.seek(-2, 1)  # undo the last ,\n
            out.write(b"\n" + indent_chars + b"}")
        elif set_obj:
            out.write(b"{")
            for elt in set_obj:
                self._serialize(elt, out, level+1)
                out.write(b",")
            out.write(b"}")
        else:
            # empty set literal doesn't exist unfortunately, replace with empty tuple
            self.ser_builtins_tuple((), out, level)

    def ser_builtins_frozenset(self, set_obj, out, level):
        self.ser_builtins_set(set_obj, out, level)

    def ser_decimal_Decimal(self, decimal_obj, out, level):
        # NOTE: the decimal is serialized as a normal number
        # this means that the deserializer will loose precision!
        # (the number will be parsed as a regular float)
        # @TODO: decide if this should be serialized as a string instead?
        out.write(str(decimal_obj).encode("utf-8"))

    def ser_datetime_datetime(self, datetime_obj, out, level):
        self._serialize(datetime_obj.isoformat(), out, level)

    def ser_datetime_timedelta(self, timedelta_obj, out, level):
        self._serialize(timedelta_obj.total_seconds(), out, level)

    def ser_datetime_time(self, time_obj, out, level):
        self._serialize(str(time_obj), out, level)

    def ser_uuid_UUID(self, uuid_obj, out, level):
        self._serialize(str(uuid_obj), out, level)

    def ser_exception_class(self, exc_obj, out, level):
        value = {
            "__class__": type(exc_obj).__name__,
            "__exception__": True,
            "args": exc_obj.args,
            "message": str(exc_obj)
        }
        self._serialize(value, out, level)

    def ser_array_array(self, array_obj, out, level):
        self._serialize(array_obj.tolist(), out, level)

    def ser_default_class(self, obj, out, level):
        try:
            value = obj.__getstate__()
            if isinstance(value, dict):
                self.ser_builtins_dict(value, out, level)
                return
        except AttributeError:
            try:
                value = dict(vars(obj))  # make sure we can serialize anything that resembles a dict
                value["__class__"] = type(obj).__name__
            except TypeError:
                raise TypeError("don't know how to serialize class " + str(type(obj)) + ". Give it vars() or an appropriate __getstate__")
        self._serialize(value, out, level)
