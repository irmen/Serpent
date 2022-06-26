"""
Serpent: ast.literal_eval() compatible object tree serialization.
Software license: "MIT software license". See http://opensource.org/licenses/MIT
"""
import sys
import ast
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
import enum
import attr
import unittest
from collections.abc import KeysView, ValuesView, ItemsView
import serpent


def strip_header(ser):
    _, _, data = ser.partition(b"\n")
    return data


class TestDeserialize(unittest.TestCase):
    def test_deserialize(self):
        data = serpent.loads(b"555")
        self.assertEqual(555, data)

    def test_deserialize_chr(self):
        unicodestring = u"euro\u20ac"
        encoded = repr(unicodestring).encode("utf-8")
        data = serpent.loads(encoded)
        self.assertEqual(unicodestring, data)

    def test_weird_complex(self):
        c1 = complex(float('inf'), 4)
        ser = serpent.dumps(c1)
        c2 = serpent.loads(ser)
        self.assertEqual(c1, c2)
        c3 = serpent.loads(b"(1e30000+4.0j)")
        self.assertEqual(c1, c3)

    def test_trailing_commas(self):
        v = serpent.loads(b"[1,2,3,]")
        self.assertEqual([1, 2, 3], v)
        v = serpent.loads(b"(1,2,3,)")
        self.assertEqual((1, 2, 3), v)
        v = serpent.loads(b"{'a':1, 'b':2, 'c':3,}")
        self.assertEqual({'a': 1, 'b': 2, 'c': 3}, v)

    def test_trailing_comma_set(self):
        v = serpent.loads(b"{1,2,3,}")
        self.assertEqual({1, 2, 3}, v)

    def test_unicode_escapes(self):
        v = serpent.loads(b"'\\u20ac'")
        self.assertEqual(u"\u20ac", v)
        v = serpent.loads(b"'\\U00022001'")
        self.assertEqual(u"\U00022001", v)

    def test_input_types(self):
        bytes_input = b"'text'"
        bytearray_input = bytearray(bytes_input)
        memview_input = memoryview(bytes_input)
        self.assertEqual("text", serpent.loads(bytes_input))
        self.assertEqual("text", serpent.loads(bytearray_input))
        self.assertEqual("text", serpent.loads(memview_input))


