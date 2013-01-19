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
import serpent


class TestBasics(unittest.TestCase):
    def test_header(self):
        ser = serpent.serialize(None).decode("utf-8")
        header, _, rest = ser.partition("\n")
        version = "%s.%s" % sys.version_info[:2]
        self.assertEqual("# serpent utf-8 python" + version, header)

    def test_primitives(self):
        ser = serpent.serialize(None)
        self.assertTrue(type(ser) is bytes)
        _, _, data = ser.partition(b"\n")
        self.assertEqual(b"None", data)


class TestSpeed(unittest.TestCase):
    def setUp(self):
        self.data = {
            "simple": "hello",
            "bytes": bytes(100),
            "list": [1,2,3,4,5,6,7,8],
            "tuple": ( 1,2,3,4,5,6,7,8 ),
            "set": {1,2,3,4,5,6,7,8,9},
            "dict": { i: str(i)*4 for i in range(10)},
            "exc": ZeroDivisionError("fault"),
            "dates": [
                datetime.datetime.now(),
                datetime.time(),
                datetime.timedelta(seconds=500)
            ],
            "uuid": uuid.uuid4()
        }
    def test_ser_speed(self):
        print("serialize without indent:", timeit.timeit(lambda: serpent.serialize(self.data, False), number=5000))
        print("serialize with indent:", timeit.timeit(lambda: serpent.serialize(self.data,True), number=5000))
    def test_deser_speed(self):
        ser = serpent.serialize(self.data, False)
        print("deserialize without indent:", timeit.timeit(lambda: serpent.deserialize(ser), number=5000))
        ser = serpent.serialize(self.data, True)
        print("deserialize with indent:", timeit.timeit(lambda: serpent.deserialize(ser), number=5000))


class TestIndent(unittest.TestCase):
    def test_indent_primitive(self):
        data = 12345
        ser = serpent.serialize(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("12345", ser)

    def test_indent_containers(self):
        data = [1,2,3]
        ser = serpent.serialize(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""[
  1,
  2,
  3
]""", ser)
        data = (1,2,3)
        ser = serpent.serialize(data, indent=True).decode("utf-8")
        _, _, ser = ser.partition("\n")
        self.assertEqual("""(
  1,
  2,
  3
)""", ser)
        data = {1}
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
