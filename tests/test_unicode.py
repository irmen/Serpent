from __future__ import print_function
import sys
import serpent
import platform

if sys.version_info>=(3,0):
    unichr = chr

teststrings = [
    u"",
    u"abc",
    u"\u20ac",
    u"\x00\x01\x80\x81\xfe\xff\u20ac\u4444\u0240slashu:\\uend.\\u20ac(no euro!)\\U00022001bigone"
]

large = u"".join(unichr(i) for i in range(256))
teststrings.append(large)
large = u"".join(unichr(i) for i in range(0x20ac+1))
teststrings.append(large)


def main():
    impl=platform.python_implementation()+"_{0}_{1}".format(sys.version_info[0], sys.version_info[1])
    print("IMPL:", impl)

    with open("data_inputs_utf8.txt", "wb") as out:
        for source in teststrings:
            out.write(source.encode("utf-8")+b"\n")

    results = []
    ser = serpent.Serializer()
    with open("data_"+impl+".serpent", "wb") as out:
        for i, source in enumerate(teststrings):
            data = ser.serialize(source)
            out.write(data)
            out.write(b"~\n~\n")
            assert b"\x00" not in data
            results.append(data)

    assert len(results)==len(teststrings)
    for i, source in enumerate(teststrings):
        print(i)
        result = serpent.loads(results[i])
        if source!=result:
            print("ERRROR!!! RESULT AFTER serpent.loads IS NOT CORRECT!")
            print("SOURCE:",repr(source))
            print("RESULT:",repr(result))
            return
    print("OK")

if __name__ == "__main__":
    main()
