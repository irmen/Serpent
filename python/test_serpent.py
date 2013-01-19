"""
Serpent: ast.literal_eval() compatible object tree serialization.

Copyright 2013, Irmen de Jong (irmen@razorvine.net)
This code is open-source, but licensed under the "MIT software license".
See http://opensource.org/licenses/MIT
"""
from __future__ import print_function, division
import unittest
import serpent
import sys


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


class TestIndent(unittest.TestCase):
    def test_indent(self):
        data = [1,2,{"one":1, "two": (None, None), "three": {"a","set"}},ZeroDivisionError()]
        ser = serpent.serialize(data, indent=True).decode("utf-8")
        print(ser)

if __name__ == '__main__':
    unittest.main()