class TestBasics(unittest.TestCase):

    def test_py2_py3_unicode_repr(self):
        data = u"hello\u20ac"
        py2repr = b"# serpent utf-8 python2.6\n'hello\\u20ac'"
        result = serpent.loads(py2repr)
        self.assertEqual(data, result, "must understand python 2.x repr form of unicode string")
        py3repr = b"# serpent utf-8 python3.2\n'hello\xe2\x82\xac'"
        try:
            result = serpent.loads(py3repr)
            self.assertEqual(data, result, "must understand python 3.x repr form of unicode string")
        except ValueError:
            self.fail("must parse it correctly")

    def test_header(self):
        ser = serpent.dumps(None)
        header, _, rest = ser.partition(b"\n")
        hdr = "# serpent utf-8 python3.2".encode("utf-8")
        self.assertEqual(hdr, header)

    def test_comments(self):
        ser = b"""# serpent utf-8 python3.2
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
        obj = {3, 4, 2, 1, 6, 5}
        ser = serpent.dumps(obj)
        data = strip_header(ser)
        self.assertEqual(13, len(data))
        ser = serpent.dumps(obj, indent=True)
        data = strip_header(ser)
        self.assertEqual(b"{\n  1,\n  2,\n  3,\n  4,\n  5,\n  6\n}", data)      # sorted

        obj = {3, "something"}
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

    def test_string_with_escapes(self):
        ser = serpent.dumps("\n")
        d = strip_header(ser)
        self.assertEqual(b"'\\n'", d)
        ser = serpent.dumps("\a")
        d = strip_header(ser)
        self.assertEqual(b"'\\x07'", d)     # repr() does this hex escape
        line = "'hello\nlastline\ttab\\@slash\a\b\f\n\r\t\v'"
        ser = serpent.dumps(line)
        d = strip_header(ser)
        self.assertEqual(b"\"'hello\\nlastline\\ttab\\\\@slash\\x07\\x08\\x0c\\n\\r\\t\\x0b'\"", d)    # the hex escapes are done by repr()
        data = serpent.loads(ser)
        self.assertEqual(line, data)

    def test_nullbytesstring(self):
        ser = serpent.dumps(u"\x00null")
        data = serpent.loads(ser)
        self.assertEqual("\x00null", data)
        ser = serpent.dumps(u"\x01")
        self.assertEqual(b"'\\x01'", strip_header(ser))
        data = serpent.loads(ser)
        self.assertEqual("\x01", data)
        ser = serpent.dumps(u"\x1f")
        self.assertEqual(b"'\\x1f'", strip_header(ser))
        data = serpent.loads(ser)
        self.assertEqual("\x1f", data)
        ser = serpent.dumps(u"\x20")
        self.assertEqual(b"' '", strip_header(ser))
        data = serpent.loads(ser)
        self.assertEqual(" ", data)

    def test_nullbytesstr(self):
        line = chr(0) + "null"
        ser = serpent.dumps(line)
        data = strip_header(ser)
        self.assertEqual(b"'\\x00null'", data, "must escape 0-byte")
        data = serpent.loads(ser)
        self.assertEqual(line, data)

    def test_detectNullByte(self):
        with self.assertRaises(ValueError) as ex:
            serpent.loads(b"'contains\x00nullbyte'")
            self.fail("must fail")
        self.assertTrue("0-bytes" in str(ex.exception))
        with self.assertRaises(ValueError) as ex:
            serpent.loads(bytearray(b"'contains\x00nullbyte'"))
            self.fail("must fail")
        self.assertTrue("0-bytes" in str(ex.exception))
        with self.assertRaises(ValueError) as ex:
            serpent.loads(memoryview(b"'contains\x00nullbyte'"))
            self.fail("must fail")
        self.assertTrue("0-bytes" in str(ex.exception))
        serpent.loads(bytearray(b"'contains no nullbyte'"))
        serpent.loads(memoryview(b"'contains no nullbyte'"))

    def test_unicode_U(self):
        u = "euro" + chr(0x20ac)+"\U00022001"
        self.assertTrue(type(u) is str)
        ser = serpent.dumps(u)
        data = serpent.loads(ser)
        self.assertEqual(u, data)

    def test_unicode_escape_allchars(self):
        # this checks for all 0x0000-0xffff chars that they will be serialized
        # into a proper repr form and when processed back by ast.literal_parse directly
        # will get turned back into the chars 0x0000-0xffff again
        highest_char = 0xffff
        all_chars = u"".join(chr(c) for c in range(highest_char+1))
        ser = serpent.dumps(all_chars)
        self.assertGreater(len(ser), len(all_chars))
        ser = ser.decode("utf-8")
        data = ast.literal_eval(ser)
        self.assertEqual(highest_char+1, len(data))
        for i, c in enumerate(data):
            if chr(i) != c:
                self.fail("char different for "+str(i))

    def test_unicode_quotes(self):
        ser = serpent.dumps(str("quotes'\""))
        data = strip_header(ser)
        self.assertEqual(b"'quotes\\'\"'", data)
        ser = serpent.dumps(str("quotes2'"))
        data = strip_header(ser)
        self.assertEqual(b"\"quotes2'\"", data)

    def test_utf8_correctness(self):
        u = u"\x00\x01\x80\x81\xfe\xffabcdef\u20ac"
        utf_8_correct = repr(u).encode("utf-8")
        if utf_8_correct.startswith(b"u"):
            utf_8_correct = utf_8_correct[1:]
        ser = serpent.dumps(u)
        d = strip_header(ser)
        self.assertEqual(utf_8_correct, d)

    def test_unicode_with_escapes_py3(self):
        ser = serpent.dumps(str("\n"))
        d = strip_header(ser)
        self.assertEqual(b"'\\n'", d)
        ser = serpent.dumps(str("\a"))
        d = strip_header(ser)
        self.assertEqual(b"'\\x07'", d)
        ser = serpent.dumps("\a"+chr(0x20ac))
        d = strip_header(ser)
        self.assertEqual(b"'\\x07\xe2\x82\xac'", d)
        line = "'euro" + chr(0x20ac) + "\nlastline\ttab\\@slash\a\b\f\n\r\t\v'"
        ser = serpent.dumps(line)
        d = strip_header(ser)
        self.assertEqual(b"\"'euro\xe2\x82\xac\\nlastline\\ttab\\\\@slash\\x07\\x08\\x0c\\n\\r\\t\\x0b'\"", d)
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
        if sys.platform == 'cli':
            self.assertEqual(b"99.123400000000004", data)
        else:
            self.assertEqual(b"99.1234", data)
        ser = serpent.dumps(decimal.Decimal("1234.9999999999"))
        data = strip_header(ser)
        self.assertEqual(b"'1234.9999999999'", data)
        ser = serpent.dumps(2 + 3j)
        data = strip_header(ser)
        self.assertEqual(b"(2.0+3.0j)", data)
        ser = serpent.dumps(2 - 3j)
        data = strip_header(ser)
        self.assertEqual(b"(2.0-3.0j)", data)

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
        self.assertEqual(ord("{"), data[0])
        self.assertEqual(ord("}"), data[-1])
        ser = serpent.dumps(mydict, indent=True)
        data = strip_header(ser)
        self.assertEqual(86, len(data))
        self.assertEqual(ord("{"), data[0])
        self.assertEqual(ord("}"), data[-1])

    def test_dict_str(self):
        data = {"key": str("value")}
        ser = serpent.dumps(data)
        data2 = serpent.loads(ser)
        self.assertEqual(str("value"), data2["key"])
        data = {str("key"): 123}
        ser = serpent.dumps(data)
        data2 = serpent.loads(ser)
        self.assertEqual(123, data2[str("key")])

    def test_dict_iters(self):
        data = {"john": 22, "sophie": 34, "bob": 26}
        ser = serpent.loads(serpent.dumps(data.keys()))
        self.assertIsInstance(ser, list)
        self.assertEqual(["bob", "john", "sophie"], sorted(ser))
        ser = serpent.loads(serpent.dumps(data.values()))
        self.assertIsInstance(ser, list)
        self.assertEqual([22, 26, 34], sorted(ser))
        ser = serpent.loads(serpent.dumps(data.items()))
        self.assertIsInstance(ser, list)
        self.assertEqual([("bob", 26), ("john", 22), ("sophie", 34)], sorted(ser))

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
        myset = {42, "Sally"}
        ser = serpent.dumps(myset)
        data = strip_header(ser)
        self.assertTrue(data == b"{42,'Sally'}" or data == b"{'Sally',42}")
        ser = serpent.dumps(myset, indent=True)
        data = strip_header(ser)
        self.assertTrue(data == b"{\n  42,\n  'Sally'\n}" or data == b"{\n  'Sally',\n  42\n}")
        # unicode elements
        data = {str("text1"), str("text2")}
        ser = serpent.dumps(data)
        data2 = serpent.loads(ser)
        self.assertEqual(2, len(data2))
        self.assertIn(str("text1"), data2)
        self.assertIn(str("text2"), data2)

    def test_bytes_default(self):
        ser = serpent.dumps(bytes(b"abcdef"))
        data = serpent.loads(ser)
        self.assertEqual({'encoding': 'base64', 'data': 'YWJjZGVm'}, data)
        ser = serpent.dumps(bytearray(b"abcdef"))
        data = serpent.loads(ser)
        self.assertEqual({'encoding': 'base64', 'data': 'YWJjZGVm'}, data)
        ser = serpent.dumps(memoryview(b"abcdef"))
        data = serpent.loads(ser)
        self.assertEqual({'encoding': 'base64', 'data': 'YWJjZGVm'}, data)

    def test_bytes_repr(self):
        ser = serpent.dumps(bytes(b"abcdef\xff"), bytes_repr=True)
        data = serpent.loads(ser)
        self.assertEqual(b'abcdef\xff', data)
        ser = serpent.dumps(bytearray(b"abcdef\xff"), bytes_repr=True)
        data = serpent.loads(ser)
        self.assertEqual(b'abcdef\xff', data)
        ser = serpent.dumps(memoryview(b"abcdef\xff"), bytes_repr=True)
        data = serpent.loads(ser)
        self.assertEqual(b'abcdef\xff', data)

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
        self.assertEqual({
            '__class__': "builtins.ZeroDivisionError",
            '__exception__': True,
            'args': ('wrong',),
            'attributes': {}
        }, data)

    def test_class_regular(self):
        c = Class1()
        ser = serpent.dumps(c)
        data = serpent.loads(ser)
        self.assertEqual({'__class__': 'Class1', 'attr': 1}, data)

    def test_class_getstate(self):
        c = Class2()
        ser = serpent.dumps(c)
        data = serpent.loads(ser)
        self.assertEqual({'attr': 42}, data)

    def test_class_slots(self):
        c = SlotsClass()
        ser = serpent.dumps(c)
        data = serpent.loads(ser)
        self.assertEqual({'__class__': 'SlotsClass', 'attr': 1}, data)

    def test_class_pprinter(self):
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

    def test_class_hashable_key_check(self):
        import pprint
        pp = pprint.PrettyPrinter(stream="dummy", width=42)
        with self.assertRaises(TypeError) as x:
            serpent.dumps({1: 1, 2: 1, 3: 1, strip_header: 1})    # can only serialize simple types as dict keys (hashable)
        self.assertTrue("hashable type" in str(x.exception))
        with self.assertRaises(TypeError) as x:
            serpent.dumps({1: 1, 2: 1, 3: 1, pp: 1})    # can only serialize simple types as dict keys (hashable)
        self.assertTrue("hashable type" in str(x.exception))

    def test_class_hashable_set_element_check(self):
        import pprint
        pp = pprint.PrettyPrinter(stream="dummy", width=42)
        with self.assertRaises(TypeError) as x:
            serpent.dumps({1, 2, 3, strip_header})     # can only serialize simple typles as set elements (hashable)
        self.assertTrue("hashable type" in str(x.exception))
        with self.assertRaises(TypeError) as x:
            serpent.dumps({1, 2, 3, pp})     # can only serialize simple typles as set elements (hashable)
        self.assertTrue("hashable type" in str(x.exception))

    def test_enum_hashable(self):
        class Color(enum.Enum):
            RED = 1
            GREEN = 2
            BLUE = 3
        data = serpent.dumps({"abc", Color.RED, Color.GREEN, Color.BLUE})
        orig = serpent.loads(data)
        self.assertEqual({"abc", 1, 2, 3}, orig)
        data = serpent.dumps({"abc": 1, Color.RED: 1, Color.GREEN: 1, Color.BLUE: 1})
        orig = serpent.loads(data)
        self.assertEqual({"abc": 1, 1: 1, 2: 1, 3: 1}, orig)

    def test_array(self):
        ser = serpent.dumps(array.array('u', str("unicode")))
        data = strip_header(ser)
        self.assertEqual(b"'unicode'", data)
        ser = serpent.dumps(array.array('i', [44, 45, 46]))
        data = strip_header(ser)
        self.assertEqual(b"[44,45,46]", data)
        ser = serpent.dumps(array.array('u', "normal"))
        data = strip_header(ser)
        self.assertEqual(b"'normal'", data)

    def test_time(self):
        ser = serpent.dumps(datetime.datetime(2013, 1, 20, 23, 59, 45, 999888))
        data = strip_header(ser)
        self.assertEqual(b"'2013-01-20T23:59:45.999888'", data)
        ser = serpent.dumps(datetime.date(2013, 1, 20))
        data = strip_header(ser)
        self.assertEqual(b"'2013-01-20'", data)
        ser = serpent.dumps(datetime.time(23, 59, 45, 999888))
        data = strip_header(ser)
        self.assertEqual(b"'23:59:45.999888'", data)
        ser = serpent.dumps(datetime.time(23, 59, 45))
        data = strip_header(ser)
        self.assertEqual(b"'23:59:45'", data)
        ser = serpent.dumps(datetime.timedelta(1, 4000, 999888, minutes=22))
        data = strip_header(ser)
        if sys.platform == 'cli':
            self.assertEqual(b"91720.999888000006", data)
        else:
            self.assertEqual(b"91720.999888", data)
        ser = serpent.dumps(datetime.timedelta(seconds=12345))
        data = strip_header(ser)
        self.assertEqual(b"12345.0", data)

    def test_timezone(self):
        import pytz    # requires pytz library
        tz_nl = pytz.timezone("Europe/Amsterdam")
        dt_tz = tz_nl.localize(datetime.datetime(2013, 1, 20, 23, 59, 45, 999888))
        ser = serpent.dumps(dt_tz)
        data = strip_header(ser)
        self.assertEqual(b"'2013-01-20T23:59:45.999888+01:00'", data)   # normal time
        dt_tz = tz_nl.localize(datetime.datetime(2013, 5, 10, 13, 59, 45, 999888))
        ser = serpent.dumps(dt_tz)
        data = strip_header(ser)
        self.assertEqual(b"'2013-05-10T13:59:45.999888+02:00'", data)   # daylight saving time

    def test_pickle_api(self):
        ser = serpent.dumps([1, 2, 3])
        serpent.loads(ser)
        tmpfn = tempfile.mktemp()
        with open(tmpfn, "wb") as outf:
            serpent.dump([1, 2, 3], outf, indent=True)
        with open(tmpfn, "rb") as inf:
            data = serpent.load(inf)
            self.assertEqual([1, 2, 3], data)
        os.remove(tmpfn)

    def test_weird_floats(self):
        values = [float('inf'), float('-inf'), float('nan'), complex(float('inf'), 4)]
        ser = serpent.dumps(values)
        ser = strip_header(serpent.dumps(values))
        self.assertEqual(b"[1e30000,-1e30000,{'__class__':'float','value':'nan'},(1e30000+4.0j)]", ser)
        values2 = serpent.loads(ser)
        self.assertEqual([float('inf'), float('-inf'), {'__class__': 'float', 'value': 'nan'}, (float('inf')+4j)], values2)
        values2 = serpent.loads(b"[1e30000,-1e30000]")
        self.assertEqual([float('inf'), float('-inf')], values2)

    def test_float_precision(self):
        # make sure we don't lose precision when converting floats (including scientific notation)
        v = serpent.loads(serpent.dumps(1.2345678987654321))
        self.assertEqual(1.2345678987654321, v)
        v = serpent.loads(serpent.dumps(5555.12345678987656))
        self.assertEqual(5555.12345678987656, v)
        v = serpent.loads(serpent.dumps(98765432123456.12345678987656))
        self.assertEqual(98765432123456.12345678987656, v)
        v = serpent.loads(serpent.dumps(98765432123456.12345678987656e+44))
        self.assertEqual(98765432123456.12345678987656e+44, v)
        v = serpent.loads(serpent.dumps((98765432123456.12345678987656e+44+665544332211.9998877665544e+33j)))
        self.assertEqual((98765432123456.12345678987656e+44+665544332211.9998877665544e+33j), v)
        v = serpent.loads(serpent.dumps((-98765432123456.12345678987656e+44 -665544332211.9998877665544e+33j)))
        self.assertEqual((-98765432123456.12345678987656e+44 -665544332211.9998877665544e+33j), v)

    def test_enums(self):
        class Animal(enum.Enum):
            BEE = 1
            CAT = 2
            DOG = 3
        v = serpent.loads(serpent.dumps(Animal.CAT))
        self.assertEqual(2, v)
        class Animal2(enum.Enum):
            BEE = 1
            CAT = 2
            DOG = 3
            HORSE = 4
            RABBIT = 5
        v = serpent.loads(serpent.dumps(Animal2.HORSE))
        self.assertEqual(4, v)

    def test_tobytes(self):
        obj = b"test"
        self.assertIs(obj, serpent.tobytes(obj))
        obj = memoryview(b"test")
        self.assertIs(obj, serpent.tobytes(obj))
        obj = bytearray(b"test")
        self.assertIs(obj, serpent.tobytes(obj))
        ser = {'data': 'dGVzdA==', 'encoding': 'base64'}
        out = serpent.tobytes(ser)
        self.assertEqual(b"test", out)
        self.assertIsInstance(out, bytes)
        with self.assertRaises(TypeError):
            serpent.tobytes({'@@@data': 'dGVzdA==', 'encoding': 'base64'})
        with self.assertRaises(TypeError):
            serpent.tobytes({'data': 'dGVzdA==', '@@@encoding': 'base64'})
        with self.assertRaises(TypeError):
            serpent.tobytes({'data': 'dGVzdA==', 'encoding': 'base99'})
        with self.assertRaises(TypeError):
            serpent.tobytes({})
        with self.assertRaises(TypeError):
            serpent.tobytes(42)


@unittest.skip("no performance tests in default test suite")
class TestSpeed(unittest.TestCase):
    def setUp(self):
        self.data = {
            "str": "hello",
            "unicode": chr(0x20ac),  # euro-character
            "numbers": [123456789012345678901234567890, 999.1234, decimal.Decimal("1.99999999999999999991")],
            "bytes": bytearray(100),
            "list": [1, 2, 3, 4, 5, 6, 7, 8, 1.1, 2.2, 3.3, 4.4, 5.5, 6.6, 7.7, 8.8, 9.9],
            "tuple": (1, 2, 3, 4, 5, 6, 7, 8),
            "set": {1, 2, 3, 4, 5, 6, 7, 8, 9},
            "dict": dict((i, str(i) * 4) for i in range(10)),
            "exc": ZeroDivisionError("fault"),
            "dates": [
                datetime.datetime.now(),
                datetime.date.today(),
                datetime.time(23, 59, 45, 999888),
                datetime.timedelta(seconds=500)
            ],
            "uuid": uuid.uuid4()
        }
        self.floatlist = [12345.6789] * 1000

    def test_ser_speed(self):
        print("serialize without indent:", timeit.timeit(lambda: serpent.dumps(self.data, False), number=1000))
        print("serialize long list of floats:", timeit.timeit(lambda: serpent.dumps(self.floatlist, False), number=100))
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
        data = {1}
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

        data = {"first": [1, 2, ("a", "b")], "second": {1: False}, "third": {1, 2}}
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
        datafile = "testserpent.utf8.bin"
        if not os.path.exists(datafile):
            mypath = os.path.split(__file__)[0]
            datafile = os.path.join(mypath, datafile)
        with open(datafile, "rb") as dfile:
            data = dfile.read()
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


class BaseClass(object):
    pass


class SubClass(BaseClass):
    pass


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

    def testSubclass(self):
        def custom_serializer(obj, serializer, stream, level):
            serializer._serialize("[(sub)class=%s]" % type(obj), stream, level)
        serpent.register_class(BaseClass, custom_serializer)
        s = SubClass()
        d = serpent.dumps(s)
        x = serpent.loads(d)
        classname = __name__+".SubClass"
        self.assertEqual("[(sub)class=<class '"+classname+"'>]", x)

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

    def testRegisterOrderPreserving(self):
        serpent._reset_special_classes_registry()
        serpent.register_class(BaseClass, lambda: None)
        serpent.register_class(SubClass, lambda: None)
        classes = list(serpent._special_classes_registry)
        self.assertEqual(KeysView, classes.pop(0))
        self.assertEqual(ValuesView, classes.pop(0))
        self.assertEqual(ItemsView, classes.pop(0))
        self.assertEqual(collections.OrderedDict, classes.pop(0))
        self.assertEqual(enum.Enum, classes.pop(0))
        self.assertEqual(BaseClass, classes.pop(0))
        self.assertEqual(SubClass, classes.pop(0))
        self.assertEqual(0, len(classes))


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

    # noinspection PyUnreachableCode
    def testMaxLevel(self):
        ser = serpent.Serializer()
        self.assertGreater(ser.maximum_level, 10)   # old Pypy appears to have a very low default recursionlimit
        array=[]
        arr=array
        for level in range(min(sys.getrecursionlimit()+10, 2000)):
            arr.append("level"+str(level))
            arr2 = []
            arr.append(arr2)
            arr=arr2
        with self.assertRaises(ValueError) as x:
            ser.serialize(array)
            self.fail("should crash")
        self.assertTrue("too deep" in str(x.exception))
        # check setting the maxlevel
        array = ["level1", ["level2", ["level3", ["level4"]]]]
        ser.maximum_level = 4
        ser.serialize(array)    # should work
        ser.maximum_level = 3
        with self.assertRaises(ValueError) as x:
            ser.serialize(array)    # should crash
            self.fail("should crash")
        self.assertTrue("too deep" in str(x.exception))


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
        self.assertEqual((11, 22), p2)

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

    def testUserDict(self):
        obj = collections.UserDict()
        obj['a'] = 1
        obj['b'] = 2
        d = serpent.dumps(obj)
        obj2 = serpent.loads(d)
        self.assertEqual({'a': 1, 'b': 2}, obj2)

    def testUserList(self):
        obj = collections.UserList([1, 2, 3])
        d = serpent.dumps(obj)
        obj2 = serpent.loads(d)
        self.assertEqual([1, 2, 3], obj2)

    def testUserString(self):
        obj = collections.UserString("test")
        d = serpent.dumps(obj)
        obj2 = serpent.loads(d)
        self.assertEqual("test", obj2)


class DataclassesTests(unittest.TestCase):

    # unfortunately, python 3.7 dataclasses are a syntax error in older python versions
    # def testDataclasses(self):
    #     @dataclass
    #     class InventoryItem:
    #         name: str
    #         unit_price: float
    #         untyped: str
    #         quantity_on_hand: int = 0
    #     item = InventoryItem("television", 1899.95, untyped="untyped", quantity_on_hand=5)
    #     ser = serpent.dumps(item)
    #     item2 = serpent.loads(ser)
    #     self.assertDictEqual({"__class__": "InventoryItem", "name": "television", "quantity_on_hand": 5,
    #                           "unit_price": 1899.95, "untyped": "untyped"}, item2)

    def testAttr(self):
        @attr.s
        class InventoryItem(object):
            name = attr.ib(type=str)
            unit_price = attr.ib(type=float)
            quantity_on_hand = attr.ib(type=int)
            untyped = attr.ib()
        item = InventoryItem("television", 1899.95, untyped="untyped", quantity_on_hand=5)
        ser = serpent.dumps(item)
        item2 = serpent.loads(ser)
        self.assertDictEqual({"__class__": "InventoryItem", "name": "television", "quantity_on_hand": 5,
                              "unit_price": 1899.95, "untyped": "untyped"}, item2)

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


if __name__ == '__main__':
    unittest.main()
