"""
Serpent: ast.literal_eval() compatible object tree serialization.

Copyright 2013, Irmen de Jong (irmen@razorvine.net)
Software license: "MIT software license". See http://opensource.org/licenses/MIT
"""
from __future__ import print_function, division
import unittest
import sys
import timeit
import datetime
import uuid
import decimal
import array
import serpent
import tempfile
import os


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
        ser = serpent.dumps(None)
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
        self.assertEqual([1,2,3,4], data)

        ser = b"[ 1, 2 ]"       # no header whatsoever
        data = serpent.loads(ser)
        self.assertEqual([1,2], data)

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
        ser = serpent.dumps(obj, indent=True)
        data = strip_header(ser)
        self.assertEqual(b"{\n  1,\n  2,\n  3,\n  4,\n  5,\n  6\n}", data)      # sorted

        obj = set([3, "something"])
        ser = serpent.dumps(obj, indent=False)
        data = strip_header(ser)
        self.assertTrue(data == b"{3,'something'}" or data == b"{'something',3}")
        ser = serpent.dumps(obj, indent=True)
        data = strip_header(ser)
        self.assertTrue(data == b"{\n  3,\n  'something'\n}" or data == b"{\n  'something',\n  3\n}")

        obj = {3: "three", "something": 99}
        ser = serpent.dumps(obj, indent=False)
        data = strip_header(ser)
        self.assertTrue(data == b"{'something':99,3:'three'}" or data == b"{3:'three','something':99}")
        ser = serpent.dumps(obj, indent=True)
        data = strip_header(ser)
        self.assertTrue(data == b"{\n  'something': 99,\n  3: 'three'\n}" or data == b"{\n  3: 'three',\n  'something': 99\n}")

        obj = {3: "three", 4: "four", 5: "five", 2: "two", 1: "one"}
        ser = serpent.dumps(obj, indent=True)
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
        ser = serpent.dumps(2+3j)
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

        myset = set([42, "Sally"])
        ser = serpent.dumps(myset)
        data = strip_header(ser)
        self.assertTrue(data == b"{42,'Sally'}" or data == b"{'Sally',42}")
        ser = serpent.dumps(myset, indent=True)
        data = strip_header(ser)
        self.assertTrue(data == b"{\n  42,\n  'Sally'\n}" or data == b"{\n  'Sally',\n  42\n}")

        # test no set-literals
        ser = serpent.dumps(myset, set_literals=False)
        data = strip_header(ser)
        self.assertTrue(data==b"(42,'Sally')" or data==b"('Sally',42)")    # must output a tuple instead of a set-literal

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
        ser = serpent.dumps([1,2,3])
        serpent.loads(ser)
        tmpfn = tempfile.mktemp()
        with open(tmpfn, "wb") as outf:
            serpent.dump([1,2,3], outf, indent=True, set_literals=True)
        with open(tmpfn, "rb") as inf:
            data = serpent.load(inf)
            self.assertEqual([1,2,3], data)
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
        use_set_literals = sys.version_info >= (3, 2)
        ser = serpent.dumps(self.data, False, set_literals=use_set_literals)
        print("deserialize without indent:", timeit.timeit(lambda: serpent.loads(ser), number=1000))
        ser = serpent.dumps(self.data, True, set_literals=use_set_literals)
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
        data = set("irmen de jong irmen de jong")
        ser = serpent.dumps(data, False)
        ser = strip_header(ser)
        self.assertNotEqual(b"' ','d','e','g','i','j','m','n','o','r'", ser[1:-1])
        ser = serpent.dumps(data, True)
        ser = strip_header(ser)
        self.assertEqual(b"\n  ' ',\n  'd',\n  'e',\n  'g',\n  'i',\n  'j',\n  'm',\n  'n',\n  'o',\n  'r'\n", ser[1:-1])

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
        ser = serpent.dumps(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""{
  1
}""", ser)
        data = {"one": 1}
        ser = serpent.dumps(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""{
  'one': 1
}""", ser)

        data = {"first": [1, 2, ("a", "b")], "second": {1: False}, "third": set([1,2])}
        ser = serpent.dumps(data, indent=True).decode("utf-8")
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
        self.assertEqual(-3+8j, obj["numbers"][3])


if __name__ == '__main__':
    unittest.main()
