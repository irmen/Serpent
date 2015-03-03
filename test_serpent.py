"""
Serpent: ast.literal_eval() compatible object tree serialization.

Copyright 2013, Irmen de Jong (irmen@razorvine.net)
Software license: "MIT software license". See http://opensource.org/licenses/MIT
"""
from __future__ import print_function, division
import sys
import timeit
import datetime
import uuid
import decimal
import array
import tempfile
import os
import hashlib
import traceback
import threading
import time
import collections

if sys.version_info < (2, 7):
    import unittest2 as unittest
else:
    import unittest

import serpent


if sys.version_info >= (3, 0):
    unicode = str
    unichr = chr


def strip_header(ser):
    if sys.platform == "cli":
        _, _, data = ser.partition("\n")
    else:
        _, _, data = ser.partition(b"\n")
    return data


class TestDeserialize(unittest.TestCase):
    def test_deserialize(self):
        data = serpent.loads(b"555")
        self.assertEqual(555, data)
        unicodestring = "euro" + unichr(0x20ac)
        encoded = repr(unicodestring).encode("utf-8")
        data = serpent.loads(encoded)
        self.assertEqual(unicodestring, data)


class TestBasics(unittest.TestCase):
    def test_header(self):
        ser = serpent.dumps(None, set_literals=True)
        if sys.platform == "cli":
            header, _, rest = ser.partition("\n")
        else:
            self.assertTrue(type(ser) is bytes)
            header, _, rest = ser.partition(b"\n")
        hdr = "# serpent utf-8 python3.2".encode("utf-8")
        self.assertEqual(hdr, header)
        ser = serpent.dumps(None, set_literals=False)
        if sys.platform == "cli":
            header, _, rest = ser.partition("\n")
        else:
            self.assertTrue(type(ser) is bytes)
            header, _, rest = ser.partition(b"\n")
        hdr = "# serpent utf-8 python2.6".encode("utf-8")
        self.assertEqual(hdr, header)

    def test_comments(self):
        ser = b"""# serpent utf-8 python2.7
[ 1, 2,
   # some comments here
   3, 4]    # more here
# and here."""
        data = serpent.loads(ser)
        self.assertEqual([1, 2, 3, 4], data)

        ser = b"[ 1, 2 ]"       # no header whatsoever
        data = serpent.loads(ser)
        self.assertEqual([1, 2], data)

    def test_sorting(self):
        obj = [3, 2, 1]
        ser = serpent.dumps(obj)
        data = strip_header(ser)
        self.assertEqual(b"[3,2,1]", data)
        obj = (3, 2, 1)
        ser = serpent.dumps(obj)
        data = strip_header(ser)
        self.assertEqual(b"(3,2,1)", data)

        obj = {3: "three", 4: "four", 2: "two", 1: "one"}
        ser = serpent.dumps(obj)
        data = strip_header(ser)
        self.assertEqual(36, len(data))
        obj = set([3, 4, 2, 1, 6, 5])
        ser = serpent.dumps(obj)
        data = strip_header(ser)
        self.assertEqual(13, len(data))
        ser = serpent.dumps(obj, indent=True, set_literals=True)
        data = strip_header(ser)
        self.assertEqual(b"{\n  1,\n  2,\n  3,\n  4,\n  5,\n  6\n}", data)      # sorted

        obj = set([3, "something"])
        ser = serpent.dumps(obj, indent=False, set_literals=True)
        data = strip_header(ser)
        self.assertTrue(data == b"{3,'something'}" or data == b"{'something',3}")
        ser = serpent.dumps(obj, indent=True, set_literals=True)
        data = strip_header(ser)
        self.assertTrue(data == b"{\n  3,\n  'something'\n}" or data == b"{\n  'something',\n  3\n}")

        obj = {3: "three", "something": 99}
        ser = serpent.dumps(obj, indent=False, set_literals=True)
        data = strip_header(ser)
        self.assertTrue(data == b"{'something':99,3:'three'}" or data == b"{3:'three','something':99}")
        ser = serpent.dumps(obj, indent=True, set_literals=True)
        data = strip_header(ser)
        self.assertTrue(data == b"{\n  'something': 99,\n  3: 'three'\n}" or data == b"{\n  3: 'three',\n  'something': 99\n}")

        obj = {3: "three", 4: "four", 5: "five", 2: "two", 1: "one"}
        ser = serpent.dumps(obj, indent=True, set_literals=True)
        data = strip_header(ser)
        self.assertEqual(b"{\n  1: 'one',\n  2: 'two',\n  3: 'three',\n  4: 'four',\n  5: 'five'\n}", data)   # sorted

    def test_none(self):
        ser = serpent.dumps(None)
        data = strip_header(ser)
        self.assertEqual(b"None", data)

    def test_string(self):
        ser = serpent.dumps("hello")
        data = strip_header(ser)
        self.assertEqual(b"'hello'", data)
        ser = serpent.dumps("quotes'\"")
        data = strip_header(ser)
        self.assertEqual(b"'quotes\\'\"'", data)
        ser = serpent.dumps("quotes2'")
        data = strip_header(ser)
        self.assertEqual(b"\"quotes2'\"", data)

    @unittest.skipIf(sys.platform == "cli", "IronPython has problems with null bytes in strings")
    def test_nullbytesstring(self):
        ser = serpent.dumps("\0null")
        data = serpent.loads(ser)
        self.assertEqual("\0null", data)

    @unittest.skipIf(sys.version_info < (3, 0), "needs python 3.x to correctly process null bytes in unicode strings")
    def test_nullbytesunicode(self):
        line = unichr(0) + "null"
        ser = serpent.dumps(line)
        data = strip_header(ser)
        self.assertEqual(b"'\\x00null'", data)
        data = serpent.loads(ser)
        self.assertEqual(line, data)

    def test_unicode(self):
        u = "euro" + unichr(0x20ac)
        self.assertTrue(type(u) is unicode)
        ser = serpent.dumps(u)
        data = serpent.loads(ser)
        self.assertEqual(u, data)

    def test_unicode_with_escapes(self):
        line = "euro" + unichr(0x20ac) + "\nlastline\ttab\\@slash"
        ser = serpent.dumps(line)
        d = strip_header(ser)
        self.assertEqual(b"'euro\xe2\x82\xac\\nlastline\\ttab\\\\@slash'", d)
        data = serpent.loads(ser)
        self.assertEqual(line, data)

    def test_numbers(self):
        ser = serpent.dumps(12345)
        data = strip_header(ser)
        self.assertEqual(b"12345", data)
        ser = serpent.dumps(123456789123456789123456789)
        data = strip_header(ser)
        self.assertEqual(b"123456789123456789123456789", data)
        ser = serpent.dumps(99.1234)
        data = strip_header(ser)
        self.assertEqual(b"99.1234", data)
        ser = serpent.dumps(decimal.Decimal("1234.9999999999"))
        data = strip_header(ser)
        self.assertEqual(b"'1234.9999999999'", data)
        ser = serpent.dumps(2 + 3j)
        data = strip_header(ser)
        self.assertEqual(b"(2+3j)", data)

    def test_bool(self):
        ser = serpent.dumps(True)
        data = strip_header(ser)
        self.assertEqual(b"True", data)

    def test_dict(self):
        ser = serpent.dumps({})
        data = strip_header(ser)
        self.assertEqual(b"{}", data)
        ser = serpent.dumps({}, indent=True)
        data = strip_header(ser)
        self.assertEqual(b"{}", data)

        mydict = {
            42: 'fortytwo',
            'status': False,
            'name': 'Sally',
            'sixteen-and-half': 16.5
        }
        ser = serpent.dumps(mydict)
        data = strip_header(ser)
        self.assertEqual(69, len(data))
        if sys.version_info < (3, 0):
            self.assertEqual(b"{", data[0])
            self.assertEqual(b"}", data[-1])
        else:
            self.assertEqual(ord("{"), data[0])
            self.assertEqual(ord("}"), data[-1])
        ser = serpent.dumps(mydict, indent=True)
        data = strip_header(ser)
        self.assertEqual(86, len(data))
        if sys.version_info < (3, 0):
            self.assertEqual(b"{", data[0])
            self.assertEqual(b"}", data[-1])
        else:
            self.assertEqual(ord("{"), data[0])
            self.assertEqual(ord("}"), data[-1])

    def test_list(self):
        ser = serpent.dumps([])
        data = strip_header(ser)
        self.assertEqual(b"[]", data)
        ser = serpent.dumps([], indent=True)
        data = strip_header(ser)
        self.assertEqual(b"[]", data)

        mylist = [42, "Sally", 16.5]
        ser = serpent.dumps(mylist)
        data = strip_header(ser)
        self.assertEqual(b"[42,'Sally',16.5]", data)
        ser = serpent.dumps(mylist, indent=True)
        data = strip_header(ser)
        self.assertEqual(b"""[
  42,
  'Sally',
  16.5
]""", data)

    def test_tuple(self):
        ser = serpent.dumps(tuple())
        data = strip_header(ser)
        self.assertEqual(b"()", data)
        ser = serpent.dumps(tuple(), indent=True)
        data = strip_header(ser)
        self.assertEqual(b"()", data)

        ser = serpent.dumps((1,))
        data = strip_header(ser)
        self.assertEqual(b"(1,)", data)
        ser = serpent.dumps((1,), indent=True)
        data = strip_header(ser)
        self.assertEqual(b"(\n  1,\n)", data)

        mytuple = (42, "Sally", 16.5)
        ser = serpent.dumps(mytuple)
        data = strip_header(ser)
        self.assertEqual(b"(42,'Sally',16.5)", data)
        ser = serpent.dumps(mytuple, indent=True)
        data = strip_header(ser)
        self.assertEqual(b"""(
  42,
  'Sally',
  16.5
)""", data)

    def test_set(self):
        ser = serpent.dumps(set())
        data = strip_header(ser)
        self.assertEqual(b"()", data)
        ser = serpent.dumps(set(), indent=True)
        data = strip_header(ser)
        self.assertEqual(b"()", data)

        # test set-literals
        myset = set([42, "Sally"])
        ser = serpent.dumps(myset, set_literals=True)
        data = strip_header(ser)
        self.assertTrue(data == b"{42,'Sally'}" or data == b"{'Sally',42}")
        ser = serpent.dumps(myset, indent=True, set_literals=True)
        data = strip_header(ser)
        self.assertTrue(data == b"{\n  42,\n  'Sally'\n}" or data == b"{\n  'Sally',\n  42\n}")

        # test no set-literals
        ser = serpent.dumps(myset, set_literals=False)
        data = strip_header(ser)
        self.assertTrue(data == b"(42,'Sally')" or data == b"('Sally',42)")    # must output a tuple instead of a set-literal

    def test_bytes(self):
        if sys.version_info >= (3, 0):
            ser = serpent.dumps(bytes(b"abcdef"))
            data = serpent.loads(ser)
            self.assertEqual({'encoding': 'base64', 'data': 'YWJjZGVm'}, data)
        ser = serpent.dumps(bytearray(b"abcdef"))
        data = serpent.loads(ser)
        self.assertEqual({'encoding': 'base64', 'data': 'YWJjZGVm'}, data)
        if sys.version_info >= (2, 7):
            ser = serpent.dumps(memoryview(b"abcdef"))
            data = serpent.loads(ser)
            self.assertEqual({'encoding': 'base64', 'data': 'YWJjZGVm'}, data)

    def test_exception(self):
        x = ZeroDivisionError("wrong")
        ser = serpent.dumps(x)
        data = serpent.loads(ser)
        self.assertEqual({
            '__class__': 'ZeroDivisionError',
            '__exception__': True,
            'args': ('wrong',),
            'attributes': {}
        }, data)
        x = ZeroDivisionError("wrong", 42)
        ser = serpent.dumps(x)
        data = serpent.loads(ser)
        self.assertEqual({
            '__class__': 'ZeroDivisionError',
            '__exception__': True,
            'args': ('wrong', 42),
            'attributes': {}
        }, data)
        x.custom_attribute = "custom_attr"
        ser = serpent.dumps(x)
        data = serpent.loads(ser)
        self.assertEqual({
            '__class__': 'ZeroDivisionError',
            '__exception__': True,
            'args': ('wrong', 42),
            'attributes': {'custom_attribute': 'custom_attr'}
        }, data)

    def test_exception2(self):
        x = ZeroDivisionError("wrong")
        ser = serpent.dumps(x, module_in_classname=True)
        data = serpent.loads(ser)
        if sys.version_info < (3, 0):
            expected_classname = "exceptions.ZeroDivisionError"
        else:
            expected_classname = "builtins.ZeroDivisionError"
        self.assertEqual({
            '__class__': expected_classname,
            '__exception__': True,
            'args': ('wrong',),
            'attributes': {}
        }, data)

    def test_class(self):
        class Class1(object):
            def __init__(self):
                self.attr = 1

        class Class2(object):
            def __getstate__(self):
                return {"attr": 42}

        class SlotsClass(object):
            __slots__ = ["attr"]

            def __init__(self):
                self.attr = 1

        c = Class1()
        ser = serpent.dumps(c)
        data = serpent.loads(ser)
        self.assertEqual({'__class__': 'Class1', 'attr': 1}, data)
        c = Class2()
        ser = serpent.dumps(c)
        data = serpent.loads(ser)
        self.assertEqual({'attr': 42}, data)
        c = SlotsClass()
        ser = serpent.dumps(c)
        data = serpent.loads(ser)
        self.assertEqual({'__class__': 'SlotsClass', 'attr': 1}, data)
        import pprint
        p = pprint.PrettyPrinter(stream="dummy", width=99)
        ser = serpent.dumps(p)
        data = serpent.loads(ser)
        self.assertEqual("PrettyPrinter", data["__class__"])
        self.assertEqual(99, data["_width"])

    def test_class2(self):
        import pprint
        pp = pprint.PrettyPrinter(stream="dummy", width=42)
        ser = serpent.dumps(pp, module_in_classname=True)
        data = serpent.loads(ser)
        self.assertEqual('pprint.PrettyPrinter', data["__class__"])

    def test_array(self):
        ser = serpent.dumps(array.array('u', unicode("unicode")))
        data = strip_header(ser)
        self.assertEqual(b"'unicode'", data)
        ser = serpent.dumps(array.array('i', [44, 45, 46]))
        data = strip_header(ser)
        self.assertEqual(b"[44,45,46]", data)
        if sys.version_info < (3, 0):
            ser = serpent.dumps(array.array('c', "normal"))
            data = strip_header(ser)
            self.assertEqual(b"'normal'", data)

    def test_time(self):
        ser = serpent.dumps(datetime.datetime(2013, 1, 20, 23, 59, 45, 999888))
        data = strip_header(ser)
        self.assertEqual(b"'2013-01-20T23:59:45.999888'", data)
        ser = serpent.dumps(datetime.time(23, 59, 45, 999888))
        data = strip_header(ser)
        self.assertEqual(b"'23:59:45.999888'", data)
        ser = serpent.dumps(datetime.time(23, 59, 45))
        data = strip_header(ser)
        self.assertEqual(b"'23:59:45'", data)
        ser = serpent.dumps(datetime.timedelta(1, 4000, 999888, minutes=22))
        data = strip_header(ser)
        self.assertEqual(b"91720.999888", data)
        ser = serpent.dumps(datetime.timedelta(seconds=12345))
        data = strip_header(ser)
        self.assertEqual(b"12345.0", data)

    def test_pickle_api(self):
        ser = serpent.dumps([1, 2, 3])
        serpent.loads(ser)
        tmpfn = tempfile.mktemp()
        with open(tmpfn, "wb") as outf:
            serpent.dump([1, 2, 3], outf, indent=True, set_literals=True)
        with open(tmpfn, "rb") as inf:
            data = serpent.load(inf)
            self.assertEqual([1, 2, 3], data)
        os.remove(tmpfn)


