"""
ast.literal_eval() compatible object tree serialization.

Serpent serializes an object tree into bytes (utf-8 encoded string) that can
be decoded and then passed as-is to ast.literal_eval() to rebuild it as the
original object tree. As such it is safe to send serpent data to other
machines over the network for instance (because only 'safe' literals are
encoded).

Compatible with Python 2.7+ (including 3.x), IronPython 2.7+, Jython 2.7+.

Serpent handles several special Python types to make life easier:

 - str  --> promoted to unicode (see below why this is)
 - bytes, bytearrays, memoryview, buffer  --> string, base-64
   (you'll have to manually un-base64 them though)
 - uuid.UUID, datetime.{datetime, date, time, timespan}  --> appropriate string/number
 - decimal.Decimal  --> string (to not lose precision)
 - array.array typecode 'c'/'u' --> string/unicode
 - array.array other typecode --> list
 - Exception  --> dict with some fields of the exception (message, args)
 - collections module types  --> mostly equivalent primitive types or dict
 - enums --> the value of the enum (Python 3.4+ or enum34 library)
 - all other types  --> dict with  __getstate__  or vars() of the object

Notes:

All str will be promoted to unicode. This is done because it is the
default anyway for Python 3.x, and it solves the problem of the str/unicode
difference between different Python versions. Also it means the serialized
output doesn't have those problematic 'u' prefixes on strings.

The serializer is not thread-safe. Make sure you're not making changes
to the object tree that is being serialized, and don't use the same
serializer in different threads.

Because the serialized format is just valid Python source code, it can
contain comments.

Set literals are not supported on python <3.2 (ast.literal_eval
limitation). If you need Python < 3.2 compatibility, you'll have to use
set_literals=False when serializing. Since version 1.6 serpent chooses
this wisely for you by default, but you can still override it if needed.

Floats +inf and -inf are handled via a trick, Float 'nan' cannot be handled
and is represented by the special value:  {'__class__':'float','value':'nan'}
We chose not to encode it as just the string 'NaN' because that could cause
memory issues when used in multiplications.

Jython's ast module cannot properly parse some literal reprs of unicode strings.
This is a known bug http://bugs.jython.org/issue2008
It seems to work when your server is Python 2.x but safest is perhaps to make
sure your data to parse contains only ascii strings when dealing with Jython.
Serpent checks for possible problems and will raise an error if it finds one,
rather than continuing with string data that might be incorrect.

Copyright by Irmen de Jong (irmen@razorvine.net)
Software license: "MIT software license". See http://opensource.org/licenses/MIT
"""

from __future__ import print_function, division
import __future__
import ast
import base64
import sys
import types
import os
import gc
import decimal
import datetime
import uuid
import array
import math
import numbers
import codecs
import collections
if sys.version_info >= (3, 4):
    from collections.abc import KeysView, ValuesView, ItemsView
    import enum
else:
    from collections import KeysView, ValuesView, ItemsView
    try:
        import enum
    except ImportError:
        enum = None


__version__ = "1.26"
__all__ = ["dump", "dumps", "load", "loads", "register_class", "unregister_class", "tobytes"]

can_use_set_literals = sys.version_info >= (3, 2)  # check if we can use set literals


def dumps(obj, indent=False, set_literals=can_use_set_literals, module_in_classname=False):
    """Serialize object tree to bytes"""
    return Serializer(indent, set_literals, module_in_classname).serialize(obj)


def dump(obj, file, indent=False, set_literals=can_use_set_literals, module_in_classname=False):
    """Serialize object tree to a file"""
    file.write(dumps(obj, indent=indent, set_literals=set_literals, module_in_classname=module_in_classname))


