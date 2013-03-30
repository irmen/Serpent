from __future__ import print_function
import serpent
import datetime
import sys

class CustomClass(object):
    def __init__(self, name, age):
        self.name = name
        self.age = age


def example():
    use_set_literals = sys.version_info >= (3, 2)  # check if we can use set literals

    data = {
        "tuple": (1,2,3),
        "date": datetime.datetime.now(),
        "set": {'a', 'b', 'c'},
        "class": CustomClass("Sally", 26)
    }

    # serialize the object
    ser = serpent.serialize(data, indent=True, set_literals=use_set_literals)
    # print it to the screen, but usually you'd save the bytes to a file or transfer them over a network connection
    print("Serialized data:")
    print(ser.decode("UTF-8"))

    # deserialize the bytes and print the objects
    obj = serpent.deserialize(ser)
    print("Deserialized data:")
    print("tuple:", obj["tuple"])
    print("date:", obj["date"])
    print("set:", obj["set"])
    clazz = obj["class"]
    print("class attributes: type={0} name={1} age={2}".format(
        clazz["__class__"], clazz["name"], clazz["age"]))


if __name__=="__main__":
    example()