class TestSpeed(unittest.TestCase):
    def setUp(self):
        self.data = {
            "str": "hello",
            "unicode": unichr(0x20ac),  # euro-character
            "numbers": [123456789012345678901234567890, 999.1234, decimal.Decimal("1.99999999999999999991")],
            "bytes": bytearray(100),
            "list": [1, 2, 3, 4, 5, 6, 7, 8],
            "tuple": (1, 2, 3, 4, 5, 6, 7, 8),
            "set": set([1, 2, 3, 4, 5, 6, 7, 8, 9]),
            "dict": dict((i, str(i) * 4) for i in range(10)),
            "exc": ZeroDivisionError("fault"),
            "dates": [
                datetime.datetime.now(),
                datetime.time(23, 59, 45, 999888),
                datetime.timedelta(seconds=500)
            ],
            "uuid": uuid.uuid4()
        }

    def test_ser_speed(self):
        print("serialize without indent:", timeit.timeit(lambda: serpent.dumps(self.data, False), number=1000))
        print("serialize with indent:", timeit.timeit(lambda: serpent.dumps(self.data, True), number=1000))

    def test_deser_speed(self):
        ser = serpent.dumps(self.data, False)
        print("deserialize without indent:", timeit.timeit(lambda: serpent.loads(ser), number=1000))
        ser = serpent.dumps(self.data, True)
        print("deserialize with indent:", timeit.timeit(lambda: serpent.loads(ser), number=1000))


