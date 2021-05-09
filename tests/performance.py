"""
Prints a comparison between different serializers.
Compares results based on size of the output, and time taken to (de)serialize.
"""

from timeit import default_timer as perf_timer
import sys
import datetime
import decimal
import uuid


class Person(object):
    def __init__(self, name, age):
        self.name = name
        self.age = age


guid = uuid.uuid4()

data = {
    "bytes": bytes(x for x in range(256)) * 300,
    "bytearray": bytearray(x for x in range(256)) * 300,
    "str": "\"0123456789\"\n'abcdefghijklmnopqrstuvwxyz'\t" * 2000,
    "unicode": u"abcdefghijklmnopqrstuvwxyz\u20ac\u20ac\u20ac\u20ac\u20ac" * 2000,
    "int": [123456789] * 1000,
    "double": [12345.987654321] * 1000,
    "long": [123456789123456789123456789123456789] * 1000,
    "tuple": [(x * x, "tuple", (300, 400, (500, 600, (x * x, x * x)))) for x in range(200)],
    "list": [[x * x, "list", [300, 400, [500, 600, [x * x]]]] for x in range(200)],
    "set": set(x * x for x in range(1000)),
    "dict": {str(i * i): {str(1000 + j): chr(j + 65) for j in range(5)} for i in range(100)},
    "exception": [ZeroDivisionError("test exeception", x * x) for x in range(1000)],
    "class": [Person("harry", x * x) for x in range(1000)],
    "datetime": [datetime.datetime.now() for x in range(1000)],
    "complex": [complex(x + x, x * x) for x in range(1000)],
    "decimal": [decimal.Decimal("1122334455667788998877665544332211.9876543212345678987654321123456789") for x in range(1000)],
    "uuid": [guid for x in range(1000)]
}

serializers = {}
try:
    import pickle
    serializers["pickle"] = (pickle.dumps, pickle.loads)
except ImportError:
    pass
try:
    import cPickle
    serializers["cpickle"] = (cPickle.dumps, cPickle.loads)
except ImportError:
    pass
import json
serializers["json"] = (lambda d: json.dumps(d).encode("utf-8"), lambda d: json.loads(d.decode("utf-8")))
import serpent
serializers["serpent"] = (serpent.dumps, serpent.loads)
import marshal
serializers["marshal"] = (marshal.dumps, marshal.loads)
try:
    import msgpack
    serializers["msgpack"] = (lambda d: msgpack.packb(d, use_bin_type=True), lambda d: msgpack.unpackb(d))
except ImportError:
    pass
try:
    import xmlrpclib as xmlrpc
except ImportError:
    import xmlrpc.client as xmlrpc


def xmldumps(data):
    return xmlrpc.dumps((data,)).encode("utf-8")


def xmlloads(data):
    return xmlrpc.loads(data.decode("utf-8"))[0]


serializers["xmlrpc"] = (xmldumps, xmlloads)


no_result = 9999999999


def run():
    results = {}
    number = 10
    repeat = 3
    for ser in serializers:
        print("serializer:", ser)
        results[ser] = {"sizes": {}, "ser-times": {}, "deser-times": {}}
        for key in sorted(data):
            print(key, end="; ")
            sys.stdout.flush()
            try:
                serialized = serializers[ser][0](data[key])
            except (TypeError, ValueError, OverflowError):
                print("error!")
                results[ser]["sizes"][key] = no_result
                results[ser]["ser-times"][key] = no_result
                results[ser]["deser-times"][key] = no_result
            else:
                results[ser]["sizes"][key] = len(serialized)
                durations_ser = []
                durations_deser = []
                serializer, deserializer = serializers[ser]
                serialized_data = serializer(data[key])
                for _ in range(repeat):
                    start = perf_timer()
                    for _ in range(number):
                        serializer(data[key])
                    durations_ser.append(perf_timer() - start)
                for _ in range(repeat):
                    start = perf_timer()
                    for _ in range(number):
                        deserialized_data = deserializer(serialized_data)
                        if type(deserialized_data) is dict:
                            encoding = deserialized_data.get("encoding")
                            if encoding=="base64":
                                deserialized_data = serpent.tobytes(deserialized_data)
                    durations_deser.append(perf_timer() - start)
                duration_ser = min(durations_ser)
                duration_deser = min(durations_deser)
                results[ser]["ser-times"][key] = round(duration_ser * 1e6 / number, 2)
                results[ser]["deser-times"][key] = round(duration_deser * 1e6 / number, 2)
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
            if size == no_result:
                size = "unsupported"
            else:
                size = "%8d" % size
            print(" %2d: %-8s  %s" % (pos + 1, serializer, size))
    print()


def tables_speed(results, what_times, header):
    print("\n%s\n" % header)
    durations_per_datatype = {}
    for ser in results:
        for datatype in results[ser]["sizes"]:
            duration = results[ser][what_times][datatype]
            if datatype not in durations_per_datatype:
                durations_per_datatype[datatype] = []
            durations_per_datatype[datatype].append((duration, ser))
    durations_per_datatype = {datatype: sorted(durations) for datatype, durations in durations_per_datatype.items()}
    for dt in sorted(durations_per_datatype):
        print(dt)
        for pos, (duration, serializer) in enumerate(durations_per_datatype[dt]):
            if duration == no_result:
                duration = "unsupported"
            else:
                duration = "%8d" % duration
            print(" %2d: %-8s  %s" % (pos + 1, serializer, duration))
    print()


if __name__ == "__main__":
    results = run()
    tables_size(results)
    tables_speed(results, "ser-times", "SPEED RESULTS (SERIALIZATION)")
    tables_speed(results, "deser-times", "SPEED RESULTS (DESERIALIZATION)")
