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


if __name__ == '__main__':
    unittest.main()
