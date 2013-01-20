"""
Serpent: ast.literal_eval() compatible object tree serialization.

Copyright 2013, Irmen de Jong (irmen@razorvine.net)
This code is open-source, but licensed under the "MIT software license".
See http://opensource.org/licenses/MIT
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


def strip_header(ser):
    if sys.platform == "cli":
        _, _, data = ser.partition("\n")
    else:
        _, _, data = ser.partition(b"\n")
    return data


class TestBasics(unittest.TestCase):
    def test_header(self):
        ser = serpent.serialize(None)
        if sys.platform == "cli":
            header, _, rest = ser.partition("\n")
        else:
            self.assertTrue(type(ser) is bytes)
            header, _, rest = ser.partition(b"\n")
        hdr = "# serpent utf-8 python%s.%s" % sys.version_info[:2]
        hdr = hdr.encode("utf-8")
        self.assertEqual(hdr, header)

    def test_none(self):
        ser = serpent.serialize(None)
        data = strip_header(ser)
        self.assertEqual(b"None", data)

    def test_string(self):
        ser = serpent.serialize("hello")
        data = strip_header(ser)
        self.assertEqual(b"'hello'", data)
        ser = serpent.serialize("quotes'\"")
        data = strip_header(ser)
        self.assertEqual(b"'quotes\\'\"'", data)
        ser = serpent.serialize("quotes2'")
        data = strip_header(ser)
        self.assertEqual(b"\"quotes2'\"", data)

    def test_numbers(self):
        ser = serpent.serialize(12345)
        data = strip_header(ser)
        self.assertEqual(b"12345", data)
        ser = serpent.serialize(123456789123456789123456789)
        data = strip_header(ser)
        self.assertEqual(b"123456789123456789123456789", data)
        ser = serpent.serialize(99.1234)
        data = strip_header(ser)
        self.assertEqual(b"99.1234", data)
        ser = serpent.serialize(decimal.Decimal("1234.9999999999"))
        data = strip_header(ser)
        self.assertEqual(b"'1234.9999999999'", data)
        ser = serpent.serialize(2+3j)
        data = strip_header(ser)
        self.assertEqual(b"(2+3j)", data)

    def test_others(self):
        ser = serpent.serialize(True)
        data = strip_header(ser)
        self.assertEqual(b"True", data)

    def test_bytes(self):
        if sys.version_info >= (3, 0):
            ser = serpent.serialize(bytes(b"abcdef"))
            data = serpent.deserialize(ser)
            self.assertEqual({'encoding': 'base64', 'data': 'YWJjZGVm'}, data)
        ser = serpent.serialize(bytearray(b"abcdef"))
        data = serpent.deserialize(ser)
        self.assertEqual({'encoding': 'base64', 'data': 'YWJjZGVm'}, data)
        if sys.version_info >= (2, 7):
            ser = serpent.serialize(memoryview(b"abcdef"))
            data = serpent.deserialize(ser)
            self.assertEqual({'encoding': 'base64', 'data': 'YWJjZGVm'}, data)

    def test_class(self):
        class Class1(object):
            def __init__(self):
                self.attr = 1
        class Class2(object):
            def __getstate__(self):
                return {"attr": 42}
        c = Class1()
        ser = serpent.serialize(c)
        data = serpent.deserialize(ser)
        self.assertEqual({'__class__': 'Class1', 'attr': 1}, data)
        c = Class2()
        ser = serpent.serialize(c)
        data = serpent.deserialize(ser)
        self.assertEqual({'attr': 42}, data)

    def test_array(self):
        ser = serpent.serialize(array.array('u', u"unicode"))
        data = strip_header(ser)
        self.assertEqual(b"'unicode'", data)
        ser = serpent.serialize(array.array('i', [44, 45, 46]))
        data = strip_header(ser)
        self.assertEqual(b"[44,45,46]", data)
        if sys.version_info < (3, 0):
            ser = serpent.serialize(array.array('c', "normal"))
            data = strip_header(ser)
            self.assertEqual(b"'normal'", data)

    def test_time(self):
        ser = serpent.serialize(datetime.datetime(2013, 1, 20, 23, 59, 45, 999888))
        data = strip_header(ser)
        self.assertEqual(b"'2013-01-20T23:59:45.999888'", data)
        ser = serpent.serialize(datetime.time(23, 59, 45, 999888))
        data = strip_header(ser)
        self.assertEqual(b"'23:59:45.999888'", data)
        ser = serpent.serialize(datetime.time(23, 59, 45))
        data = strip_header(ser)
        self.assertEqual(b"'23:59:45'", data)
        ser = serpent.serialize(datetime.timedelta(1, 4000, 999888, minutes=22))
        data = strip_header(ser)
        self.assertEqual(b"91720.999888", data)
        ser = serpent.serialize(datetime.timedelta(seconds=12345))
        data = strip_header(ser)
        self.assertEqual(b"12345.0", data)


class TestSpeed(unittest.TestCase):
    def setUp(self):
        self.data = {
            "str": "hello",
            "unicode": u"\u20ac",
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
        print("serialize without indent:", timeit.timeit(lambda: serpent.serialize(self.data, False), number=1000))
        print("serialize with indent:", timeit.timeit(lambda: serpent.serialize(self.data, True), number=1000))

    def test_deser_speed(self):
        ser = serpent.serialize(self.data, False)
        print("deserialize without indent:", timeit.timeit(lambda: serpent.deserialize(ser), number=1000))
        ser = serpent.serialize(self.data, True)
        print("deserialize with indent:", timeit.timeit(lambda: serpent.deserialize(ser), number=1000))


class TestIndent(unittest.TestCase):
    def test_indent_primitive(self):
        data = 12345
        ser = serpent.serialize(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("12345", ser)

    def test_indent_containers(self):
        data = [1, 2, 3]
        ser = serpent.serialize(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""[
  1,
  2,
  3
]""", ser)
        data = (1, 2, 3)
        ser = serpent.serialize(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""(
  1,
  2,
  3
)""", ser)
        data = set([1])
        ser = serpent.serialize(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        if sys.version_info < (3, 2):
            self.assertEqual("""(
  1,
)""", ser)
        else:
            self.assertEqual("""{
  1
}""", ser)
        data = {"one": 1}
        ser = serpent.serialize(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""{
  'one': 1
}""", ser)


if __name__ == '__main__':
    unittest.main()
