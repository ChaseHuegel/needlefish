using System;
using System.Collections.Generic;

using Needlefish.Types;

using Xunit;
using Xunit.Abstractions;

namespace Needlefish.Tests
{
    public class NeedlefishFormatterTests
    {
        private readonly ITestOutputHelper Output;
        public NeedlefishFormatterTests(ITestOutputHelper testOutputHelper)
        {
            Output = testOutputHelper;
        }

        [Fact]
        public void Benchmark()
        {
            TestObject testObject = new TestObject();
            TestObject testObject2 = new TestObject();
            byte[] bytes = null;
            
            Utils.Benchmark(Output, () => {
                bytes = NeedlefishFormatter.Serialize(testObject);
                NeedlefishFormatter.Populate(testObject, bytes);
            });

            Output.WriteLine($"Buffer: {bytes.Length}b");
        }

        [Fact]
        public void BenchmarkSerialize()
        {
            TestObject testObject = new TestObject();
            byte[] bytes = null;
            
            Utils.Benchmark(Output, () => {
                bytes = NeedlefishFormatter.Serialize(testObject);
            });

            Output.WriteLine($"Buffer: {bytes.Length}b");
        }

        [Fact]
        public void BenchmarkDeserialize()
        {
            TestObject testObject = new TestObject();
            byte[] bytes = NeedlefishFormatter.Serialize(testObject);
            
            Utils.Benchmark(Output, () => {
                testObject = NeedlefishFormatter.Deserialize<TestObject>(bytes);
            });

            Output.WriteLine($"Buffer: {bytes.Length}b");
        }

        [Fact]
        public void BenchmarkPopulate()
        {
            TestObject testObject = new TestObject();
            byte[] bytes = NeedlefishFormatter.Serialize(testObject);
            
            Utils.Benchmark(Output, () => {
                NeedlefishFormatter.Populate(testObject, bytes);
            });

            Output.WriteLine($"Buffer: {bytes.Length}b");
        }

        [Fact]
        public void TestSerialize()
        {
            TestObject testObject = new TestObject();
            byte[] bytes = NeedlefishFormatter.Serialize(testObject);

            Assert.True(true);
        }

        [Fact]
        public void TestDeserialize()
        {
            TestObject testObject = new TestObject();
            byte[] bytes = NeedlefishFormatter.Serialize(testObject);
            TestObject testObject2 = NeedlefishFormatter.Deserialize<TestObject>(bytes);

            Assert.Equal(testObject2.Name, testObject.Name);
            Assert.Equal(testObject2.EmptyString, testObject.EmptyString);
            Assert.Equal(testObject2.NullString, testObject.NullString);
            Assert.Equal(testObject2.MyInt, testObject.MyInt);
            Assert.Equal(testObject2.MyFloat, testObject.MyFloat);
            Assert.Equal(testObject2.MyDecimal, testObject.MyDecimal);
            Assert.Equal(testObject2.IntEnum, testObject.IntEnum);
            Assert.Equal(testObject2.ByteEnum, testObject.ByteEnum);
            Assert.Equal(testObject2.MultiBool, testObject.MultiBool);

            Assert.Equal(testObject2.MyDoubleArray.Length, testObject.MyDoubleArray.Length);
            for (int i = 0; i < testObject2.MyDoubleArray.Length; i++)
                Assert.Equal(testObject2.MyDoubleArray[i], testObject.MyDoubleArray[i]);
            
            Assert.Equal(testObject2.EnumArray.Length, testObject.EnumArray.Length);
            for (int i = 0; i < testObject2.EnumArray.Length; i++)
                Assert.Equal(testObject2.EnumArray[i], testObject.EnumArray[i]);
            
            Assert.Equal(testObject2.BoolList.Count, testObject.BoolList.Count);
            for (int i = 0; i < testObject2.BoolList.Count; i++)
                Assert.Equal(testObject2.BoolList[i], testObject.BoolList[i]);

            Assert.Equal(testObject2.MyTestClass.MyInt, testObject.MyTestClass.MyInt);
            Assert.Equal(testObject2.MyTestClass.TestString, testObject.MyTestClass.TestString);
            Assert.Equal(testObject2.NullClass, testObject.NullClass);
        }

        [Fact]
        public void TestPopulate()
        {
            TestObject testObject = new TestObject {
                Name = "Different name",
                IntEnum = TestObject.TestEnum.B,
                MyFloat = 0f,
                EmptyString = null
            };

            byte[] bytes = NeedlefishFormatter.Serialize(testObject);

            TestObject testObject2 = new TestObject();
            NeedlefishFormatter.Populate(testObject2, bytes);

            Assert.Equal(testObject2.Name, testObject.Name);
            Assert.Equal(testObject2.IntEnum, testObject.IntEnum);
            Assert.Equal(testObject2.MyFloat, testObject.MyFloat);
            Assert.Equal(testObject2.EmptyString, testObject.EmptyString);
        }

        public class TestObject : IDataBody
        {
            public string Name = "My Name";
            public string EmptyString = string.Empty;
            public string NullString;
            public int MyInt;
            public float MyFloat = 7.11f;
            public decimal MyDecimal = Decimal.MaxValue;
            public TestEnum IntEnum;
            public TestEnumByte ByteEnum = TestEnumByte.C;
            public MultiBool MultiBool = new MultiBool {
                [0] = true,
                [3] = true
            };
            public double[] MyDoubleArray = new double[] { 0.3d, 0.1d, 3.5d };
            public TestEnum[] EnumArray = new TestEnum[] { TestEnum.A, TestEnum.C };
            public List<bool> BoolList = new List<bool>();
            public TestClass MyTestClass = new TestClass();
            public TestClass NullClass;

            public enum TestEnum
            {
                A, B, C, D
            }

            public enum TestEnumByte : byte
            {
                A, B, C, D
            }

            public class TestClass
            {
                public int MyInt = 10;
                public string TestString = "This is in a nested class.";
            }
        }
    }
}