def loads(serialized_bytes):
    """Deserialize bytes back to object tree. Uses ast.literal_eval (safe)."""
    if os.name == "java":
        if type(serialized_bytes) is memoryview:
            serialized_bytes = serialized_bytes.tobytes()
        elif type(serialized_bytes) is buffer:
            serialized_bytes = serialized_bytes[:]
        serialized = serialized_bytes.decode("utf-8")
    elif sys.platform == "cli":
        if type(serialized_bytes) is memoryview:
            serialized_bytes = serialized_bytes.tobytes()
        serialized = codecs.decode(serialized_bytes, "utf-8")
    else:
        serialized = codecs.decode(serialized_bytes, "utf-8")
    if '\x00' in serialized:
        raise ValueError("The serpent data contains 0-bytes so it cannot be parsed by ast.literal_eval. Has it been corrupted?")
    if sys.version_info < (3, 0):
        # python 2.x: parse with unicode_literals (promotes all strings to unicode)
        # note: this doesn't work on jython... see bug http://bugs.jython.org/issue2008
        # so we add a safety net, to avoid working with incorrectly processed unicode strings
        serialized = compile(serialized, "<serpent>", mode="eval", flags=ast.PyCF_ONLY_AST | __future__.unicode_literals.compiler_flag)
        if os.name == "java":
            for node in ast.walk(serialized):
                if isinstance(node, ast.Str):
                    if isinstance(node.s, str) and any(c for c in node.s if c > '\x7f'):
                        # In this case there is risk of incorrectly parsed unicode data. Play safe and crash.
                        raise ValueError("cannot properly parse unicode string with ast in Jython, see bug http://bugs.jython.org/issue2008"
                                         " - use python 2.x server or convert strings to ascii yourself first")
                    node.s = node.s.decode("unicode-escape")
    try:
        if os.name != "java" and sys.platform != "cli":
            gc.disable()
        return ast.literal_eval(serialized)
    finally:
        gc.enable()


def load(file):
    """Deserialize bytes from a file back to object tree. Uses ast.literal_eval (safe)."""
    data = file.read()
    return loads(data)


def _ser_OrderedDict(obj, serializer, outputstream, indentlevel):
    obj = {
        "__class__": "collections.OrderedDict" if serializer.module_in_classname else "OrderedDict",
        "items": list(obj.items())
    }
    serializer._serialize(obj, outputstream, indentlevel)


def _ser_DictView(obj, serializer, outputstream, indentlevel):
    serializer.ser_builtins_list(obj, outputstream, indentlevel)


_special_classes_registry = collections.OrderedDict()   # must be insert-order preserving to make sure of proper precedence rules


def _reset_special_classes_registry():
    _special_classes_registry.clear()
    _special_classes_registry[KeysView] = _ser_DictView
    _special_classes_registry[ValuesView] = _ser_DictView
    _special_classes_registry[ItemsView] = _ser_DictView
    if sys.version_info >= (2, 7):
        _special_classes_registry[collections.OrderedDict] = _ser_OrderedDict
    if enum is not None:
        def _ser_Enum(obj, serializer, outputstream, indentlevel):
            serializer._serialize(obj.value, outputstream, indentlevel)
        _special_classes_registry[enum.Enum] = _ser_Enum


_reset_special_classes_registry()


def unregister_class(clazz):
    """Unregister the specialcase serializer for the given class."""
    if clazz in _special_classes_registry:
        del _special_classes_registry[clazz]


def register_class(clazz, serializer):
    """
    Register a special serializer function for objects of the given class.
    The function will be called with (object, serpent_serializer, outputstream, indentlevel) arguments.
    The function must write the serialized data to outputstream. It doesn't return a value.
    """
    _special_classes_registry[clazz] = serializer


class BytesWrapper(object):
    """
    Wrapper for bytes, bytearray etc. to make them appear as base-64 encoded data.
    You can use the tobytes utility function to decode this back into the actual bytes (or do it manually)
    """
    def __init__(self, data):
        self.data = data

    def __getstate__(self):
        if sys.platform == "cli":
            b64 = base64.b64encode(str(self.data))  # weird IronPython bug?
        elif (os.name == "java" or sys.version_info < (2, 7)) and type(self.data) is bytearray:
            b64 = base64.b64encode(bytes(self.data))  # Jython bug http://bugs.jython.org/issue2011
        else:
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


_repr_types = set([
    str,
    int,
    bool,
    type(None)
])

_translate_types = {
    bytes: BytesWrapper.from_bytes,
    bytearray: BytesWrapper.from_bytearray,
    collections.deque: list,
}

if sys.version_info >= (3, 0):
    _translate_types.update({
        collections.UserDict: dict,
        collections.UserList: list,
        collections.UserString: str
    })

_bytes_types = [bytes, bytearray, memoryview]

# do some dynamic changes to the types configuration if needed
if bytes is str:
    del _translate_types[bytes]
if hasattr(types, "BufferType"):
    _translate_types[types.BufferType] = BytesWrapper.from_buffer
    _bytes_types.append(buffer)
try:
    _translate_types[memoryview] = BytesWrapper.from_memoryview
except NameError:
    pass
if sys.platform == "cli":
    _repr_types.remove(str)  # IronPython needs special str treatment, otherwise it treats unicode wrong
_bytes_types = tuple(_bytes_types)


