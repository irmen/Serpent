using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

namespace Razorvine.Serpent.Test
{
    public class CycleTest
    {
        [Fact]
        public void testTupleOk()
        {
            var ser = new Serializer();
            var t = new[] {1, 2, 3};
            var d = new object[] {t, t, t};
            var data = ser.Serialize(d);
            var parser = new Parser();
            parser.Parse(data);
        }

        [Fact]
        public void testListOk()
        {
            var ser = new Serializer();
            var t = new List<int> {1, 2, 3};
            var d = new List<object> {t, t, t};
            var data = ser.Serialize(d);
            var parser = new Parser();
            parser.Parse(data);
        }

        [Fact]
        public void testDictOk()
        {
            var ser = new Serializer();
            var t = new Hashtable {["a"] = 1};
            var d = new Hashtable
            {
                ["x"] = t,
                ["y"] = t,
                ["z"] = t
            };
            var data = ser.Serialize(d);
            var parser = new Parser();
            parser.Parse(data);
        }

        [Fact]
        public void testListCycle()
        {
            var ser = new Serializer();
            var d = new List<object> {1, 2};
            d.Add(d);
            Assert.Throws<ArgumentException>(() => ser.Serialize(d));
        }

        [Fact]
        public void testDictCycle()
        {
            var ser = new Serializer();
            var d = new Hashtable
            {
                ["x"] = 1,
                ["y"] = 2
            };
            d["z"] = d;
            Assert.Throws<ArgumentException>(() => ser.Serialize(d));
        }

        [Fact]
        public void testClassCycle()
        {
            var ser = new Serializer();
            var d = new SerializeTestClass
            {
                x = 42,
                i = 99,
                s = "hello"
            };
            d.obj = d; // make cycle

            try
            {
                ser.Serialize(d);
                throw new Exception("should not reach this");
            }
            catch (ArgumentException x)
            {
                Assert.Contains("nesting too deep", x.Message);
            }
        }

        [Fact]
        public void testMaxLevel()
        {
            var ser = new Serializer();
            Assert.Equal(500, ser.MaximumLevel);

            object[] array =
            {
                "level1",
                new object[]
                {
                    "level2",
                    new object[]
                    {
                        "level3",
                        new object[]
                        {
                            "level 4"
                        }
                    }
                }
            };

            ser.MaximumLevel = 4;
            ser.Serialize(array); // should work

            ser.MaximumLevel = 3;
            try
            {
                ser.Serialize(array);
                Assert.True(false, "should fail");
            }
            catch (ArgumentException x)
            {
                Assert.Contains("too deep", x.Message);
            }
        }
    }
}