"""
ast.literal_eval() compatible object tree serialization.

Serpent serializes an object tree into bytes (utf-8 encoded string) that can
be decoded and then passed as-is to ast.literal_eval() to rebuild it as the
original object tree. As such it is safe to send serpent data to other
machines over the network for instance (because only 'safe' literals are
encoded).

Compatible with recent Python 3 versions

Serpent handles several special Python types to make life easier:

 - bytes, bytearrays, memoryview --> string, base-64
   (you'll have to manually un-base64 them though)
 - uuid.UUID, datetime.{datetime, date, time, timespan}  --> appropriate string/number
 - decimal.Decimal  --> string (to not lose precision)
 - array.array typecode 'u' --> string
 - array.array other typecode --> list
 - Exception  --> dict with some fields of the exception (message, args)
 - collections module types  --> mostly equivalent primitive types or dict
 - enums --> the value of the enum
 - all other types  --> dict with  __getstate__  or vars() of the object

Notes:

The serializer is not thread-safe. Make sure you're not making changes
to the object tree that is being serialized, and don't use the same
serializer in different threads.

Because the serialized format is just valid Python source code, it can
contain comments.

Floats +inf and -inf are handled via a trick, Float 'nan' cannot be handled
and is represented by the special value:  {'__class__':'float','value':'nan'}
We chose not to encode it as just the string 'NaN' because that could cause
memory issues when used in multiplications.

Copyright by Irmen de Jong (irmen@razorvine.net)
Software license: "MIT software license". See http://opensource.org/licenses/MIT
"""

import ast
import base64
import sys
import gc
import decimal
import datetime
import uuid
import array
import math
import numbers
import codecs
import collections
import enum
from collections.abc import KeysView, ValuesView, ItemsView

__version__ = "1.40"
__all__ = ["dump", "dumps", "load", "loads", "register_class", "unregister_class", "tobytes"]


def dumps(obj, indent=False, module_in_classname=False, bytes_repr=False):
    """
    Serialize object tree to bytes.
    indent = indent the output over multiple lines (default=false)
    module_in_classname = include module prefix for class names or only use the class name itself
    bytes_repr = should the bytes literal value representation be used instead of base-64 encoding for bytes types?
    """
    return Serializer(indent, module_in_classname, bytes_repr).serialize(obj)


def dump(obj, file, indent=False, module_in_classname=False, bytes_repr=False):
    """
    Serialize object tree to a file.
    indent = indent the output over multiple lines (default=false)
    module_in_classname = include module prefix for class names or only use the class name itself
    bytes_repr = should the bytes literal value representation be used instead of base-64 encoding for bytes types?
    """
    file.write(dumps(obj, indent=indent, module_in_classname=module_in_classname, bytes_repr=bytes_repr))


def loads(serialized_bytes):
    """Deserialize bytes back to object tree. Uses ast.literal_eval (safe)."""
    serialized = codecs.decode(serialized_bytes, "utf-8")
    if '\x00' in serialized:
        raise ValueError(
            "The serpent data contains 0-bytes so it cannot be parsed by ast.literal_eval. Has it been corrupted?")
    try:
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


_special_classes_registry = collections.OrderedDict()  # must be insert-order preserving to make sure of proper precedence rules


def _reset_special_classes_registry():
    _special_classes_registry.clear()
    _special_classes_registry[KeysView] = _ser_DictView
    _special_classes_registry[ValuesView] = _ser_DictView
    _special_classes_registry[ItemsView] = _ser_DictView
    _special_classes_registry[collections.OrderedDict] = _ser_OrderedDict

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


_repr_types = {str, int, bool, type(None)}

_translate_types = {
    collections.deque: list,
    collections.UserDict: dict,
    collections.UserList: list,
    collections.UserString: str
}

_bytes_types = (bytes, bytearray, memoryview)


def _translate_byte_type(t, data, bytes_repr):
    if bytes_repr:
        if t == bytes:
            return repr(data)
        elif t == bytearray:
            return repr(bytes(data))
        elif t == memoryview:
            return repr(bytes(data))
        else:
            raise TypeError("invalid bytes type")
    else:
        b64 = base64.b64encode(data)
        return repr({
            "data": b64 if type(b64) is str else b64.decode("ascii"),
            "encoding": "base64"
        })


def tobytes(obj):
    """
    Utility function to convert obj back to actual bytes if it is a serpent-encoded bytes dictionary
    (a dict with base-64 encoded 'data' in it and 'encoding'='base64').
    If obj is already bytes or a byte-like type, return obj unmodified.
    Will raise TypeError if obj is none of the above.

    All this is not required if you called serpent with 'bytes_repr' set to True, since Serpent 1.40
    that can be used to directly encode bytes into the bytes literal value representation.
    That will be less efficient than the default base-64 encoding though, but it's a bit more convenient.
    """
    if isinstance(obj, _bytes_types):
        return obj
    if isinstance(obj, dict) and "data" in obj and obj.get("encoding") == "base64":
        try:
            return base64.b64decode(obj["data"])
        except TypeError:
            return base64.b64decode(obj["data"].encode("ascii"))  # needed for certain older versions of pypy
    raise TypeError("argument is neither bytes nor serpent base64 encoded bytes dict")


class Serializer(object):
    """
    Serialize an object tree to a byte stream.
    It is not thread-safe: make sure you're not making changes to the
    object tree that is being serialized, and don't use the same serializer
    across different threads.
    """
    dispatch = {}

    def __init__(self, indent=False, module_in_classname=False, bytes_repr=False):
        """
        Initialize the serializer.
        indent=indent the output over multiple lines (default=false)
        module_in_classname = include module prefix for class names or only use the class name itself
        bytes_repr = should the bytes literal value representation be used instead of base-64 encoding for bytes types?
        """
        self.indent = indent
        self.module_in_classname = module_in_classname
        self.serialized_obj_ids = set()
        self.special_classes_registry_copy = None
        self.maximum_level = min(sys.getrecursionlimit() // 5, 1000)
        self.bytes_repr = bytes_repr

    def serialize(self, obj):
        """Serialize the object tree to bytes."""
        self.special_classes_registry_copy = _special_classes_registry.copy()  # make it thread safe
        header = "# serpent utf-8 python3.2\n"
        out = [header]
        try:
            gc.disable()
            self.serialized_obj_ids = set()
            self._serialize(obj, out, 0)
        finally:
            gc.enable()
        self.special_classes_registry_copy = None
        del self.serialized_obj_ids
        return "".join(out).encode("utf-8")

    _shortcut_dispatch_types = {float, complex, tuple, list, dict, set, frozenset}

    def _serialize(self, obj, out, level):
        if level > self.maximum_level:
            raise ValueError(
                "Object graph nesting too deep. Increase serializer.maximum_level if you think you need more, "
                " but this may cause a RecursionError instead if Python's recursion limit doesn't allow it.")
        t = type(obj)
        if t in _bytes_types:
            out.append(_translate_byte_type(t, obj, self.bytes_repr))
            return
        if t in _translate_types:
            obj = _translate_types[t](obj)
            t = type(obj)
        if t in _repr_types:
            out.append(repr(obj))  # just a simple repr() is enough for these objects
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
            if issubclass(t, enum.Enum):
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
        append = out.append
        serialize = self._serialize
        if self.indent and set_obj:
            indent_chars = "  " * level
            indent_chars_inside = indent_chars + "  "
            append("{\n")
            try:
                sorted_elts = sorted(set_obj)
            except TypeError:  # can occur when elements can't be ordered (Python 3.x)
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
        if array_obj.typecode == 'u':
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
