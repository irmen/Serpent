from __future__ import print_function
import timeit
import sys

class Person(object):
    def __init__(self, name, age):
        self.name = name
        self.age = age


data = {

    "bytes": b"0123456789abcdefghijklmnopqrstuvwxyz" * 1000,
    "bytearray": bytearray(b"0123456789abcdefghijklmnopqrstuvwxyz") * 1000,
    "str": "\"0123456789\"\n'abcdefghijklmnopqrstuvwxyz'\t" * 1000,
    "unicode": u"abc\u20ac" * 10 * 1000,
    "int": [123456789] * 1000,
    "double": [12345.987654321] * 1000,
    "long": [123456789123456789123456789123456789] * 1000,
    "tuple": [(x*x, "tuple", (300, 400, (500, 600, (x*x, x*x, x*x, x*x)))) for x in range(200)],
    "list": [[x*x, "tuple", [300, 400, [500, 600, [x*x, x*x, x*x, x*x]]]] for x in range(200)],
    "set": set(x*x for x in range(1000)),
    "dict": {i*i: {1000+j: chr(j+65) for j in range(5)} for i in range(100)},
    "exception": [ZeroDivisionError("test exeception", x*x) for x in range(1000)],
    "class": [Person("harry", x*x) for x in range(1000)]
}

serializers = {}
try:
    import pickle
    serializers["pickle"] = pickle.dumps
except ImportError:
    pass
try:
    import cPickle
    serializers["cpickle"] = cPickle.dumps
except ImportError:
    pass
import json
serializers["json"] = json.dumps
import serpent
serializers["serpent"] = serpent.dumps
import marshal
serializers["marshal"] = marshal.dumps
try:
    import xmlrpclib
    def xmldumps(data):
        return xmlrpclib.dumps((data,))
    serializers["xmlrpc"]=xmldumps
except ImportError:
    pass
try:
    import xmlrpclib as xmlrpc
except ImportError:
    import xmlrpc.client as xmlrpc
def xmldumps(data):
    return xmlrpc.dumps((data,))
serializers["xmlrpc"]=xmldumps



def run():
    results = {}
    number = 10
    repeat = 3
    for ser in serializers:
        print("serializer:", ser)
        results[ser] = {"sizes": {}, "timings": {}}
        for key in sorted(data):
            print(key, end="; ")
            sys.stdout.flush()
            try:
                serialized = serializers[ser](data[key])
            except (TypeError, ValueError, OverflowError) as x:
                print("error!")
                results[ser]["sizes"][key] = 0
                results[ser]["timings"][key] = 0
            else:
                results[ser]["sizes"][key] = len(serialized)
                durations = [
                                timeit.timeit("_=serializers['%s'](data[key])" % ser, "from performance import data, serializers; key='%s'" % key, number=number)
                                for _ in range(repeat)
                            ]
                duration = min(durations)
                results[ser]["timings"][key] = round(duration * 1e6 / number, 2)
        print()
    return results


def tables_size(results):
    print("\nSIZE RESULTS\n")
    sizes_per_datatype = {}
    for ser in results:
        for datatype in results[ser]["sizes"]:
            size = results[ser]["sizes"][datatype]
            if datatype not in sizes_per_datatype:
                sizes_per_datatype[datatype] = []
            sizes_per_datatype[datatype].append((size, ser))
    sizes_per_datatype = {datatype: sorted(sizes) for datatype, sizes in sizes_per_datatype.items()}
    for dt in sorted(sizes_per_datatype):
        print(dt)
        for pos, (size, serializer) in enumerate(sizes_per_datatype[dt]):
            print(" %2d: %-8s %6d" % (pos+1, serializer, size))
    print()

def tables_speed(results):
    print("\nSPEED RESULTS\n")
    durations_per_datatype = {}
    for ser in results:
        for datatype in results[ser]["sizes"]:
            duration = results[ser]["timings"][datatype]
            if datatype not in durations_per_datatype:
                durations_per_datatype[datatype] = []
            durations_per_datatype[datatype].append((duration, ser))
    durations_per_datatype = {datatype: sorted(durations) for datatype, durations in durations_per_datatype.items()}
    for dt in sorted(durations_per_datatype):
        print(dt)
        for pos, (duration, serializer) in enumerate(durations_per_datatype[dt]):
            print(" %2d: %-8s %6d" % (pos+1, serializer, duration))
    print()

if __name__=="__main__":
    results = run()
    tables_size(results)
    tables_speed(results)


