import ast
import base64
import sys

__all__ = ["serialize", "deserialize", "make_safe_tree", "Bytes"]


def serialize(obj, indent=False):
    return ToStringSerializer(indent).serialize(obj)

def deserialize(serialized):
    return ast.literal_eval(serialized)

def make_safe_tree(obj):
    return TreeMaker().serialize(obj)


class Bytes(object):
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


class ToStringSerializer(object):
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

    if sys.version_info < (3, 0):
        # fix some Python 2.x types
        import types
        repr_types.add(types.UnicodeType)
        repr_types.add(types.LongType)
        del translate_types[bytes]  # bytes == str
        translate_types[types.BufferType] = Bytes.from_buffer

    def __init__(self, indent=False):
        self.indent = indent

    # XXX todo: write to a byte buffer instead of a string, UTF-8 encoded
    # XXX todo: the bytestream should be passed as an argument
    # XXX todo: add utf-8 encoding comment in the output
    # XXX todo: strings should be written as utf-8 encoded strings, not their repr (=inefficient)
    def serialize(self, obj):
        """Serialize the object tree to a string representation."""
        return self._serialize(obj, 0)

    def _serialize(self, obj, level):
        t = type(obj)
        if t in self.translate_types:
            obj = self.translate_types[t](obj)
            t = type(obj)
        if t in self.repr_types:
            return self._repr_obj(obj)
        if isinstance(obj, BaseException):
            return self.ser_exception_class(obj, level)
        module = t.__module__
        if module == "__builtin__":
            module = "builtins"  # python 2.x compatibility
        method = "ser_{0}_{1}".format(module, t.__name__)
        return getattr(self, method, self.ser_default_class)(obj, level)

    def _repr_obj(self, obj):
        return repr(obj)

    def ser_builtins_tuple(self, obj, level):
        ser = []
        for elt in obj:
            ser.append(self._serialize(elt, level+1))
        if self.indent and ser:
            indent_chars = "  "*level
            indent_chars_inside = indent_chars + "  "
            out = (",\n" + indent_chars_inside).join(ser)
            if len(ser) == 1:
                return "(\n%s%s,\n%s)" % (indent_chars_inside, out, indent_chars)   # tuple special case when there's only one element
            return "(\n%s%s\n%s)" % (indent_chars_inside, out, indent_chars)   # tuple special case when there's only one element
        out = (",".join(ser))
        if len(ser) == 1:
            return "(%s,)" % out  # tuple special case when there's only one element
        return "(%s)" % out

    def ser_builtins_list(self, obj, level):
        ser = []
        for elt in obj:
            ser.append(self._serialize(elt, level+1))
        if self.indent and ser:
            indent_chars = "  "*level
            indent_chars_inside = indent_chars + "  "
            ser = (",\n" + indent_chars_inside).join(ser)
            return "[\n%s%s\n%s]" % (indent_chars_inside, ser, indent_chars)
        return "[%s]" % (",".join(ser))

    def ser_builtins_dict(self, obj, level):
        ser = []
        kv_format = "{0}: {1}" if self.indent else "{0}:{1}"
        for k, v in obj.items():
            ser.append(kv_format.format(self._serialize(k, level+1), self._serialize(v, level+1)))
        if self.indent and ser:
            indent_chars = "  "*level
            indent_chars_inside = indent_chars + "  "
            ser = (",\n" + indent_chars_inside).join(ser)
            return "{\n%s%s\n%s}" % (indent_chars_inside, ser, indent_chars)
        return "{%s}" % (",".join(ser))

    def ser_builtins_set(self, obj, level):
        ser = []
        for elt in obj:
            ser.append(self._serialize(elt, level+1))
        if self.indent and ser:
            indent_chars = "  "*level
            indent_chars_inside = indent_chars + "  "
            ser = (",\n" + indent_chars_inside).join(ser)
            return "{\n%s%s\n%s}" % (indent_chars_inside, ser, indent_chars)
        return "{%s}" % (",".join(ser))

    def ser_builtins_frozenset(self, obj, level):
        return self.ser_builtins_set(obj, level)

    def ser_decimal_Decimal(self, obj, level):
        return str(obj)

    def ser_datetime_datetime(self, obj, level):
        return self._serialize(obj.isoformat(), level)

    def ser_datetime_timedelta(self, obj, level):
        return self._serialize(obj.total_seconds(), level)

    def ser_datetime_time(self, obj, level):
        return self._serialize(str(obj), level)

    def ser_uuid_UUID(self, obj, level):
        return self._serialize(str(obj), level)

    def ser_exception_class(self, obj, level):
        value = {
            "__class__": type(obj).__name__,
            "__exception__": True,
            "args": obj.args,
            "message": str(obj)
        }
        return self._serialize(value, level)

    def ser_array_array(self, obj, level):
        return self._serialize(obj.tolist(), level)

    def ser_default_class(self, obj, level):
        try:
            value = obj.__getstate__()
            if isinstance(value, dict):
                return self.ser_builtins_dict(value, level)
        except AttributeError:
            try:
                value = dict(vars(obj))  # make sure we can serialize anything that resembles a dict
                value["__class__"] = type(obj).__name__
            except TypeError:
                raise TypeError("don't know how to serialize class " + str(type(obj)) + ", give it vars() or define an appropriate __getstate__")
        return self._serialize(value, level)


class TreeMaker(ToStringSerializer):
    """
    Converts the object tree to a safe representation thereof.
    Doesn't actually serialize anything to strings.
    """
    def _repr_obj(self, obj):
        return obj  # can be used as-is

    def ser_builtins_tuple(self, obj, level):
        ser = [self._serialize(elt, level) for elt in obj]
        return tuple(ser)

    def ser_builtins_list(self, obj, level):
        return [self._serialize(elt, level) for elt in obj]

    def ser_builtins_dict(self, obj, level):
        return { self._serialize(k, level): self._serialize(v, level) for k, v in obj.items() }

    def ser_builtins_set(self, obj, level):
        return { self._serialize(elt, level) for elt in obj }

