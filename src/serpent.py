"""
Serpent: ast.literal_eval() compatible object tree serialization.
Serializes an object tree into bytes (utf-8 encoded string) that can be decoded and then
passed as-is to ast.literal_eval() to rebuild it as the original object tree.
As such it is safe to send serpent data to other machines over the network for instance
(because only 'safe' literals are encoded).

Compatible with Python 2.6+ (including 3.x), IronPython 2.7+, Jython 2.7+.

Serpent handles several special Python types to make life easier:
 bytes, bytearrays, memoryview, buffer  --> string (base-64)
 uuid.UUID, datetime.{datetime, time, timespan}  --> appropriate string/number
 decimal.Decimal  --> string (to not lose precision)
 array.array typecode 'c'/'u' --> string/unicode
 array.array other typecode --> list
 Exception  --> dict with some fields of the exception (message, args)
 all other types  --> dict with  __getstate__  or vars() of the object

Small caveat:
Python 2.x will return unicode for all strings, even when it was a str when serializing.
(in other words, strs are all promoted to unicode. This is normal for Python 3.x by the way,
which only has unicode strings)

@TODO: tests.
@TODO: java and C# implementations, including deserializers.

Copyright 2013, Irmen de Jong (irmen@razorvine.net)
This code is open-source, but licensed under the "MIT software license".
See http://opensource.org/licenses/MIT
"""
import ast
import base64
import sys
import types
if sys.platform == "cli":
    from io import BytesIO   # IronPython
elif sys.version_info < (3, 0):
    import __future__
    from cStringIO import StringIO as BytesIO   # python 2.x
else:
    from io import BytesIO   # python 3.x

__all__ = ["serialize", "deserialize"]


def serialize(obj, indent=False):
    out = BytesIO()
    StreamSerializer(out, indent).serialize(obj)
    return out.getvalue()


def deserialize(serialized_bytes):
    serialized = serialized_bytes.decode("utf-8")
    if sys.version_info < (3, 0):
        # python 2.x: parse with unicode_literals (promotes all strings to unicode)
        serialized = compile(serialized, "<serpent>", mode="eval", flags=ast.PyCF_ONLY_AST | __future__.unicode_literals.compiler_flag)
    return ast.literal_eval(serialized)


class BytesWrapper(object):
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
        return BytesWrapper(data)

    @staticmethod
    def from_bytearray(data):
        return BytesWrapper(data)

    @staticmethod
    def from_memoryview(data):
        return BytesWrapper(data.tobytes())

    @staticmethod
    def from_buffer(data):
        return BytesWrapper(data)


class StreamSerializer(object):
    """Serpent stream serializer. Serialize an object tree to a byte stream."""
    #noinspection PySetFunctionToLiteral
    repr_types = set([
        str,
        int,
        float,
        complex,
        bool,
        type(None)
    ])

    translate_types = {
        bytes: BytesWrapper.from_bytes,
        bytearray: BytesWrapper.from_bytearray
    }

    # do some dynamic changes to the types configuration if needed
    if bytes is str:
        del translate_types[bytes]
    if hasattr(types, "BufferType"):
        translate_types[types.BufferType] = BytesWrapper.from_buffer
    try:
        translate_types[memoryview] = BytesWrapper.from_memoryview
    except NameError:
        pass
    if sys.platform == "cli":
        repr_types.remove(str)  # IronPython needs special str treatment

    def __init__(self, out, indent=False):
        """
        Initialize the serializer. It is not thread safe.
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

    def ser_builtins_str(self, str_obj, out, level):
        # for IronPython where str==unicode and repr() yields undesired result
        self.ser_builtins_unicode(str_obj, out, level)

    def ser_builtins_unicode(self, unicode_obj, out, level):
        # for python 2.x.
        z = unicode_obj.encode("utf-8")
        z = z.replace("\\", "\\\\")  # double-escape the backslashes
        if "'" not in z:
            z = "'" + z + "'"
        elif '"' not in z:
            z = '"' + z + '"'
        else:
            z = z.replace("'", "\\'")
            z = "'" + z + "'"
        out.write(z)

    def ser_builtins_long(self, long_obj, out, level):
        out.write(str(long_obj).encode("utf-8"))        # for python 2.x

    def ser_builtins_tuple(self, tuple_obj, out, level):
        if self.indent and tuple_obj:
            indent_chars = b"  " * level
            out.write(b"(\n")
            for elt in tuple_obj:
                out.write(indent_chars + b"  ")
                self._serialize(elt, out, level + 1)
                out.write(b",\n")
            out.seek(-1, 1)  # undo the last \n
            if len(tuple_obj) > 1:
                out.seek(-1, 1)  # undo the last ,
            out.write(b"\n" + indent_chars + b")")
        else:
            out.write(b"(")
            for elt in tuple_obj:
                self._serialize(elt, out, level + 1)
                out.write(b",")
            if len(tuple_obj) > 1:
                out.seek(-1, 1)  # undo the last ,
            out.write(b")")

    def ser_builtins_list(self, list_obj, out, level):
        if self.indent and list_obj:
            indent_chars = b"  " * level
            out.write(b"[\n")
            for elt in list_obj:
                out.write(indent_chars + b"  ")
                self._serialize(elt, out, level + 1)
                out.write(b",\n")
            out.seek(-2, 1)  # undo the last ,\n
            out.write(b"\n" + indent_chars + b"]")
        else:
            out.write(b"[")
            for elt in list_obj:
                self._serialize(elt, out, level + 1)
                out.write(b",")
            if list_obj:
                out.seek(-1, 1)  # undo the last ,
            out.write(b"]")

    def ser_builtins_dict(self, dict_obj, out, level):
        if self.indent and dict_obj:
            indent_chars = b"  " * level
            out.write(b"{\n")
            for k, v in dict_obj.items():
                out.write(indent_chars + b"  ")
                self._serialize(k, out, level + 1)
                out.write(b": ")
                self._serialize(v, out, level + 1)
                out.write(b",\n")
            out.seek(-2, 1)  # undo the last ,\n
            out.write(b"\n" + indent_chars + b"}")
        else:
            out.write(b"{")
            for k, v in dict_obj.items():
                self._serialize(k, out, level + 1)
                out.write(b":")
                self._serialize(v, out, level + 1)
                out.write(b",")
            if dict_obj:
                out.seek(-1, 1)  # undo the last ,
            out.write(b"}")

    def ser_builtins_set(self, set_obj, out, level):
        if self.indent and set_obj:
            indent_chars = b"  " * level
            out.write(b"{\n")
            for elt in set_obj:
                out.write(indent_chars + b"  ")
                self._serialize(elt, out, level + 1)
                out.write(b",\n")
            out.seek(-2, 1)  # undo the last ,\n
            out.write(b"\n" + indent_chars + b"}")
        elif set_obj:
            out.write(b"{")
            for elt in set_obj:
                self._serialize(elt, out, level + 1)
                out.write(b",")
            out.write(b"}")
        else:
            # empty set literal doesn't exist unfortunately, replace with empty tuple
            self.ser_builtins_tuple((), out, level)

    def ser_builtins_frozenset(self, set_obj, out, level):
        self.ser_builtins_set(set_obj, out, level)

    def ser_decimal_Decimal(self, decimal_obj, out, level):
        # decimal is serialized as a string to avoid losing precision
        self._serialize(str(decimal_obj), out, level)

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
        if array_obj.typecode == 'c':
            self._serialize(array_obj.tostring(), out, level)
        elif array_obj.typecode == 'u':
            self._serialize(array_obj.tounicode(), out, level)
        else:
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
