using Farm.Core;
using NUnit.Framework;

namespace Farm.Tests
{
    public class NumberFormatterTests
    {
        [TestCase(0, "0")]
        [TestCase(999, "999")]
        [TestCase(1000, "1.00K")]
        [TestCase(12000, "12.0K")]
        [TestCase(999000, "999K")]
        [TestCase(1_000_000, "1.00M")]
        [TestCase(999_000_000, "999M")]
        [TestCase(1_000_000_000, "1.00B")]
        public void Format_MatchesExpectedNotation(double value, string expected)
        {
            Assert.AreEqual(expected, NumberFormatter.Format(new BigNumber(value)));
        }

        [Test]
        public void Format_OneTrillion_UsesLetterA()
        {
            var value = new BigNumber(1d, 12);
            Assert.AreEqual("1.00a", NumberFormatter.Format(value));
        }

        [Test]
        public void Format_1e15_UsesLetterB()
        {
            var value = new BigNumber(1d, 15);
            Assert.AreEqual("1.00b", NumberFormatter.Format(value));
        }

        [Test]
        public void Format_AfterZ_UsesDoubleLetterAA()
        {
            const int exponentForAA = 12 + 26 * 3;
            var value = new BigNumber(1d, exponentForAA);
            Assert.AreEqual("1.00aa", NumberFormatter.Format(value));
        }

        [Test]
        public void Format_RoundingCarriesIntoNextGroup()
        {
            // 999.996K should round up to 1.00M, not 1000.00K.
            var value = new BigNumber(9.99996d, 5);
            Assert.AreEqual("1.00M", NumberFormatter.Format(value));
        }
    }
}
