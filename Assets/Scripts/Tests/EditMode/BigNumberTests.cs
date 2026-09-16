using Farm.Core;
using NUnit.Framework;

namespace Farm.Tests
{
    public class BigNumberTests
    {
        [Test]
        public void Constructor_NormalizesMantissaIntoUnitRange()
        {
            var number = new BigNumber(2500d, 0);
            Assert.AreEqual(2.5d, number.Mantissa, 1e-9);
            Assert.AreEqual(3, number.Exponent);
        }

        [Test]
        public void Addition_CombinesDifferentExponents()
        {
            var a = new BigNumber(1_000_000d);
            var b = new BigNumber(500d);
            BigNumber result = a + b;
            Assert.AreEqual(1_000_500d, result.ToDouble(), 1d);
        }

        [Test]
        public void Addition_IgnoresNegligibleSmallerTerm()
        {
            var huge = new BigNumber(1d, 30);
            var tiny = new BigNumber(1d, 5);
            BigNumber result = huge + tiny;
            Assert.AreEqual(huge, result);
        }

        [Test]
        public void Subtraction_ReturnsZeroWhenEqual()
        {
            var a = new BigNumber(500d);
            BigNumber result = a - a;
            Assert.IsTrue(result.IsZero);
        }

        [Test]
        public void CompareTo_OrdersByExponentThenMantissa()
        {
            var small = new BigNumber(999d);
            var large = new BigNumber(1000d);
            Assert.Less(small.CompareTo(large), 0);
        }

        [Test]
        public void RawStringRoundTrip_PreservesValue()
        {
            var original = new BigNumber(1.2345d, 42);
            string raw = original.ToRawString();
            BigNumber parsed = BigNumber.Parse(raw);
            Assert.AreEqual(original, parsed);
        }

        [Test]
        public void Multiply_ByScalar_AppliesPercentageIncrease()
        {
            var price = new BigNumber(100d);
            BigNumber upgraded = price * 1.1d; // +10%
            Assert.AreEqual(110d, upgraded.ToDouble(), 1e-6);
        }
    }
}
