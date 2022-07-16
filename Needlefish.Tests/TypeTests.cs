using System;

using Needlefish;
using Needlefish.Types;

using Xunit;

namespace Needlefish.Tests
{
    public class TypeTests
    {
        [Fact]
        public void TestMultiBool()
        {
            MultiBool multiBool = new MultiBool() {
                [0] = false,
                [1] = true,
                [2] = false,
                [3] = true,
                [4] = false,
                [5] = true,
                [6] = false,
                [7] = true
            };

            Assert.True(multiBool[0] == false);
            Assert.True(multiBool[1] == true);
            Assert.True(multiBool[2] == false);
            Assert.True(multiBool[3] == true);
            Assert.True(multiBool[4] == false);
            Assert.True(multiBool[5] == true);
            Assert.True(multiBool[6] == false);
            Assert.True(multiBool[7] == true);
        }
    }
}