class TestIndent(unittest.TestCase):
    def test_indent_primitive(self):
        data = 12345
        ser = serpent.dumps(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("12345", ser)

    def test_indent_sorting(self):
        # non-indented should not be sorted, indented should
        data = {"ee": 1, "dd": 1, "cc": 1, "bb": 1, "aa": 1, 'ff': 1, 'hh': 1, 'gg': 1}
        ser = serpent.dumps(data, False)
        ser = strip_header(ser)
        self.assertNotEqual(b"{'aa':1,'bb':1,'cc':1,'dd':1,'ee':1,'ff':1,'gg':1,'hh':1}", ser)
        ser = serpent.dumps(data, True)
        ser = strip_header(ser)
        self.assertEqual(b"""{
  'aa': 1,
  'bb': 1,
  'cc': 1,
  'dd': 1,
  'ee': 1,
  'ff': 1,
  'gg': 1,
  'hh': 1
}""", ser)
        data = set("irmen de jong irmen de jong666")
        ser = serpent.dumps(data, False)
        ser = strip_header(ser)
        self.assertNotEqual(b"' ','6','d','e','g','i','j','m','n','o','r'", ser[1:-1])
        ser = serpent.dumps(data, True)
        ser = strip_header(ser)
        self.assertEqual(b"\n  ' ',\n  '6',\n  'd',\n  'e',\n  'g',\n  'i',\n  'j',\n  'm',\n  'n',\n  'o',\n  'r'\n", ser[1:-1])

    def test_indent_containers(self):
        data = [1, 2, 3]
        ser = serpent.dumps(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""[
  1,
  2,
  3
]""", ser)
        data = (1, 2, 3)
        ser = serpent.dumps(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""(
  1,
  2,
  3
)""", ser)
        data = set([1])
        ser = serpent.dumps(data, indent=True, set_literals=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""{
  1
}""", ser)
        data = {"one": 1}
        ser = serpent.dumps(data, indent=True, set_literals=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""{
  'one': 1
}""", ser)

        data = {"first": [1, 2, ("a", "b")], "second": {1: False}, "third": set([1, 2])}
        ser = serpent.dumps(data, indent=True, set_literals=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""{
  'first': [
    1,
    2,
    (
      'a',
      'b'
    )
  ],
  'second': {
    1: False
  },
  'third': {
    1,
    2
  }
}""", ser)


class TestFiledump(unittest.TestCase):
    def testFile(self):
        if sys.version_info < (3, 2):
            self.skipTest("testdatafile contains stuff that is not supported by ast.literal_eval on Python < 3.2")
        with open("testserpent.utf8.bin", "rb") as file:
            data = file.read()
        obj = serpent.loads(data)
        self.assertEqual(-3 + 8j, obj["numbers"][3])


class TestInterceptClass(unittest.TestCase):
    def testRegular(self):
        import pprint
        p = pprint.PrettyPrinter(stream="dummy", width=42)
        ser = serpent.dumps(p)
        data = serpent.loads(ser)
        self.assertEqual(42, data["_width"])
        self.assertEqual("PrettyPrinter", data["__class__"])

    def testIntercept(self):
        ex = ZeroDivisionError("wrong")
        ser = serpent.dumps(ex)
        data = serpent.loads(ser)
        # default behavior is to serialize the exception to a dict
        self.assertEqual({'__exception__': True, 'args': ('wrong',), '__class__': 'ZeroDivisionError', 'attributes': {}}, data)

        def custom_exception_translate(obj, serializer, stream, indent):
            serializer._serialize("custom_exception!", stream, indent)

        try:
            serpent.register_class(Exception, custom_exception_translate)
            ser = serpent.dumps(ex)
            data = serpent.loads(ser)
            self.assertEqual("custom_exception!", data)
        finally:
            serpent.unregister_class(Exception)


class Something(object):
    def __init__(self, name, value):
        self.name = name
        self.value = value

    def __getstate__(self):
        return ("bogus", "state")


class TestCustomClasses(unittest.TestCase):
    def testCustomClass(self):
        def something_serializer(obj, serializer, stream, level):
            d = {
                "__class__": "Something",
                "custom": True,
                "name": obj.name,
                "value": obj.value
            }
            serializer.ser_builtins_dict(d, stream, level)

        serpent.register_class(Something, something_serializer)
        s = Something("hello", 42)
        d = serpent.dumps(s)
        x = serpent.loads(d)
        self.assertEqual({"__class__": "Something", "custom": True, "name": "hello", "value": 42}, x)
        serpent.unregister_class(Something)
        d = serpent.dumps(s)
        x = serpent.loads(d)
        self.assertEqual(("bogus", "state"), x)

    def testUUID(self):
        uid = uuid.uuid4()
        string_uid = str(uid)
        ser = serpent.dumps(uid)
        x = serpent.loads(ser)
        self.assertEqual(string_uid, x)

        def custom_uuid_translate(obj, serp, stream, level):
            serp._serialize("custom_uuid!", stream, level)

        serpent.register_class(uuid.UUID, custom_uuid_translate)
        try:
            ser = serpent.dumps(uid)
            x = serpent.loads(ser)
            self.assertEqual("custom_uuid!", x)
        finally:
            serpent.unregister_class(uuid.UUID)


class TestPyro4(unittest.TestCase):
    def testException(self):
        try:
            hashlib.new("non-existing-hash-name")
            ev = None
        except:
            et, ev, etb = sys.exc_info()
            tb_lines = traceback.format_exception(et, ev, etb)
            ev._pyroTraceback = tb_lines
        ser = serpent.dumps(ev, module_in_classname=False)
        data = serpent.loads(ser)
        self.assertTrue(data["__exception__"])
        attrs = data["attributes"]
        self.assertIsInstance(attrs["_pyroTraceback"], list)
        tb_txt = "".join(attrs["_pyroTraceback"])
        self.assertTrue(tb_txt.startswith("Traceback"))
        self.assertTrue(data["args"][0].startswith("unsupported hash"))
        self.assertEqual("ValueError", data["__class__"])


class TestCyclic(unittest.TestCase):
    def testTupleOk(self):
        t = (1, 2, 3)
        d = (t, t, t)
        data = serpent.dumps(d)
        serpent.loads(data)

    def testListOk(self):
        t = [1, 2, 3]
        d = [t, t, t]
        data = serpent.dumps(d)
        serpent.loads(data)

    def testDictOk(self):
        t = {"a": 1}
        d = {"x": t, "y": t, "z": t}
        data = serpent.dumps(d)
        serpent.loads(data)

    def testListCycle(self):
        d = [1, 2, 3]
        d.append(d)
        with self.assertRaises(ValueError) as e:
            serpent.dumps(d)
        self.assertEqual("Circular reference detected (list)", str(e.exception))

    def testDictCycle(self):
        d = {"x": 1, "y": 2}
        d["d"] = d
        with self.assertRaises(ValueError) as e:
            serpent.dumps(d)
        self.assertEqual("Circular reference detected (dict)", str(e.exception))

    def testClassCycle(self):
        d = Cycle()
        d.make_cycle(d)
        with self.assertRaises(ValueError) as e:
            serpent.dumps(d)
        self.assertEqual("Circular reference detected (class)", str(e.exception))


class Cycle(object):
    def __init__(self):
        self.name = "cycle"
        self.ref = None

    def make_cycle(self, ref):
        self.ref = ref


class RegisterThread(threading.Thread):
    def __init__(self):
        super(RegisterThread, self).__init__()
        self.stop_running=False

    def run(self):
        i = 0
        while not self.stop_running:
            serpent.register_class(type("clazz %d" % i, (), {}), None)   # just register dummy serializer
            i += 1


class SerializationThread(threading.Thread):
    def __init__(self):
        super(SerializationThread, self).__init__()
        self.stop_running = False
        self.error = None

    def run(self):
        big_list = [Cycle() for _ in range(1000)]
        while not self.stop_running:
            try:
                _ = serpent.dumps(big_list)
            except RuntimeError as x:
                self.error = x
                print(x)
                break

@unittest.skip
class TestThreading(unittest.TestCase):
    def testThreadsafeTypeRegistrations(self):
        reg = RegisterThread()
        ser = SerializationThread()
        reg.daemon = ser.daemon = True
        reg.start()
        ser.start()
        time.sleep(1)
        reg.stop_running = ser.stop_running = True
        self.assertIsNone(ser.error)


class TestCollections(unittest.TestCase):
    @unittest.skipIf(sys.version_info < (2, 7), "collections.OrderedDict is python 2.7+")
    def testOrderedDict(self):
        o = collections.OrderedDict()
        o['apple'] = 1
        o['banana'] = 2
        o['orange'] = 3
        d = serpent.dumps(o)
        o2 = serpent.loads(d)
        self.assertEqual({"__class__": "OrderedDict", "items": [('apple', 1), ('banana', 2), ('orange', 3)]}, o2)

    def testNamedTuple(self):
        Point = collections.namedtuple('Point', ['x', 'y'])
        p = Point(11, 22)
        d = serpent.dumps(p)
        p2 = serpent.loads(d)
        if sys.version_info < (2, 7) or sys.platform == "cli":
            # named tuple serialization is unfortunately broken on python <2.7 or ironpython; it leaves out the actual values
            self.assertEqual({"__class__": "Point"}, p2)
        elif os.name == "java":
            # named tuple serialization is unfortunately broken on jython; it forgets about the order
            self.assertEqual({"__class__": "Point", "x": 11, "y": 22}, p2)
        elif sys.version_info >= (3, 3) or ((2, 7) <= sys.version_info < (3, 0)):
            # only these versions got it 100% right!
            self.assertEqual({"__class__": "Point", "items": [('x', 11), ('y', 22)]}, p2)
        else:
            # other versions forget about the order....
            self.assertEqual({"__class__": "Point", "x": 11, "y": 22}, p2)

    @unittest.skipIf(sys.version_info < (2, 7), "collections.Counter is python 2.7+")
    def testCounter(self):
        c = collections.Counter("even")
        d = serpent.dumps(c)
        c2 = serpent.loads(d)
        self.assertEqual({'e': 2, 'v': 1, 'n': 1}, c2)

    def testDeque(self):
        obj = collections.deque([1, 2, 3])
        d = serpent.dumps(obj)
        obj2 = serpent.loads(d)
        self.assertEqual([1, 2, 3], obj2)

    @unittest.skipIf(sys.version_info < (3, 3), "ChainMap is python 3.3+")
    def testChainMap(self):
        c = collections.ChainMap({"a": 1}, {"b": 2}, {"c": 3})
        d = serpent.dumps(c)
        c2 = serpent.loads(d)
        self.assertEqual({'__class__': 'ChainMap', 'maps': [{'a': 1}, {'b': 2}, {'c': 3}]}, c2)

    def testDefaultDict(self):
        dd = collections.defaultdict(list)
        dd['a'] = 1
        dd['b'] = 2
        d = serpent.dumps(dd)
        dd2 = serpent.loads(d)
        self.assertEqual({'a': 1, 'b': 2}, dd2)

    @unittest.skipIf(sys.version_info < (3, 0), "collections.UserDict is python 3.0+")
    def testUserDict(self):
        obj = collections.UserDict()
        obj['a'] = 1
        obj['b'] = 2
        d = serpent.dumps(obj)
        obj2 = serpent.loads(d)
        self.assertEqual({'a': 1, 'b': 2}, obj2)

    @unittest.skipIf(sys.version_info < (3, 0), "collections.UserList is python 3.0+")
    def testUserList(self):
        obj = collections.UserList([1, 2, 3])
        d = serpent.dumps(obj)
        obj2 = serpent.loads(d)
        self.assertEqual([1, 2, 3], obj2)

    @unittest.skipIf(sys.version_info < (3, 0), "collections.UserString is python 3.0+")
    def testUserString(self):
        obj = collections.UserString("test")
        d = serpent.dumps(obj)
        obj2 = serpent.loads(d)
        self.assertEqual("test", obj2)


if __name__ == '__main__':
    unittest.main()