def tobytes(obj):
    """
    Utility function to convert obj back to actual bytes if it is a serpent-encoded bytes dictionary
    (a dict with base-64 encoded 'data' in it and 'encoding'='base64').
    If obj is already bytes or a byte-like type, return obj unmodified.
    Will raise TypeError if obj is none of the above.
    """
    if isinstance(obj, _bytes_types):
        return obj
    if isinstance(obj, dict) and "data" in obj and obj.get("encoding") == "base64":
        try:
            return base64.b64decode(obj["data"])
        except TypeError:
            return base64.b64decode(obj["data"].encode("ascii"))   # needed for certain older versions of pypy
    raise TypeError("argument is neither bytes nor serpent base64 encoded bytes dict")


class Serializer(object):
    """
    Serialize an object tree to a byte stream.
    It is not thread-safe: make sure you're not making changes to the
    object tree that is being serialized, and don't use the same serializer
    across different threads.
    """
    dispatch = {}

    def __init__(self, indent=False, set_literals=can_use_set_literals, module_in_classname=False):
        """
        Initialize the serializer.
        indent=indent the output over multiple lines (default=false)
        setLiterals=use set-literals or not (set to False if you need compatibility with Python < 3.2).
        Serpent chooses a sensible default for you.
        module_in_classname = include module prefix for class names or only use the class name itself
        """
        self.indent = indent
        self.set_literals = set_literals
        self.module_in_classname = module_in_classname
        self.serialized_obj_ids = set()
        self.special_classes_registry_copy = None
        self.maximum_level = min(sys.getrecursionlimit() // 5, 1000)

    def serialize(self, obj):
        """Serialize the object tree to bytes."""
        self.special_classes_registry_copy = _special_classes_registry.copy()   # make it thread safe
        header = "# serpent utf-8 "
        if self.set_literals:
            header += "python3.2\n"   # set-literals require python 3.2+ to deserialize (ast.literal_eval limitation)
        else:
            header += "python2.6\n"   # don't change this, otherwise we can't read older serpent strings
        out = [header]
        if os.name == "java" and type(obj) is buffer:
            obj = bytearray(obj)
        try:
            if os.name != "java" and sys.platform != "cli":
                gc.disable()
            self.serialized_obj_ids = set()
            self._serialize(obj, out, 0)
        finally:
            gc.enable()
        self.special_classes_registry_copy = None
        del self.serialized_obj_ids
        return "".join(out).encode("utf-8")

    _shortcut_dispatch_types = frozenset([float, complex, tuple, list, dict, set, frozenset])

    def _serialize(self, obj, out, level):
        if level > self.maximum_level:
            raise ValueError("Object graph nesting too deep. Increase serializer.maximum_level if you think you need more, "
                             " but this may cause a RecursionError instead if Python's recursion limit doesn't allow it.")
        t = type(obj)
        if t in _translate_types:
            obj = _translate_types[t](obj)
            t = type(obj)
        if t in _repr_types:
            out.append(repr(obj))    # just a simple repr() is enough for these objects
            return
        if t in self._shortcut_dispatch_types:
            # we shortcut these builtins directly to the dispatch function to avoid type lookup overhead below
            return self.dispatch[t](self, obj, out, level)
        # check special registered types:
        special_classes = self.special_classes_registry_copy
        for clazz in special_classes:
            if isinstance(obj, clazz):
                special_classes[clazz](obj, self, out, level)
                return
        # serialize dispatch
        try:
            func = self.dispatch[t]
        except KeyError:
            # walk the MRO until we find a base class we recognise
            for type_ in t.__mro__:
                if type_ in self.dispatch:
                    func = self.dispatch[type_]
                    break
            else:
                # fall back to the default class serializer
                func = Serializer.ser_default_class
        func(self, obj, out, level)

    def ser_builtins_str(self, str_obj, out, level):
        # special case str, for IronPython where str==unicode and repr() yields undesired result
        self.ser_builtins_unicode(str_obj, out, level)
    dispatch[str] = ser_builtins_str

    def ser_builtins_float(self, float_obj, out, level):
        if math.isnan(float_obj):
            # there's no literal expression for a float NaN...
            out.append("{'__class__':'float','value':'nan'}")
        elif math.isinf(float_obj):
            # output a literal expression that overflows the float and results in +/-INF
            if float_obj > 0:
                out.append("1e30000")
            else:
                out.append("-1e30000")
        else:
            out.append(repr(float_obj))
    dispatch[float] = ser_builtins_float

    def ser_builtins_complex(self, complex_obj, out, level):
        out.append("(")
        self.ser_builtins_float(complex_obj.real, out, level)
        if complex_obj.imag >= 0:
            out.append("+")
        self.ser_builtins_float(complex_obj.imag, out, level)
        out.append("j)")
    dispatch[complex] = ser_builtins_complex

    if sys.version_info < (3, 0):
        # this method is used for python 2.x unicode (python 3.x doesn't use this)
        def ser_builtins_unicode(self, unicode_obj, out, level):
            z = repr(unicode_obj)
            if z[0] == 'u':
                z = z[1:]    # get rid of the unicode 'u' prefix
            out.append(z)
        dispatch[unicode] = ser_builtins_unicode

    if sys.version_info < (3, 0):
        def ser_builtins_long(self, long_obj, out, level):
            # used with python 2.x
            out.append(str(long_obj))
        dispatch[long] = ser_builtins_long

    def ser_builtins_tuple(self, tuple_obj, out, level):
        append = out.append
        serialize = self._serialize
        if self.indent and tuple_obj:
            indent_chars = "  " * level
            indent_chars_inside = indent_chars + "  "
            append("(\n")
            for elt in tuple_obj:
                append(indent_chars_inside)
                serialize(elt, out, level + 1)
                append(",\n")
            out[-1] = out[-1].rstrip()  # remove the last \n
            if len(tuple_obj) > 1:
                del out[-1]  # undo the last ,
            append("\n" + indent_chars + ")")
        else:
            append("(")
            for elt in tuple_obj:
                serialize(elt, out, level + 1)
                append(",")
            if len(tuple_obj) > 1:
                del out[-1]  # undo the last ,
            append(")")
    dispatch[tuple] = ser_builtins_tuple

    def ser_builtins_list(self, list_obj, out, level):
        if id(list_obj) in self.serialized_obj_ids:
            raise ValueError("Circular reference detected (list)")
        self.serialized_obj_ids.add(id(list_obj))
        append = out.append
        serialize = self._serialize
        if self.indent and list_obj:
            indent_chars = "  " * level
            indent_chars_inside = indent_chars + "  "
            append("[\n")
            for elt in list_obj:
                append(indent_chars_inside)
                serialize(elt, out, level + 1)
                append(",\n")
            del out[-1]  # remove the last ,\n
            append("\n" + indent_chars + "]")
        else:
            append("[")
            for elt in list_obj:
                serialize(elt, out, level + 1)
                append(",")
            if list_obj:
                del out[-1]  # remove the last ,
            append("]")
        self.serialized_obj_ids.discard(id(list_obj))
    dispatch[list] = ser_builtins_list

    def _check_hashable_type(self, t):
        if t not in (bool, bytes, str, tuple) and not issubclass(t, numbers.Number):
            if enum is not None:
                if issubclass(t, enum.Enum):
                    return
            elif sys.version_info < (3, 0) and t is unicode:
                return
            raise TypeError("one of the keys in a dict or set is not of a primitive hashable type: " +
                            str(t) + ". Use simple types as keys or use a list or tuple as container.")

    def ser_builtins_dict(self, dict_obj, out, level):
        if id(dict_obj) in self.serialized_obj_ids:
            raise ValueError("Circular reference detected (dict)")
        self.serialized_obj_ids.add(id(dict_obj))
        append = out.append
        serialize = self._serialize
        if self.indent and dict_obj:
            indent_chars = "  " * level
            indent_chars_inside = indent_chars + "  "
            append("{\n")
            dict_items = dict_obj.items()
            try:
                sorted_items = sorted(dict_items)
            except TypeError:  # can occur when elements can't be ordered (Python 3.x)
                sorted_items = dict_items
            for key, value in sorted_items:
                append(indent_chars_inside)
                self._check_hashable_type(type(key))
                serialize(key, out, level + 1)
                append(": ")
                serialize(value, out, level + 1)
                append(",\n")
            del out[-1]  # remove last ,\n
            append("\n" + indent_chars + "}")
        else:
            append("{")
            for key, value in dict_obj.items():
                self._check_hashable_type(type(key))
                serialize(key, out, level + 1)
                append(":")
                serialize(value, out, level + 1)
                append(",")
            if dict_obj:
                del out[-1]  # remove the last ,
            append("}")
        self.serialized_obj_ids.discard(id(dict_obj))
    dispatch[dict] = ser_builtins_dict

    def ser_builtins_set(self, set_obj, out, level):
        if not self.set_literals:
            if self.indent:
                set_obj = sorted(set_obj)
            self._serialize(tuple(set_obj), out, level)     # use a tuple instead of a set literal
            return
        append = out.append
        serialize = self._serialize
        if self.indent and set_obj:
            indent_chars = "  " * level
            indent_chars_inside = indent_chars + "  "
            append("{\n")
            try:
                sorted_elts = sorted(set_obj)
            except TypeError:   # can occur when elements can't be ordered (Python 3.x)
                sorted_elts = set_obj
            for elt in sorted_elts:
                append(indent_chars_inside)
                self._check_hashable_type(type(elt))
                serialize(elt, out, level + 1)
                append(",\n")
            del out[-1]  # remove the last ,\n
            append("\n" + indent_chars + "}")
        elif set_obj:
            append("{")
            for elt in set_obj:
                self._check_hashable_type(type(elt))
                serialize(elt, out, level + 1)
                append(",")
            del out[-1]  # remove the last ,
            append("}")
        else:
            # empty set literal doesn't exist unfortunately, replace with empty tuple
            self.ser_builtins_tuple((), out, level)
    dispatch[set] = ser_builtins_set

    def ser_builtins_frozenset(self, set_obj, out, level):
        self.ser_builtins_set(set_obj, out, level)
    dispatch[frozenset] = ser_builtins_set

    def ser_decimal_Decimal(self, decimal_obj, out, level):
        # decimal is serialized as a string to avoid losing precision
        out.append(repr(str(decimal_obj)))
    dispatch[decimal.Decimal] = ser_decimal_Decimal

    def ser_datetime_datetime(self, datetime_obj, out, level):
        out.append(repr(datetime_obj.isoformat()))
    dispatch[datetime.datetime] = ser_datetime_datetime

    def ser_datetime_date(self, date_obj, out, level):
        out.append(repr(date_obj.isoformat()))
    dispatch[datetime.date] = ser_datetime_date

    if os.name == "java" or sys.version_info < (2, 7):    # jython bug http://bugs.jython.org/issue2010
        def ser_datetime_timedelta(self, timedelta_obj, out, level):
            secs = ((timedelta_obj.days * 86400 + timedelta_obj.seconds) * 10 ** 6 + timedelta_obj.microseconds) / 10 ** 6
            out.append(repr(secs))
    else:
        def ser_datetime_timedelta(self, timedelta_obj, out, level):
            secs = timedelta_obj.total_seconds()
            out.append(repr(secs))
    dispatch[datetime.timedelta] = ser_datetime_timedelta

    def ser_datetime_time(self, time_obj, out, level):
        out.append(repr(str(time_obj)))
    dispatch[datetime.time] = ser_datetime_time

    def ser_uuid_UUID(self, uuid_obj, out, level):
        out.append(repr(str(uuid_obj)))
    dispatch[uuid.UUID] = ser_uuid_UUID

    def ser_exception_class(self, exc_obj, out, level):
        value = {
            "__class__": self.get_class_name(exc_obj),
            "__exception__": True,
            "args": exc_obj.args,
            "attributes": vars(exc_obj)  # add any custom attributes
        }
        self._serialize(value, out, level)
    dispatch[BaseException] = ser_exception_class

    def ser_array_array(self, array_obj, out, level):
        if array_obj.typecode == 'c':
            self._serialize(array_obj.tostring(), out, level)
        elif array_obj.typecode == 'u':
            self._serialize(array_obj.tounicode(), out, level)
        else:
            self._serialize(array_obj.tolist(), out, level)
    dispatch[array.array] = ser_array_array

    def ser_default_class(self, obj, out, level):
        if id(obj) in self.serialized_obj_ids:
            raise ValueError("Circular reference detected (class)")
        self.serialized_obj_ids.add(id(obj))
        try:
            try:
                value = obj.__getstate__()
                if value is None and isinstance(obj, tuple):
                    # collections.namedtuple specialcase (if it is not handled by the tuple serializer)
                    value = {
                        "__class__": self.get_class_name(obj),
                        "items": list(obj._asdict().items())
                    }
                if isinstance(value, dict):
                    self.ser_builtins_dict(value, out, level)
                    return
            except AttributeError:
                try:
                    value = dict(vars(obj))  # make sure we can serialize anything that resembles a dict
                    value["__class__"] = self.get_class_name(obj)
                except TypeError:
                    if hasattr(obj, "__slots__"):
                        # use the __slots__ instead of the vars dict
                        value = {}
                        for slot in obj.__slots__:
                            value[slot] = getattr(obj, slot)
                        value["__class__"] = self.get_class_name(obj)
                    else:
                        raise TypeError("don't know how to serialize class " +
                                        str(obj.__class__) + ". Give it vars() or an appropriate __getstate__")
            self._serialize(value, out, level)
        finally:
            self.serialized_obj_ids.discard(id(obj))

    def get_class_name(self, obj):
        if self.module_in_classname:
            return "%s.%s" % (obj.__class__.__module__, obj.__class__.__name__)
        else:
            return obj.__class__.__name__
