using Farm.Core;
using Farm.Money;
using NUnit.Framework;

namespace Farm.Tests
{
    public class CurrencyServiceTests
    {
        [Test]
        public void Add_IncreasesBalance()
        {
            var service = new CurrencyService();
            service.Add(CurrencyType.Cash, new BigNumber(100d));
            Assert.AreEqual(0, service.GetBalance(CurrencyType.Cash).CompareTo(new BigNumber(100d)));
        }

        [Test]
        public void TrySpend_FailsWhenInsufficientFunds()
        {
            var service = new CurrencyService();
            service.Add(CurrencyType.Cash, new BigNumber(50d));
            bool spent = service.TrySpend(CurrencyType.Cash, new BigNumber(100d));
            Assert.IsFalse(spent);
            Assert.AreEqual(0, service.GetBalance(CurrencyType.Cash).CompareTo(new BigNumber(50d)));
        }

        [Test]
        public void TrySpend_SucceedsAndDeductsExactAmount()
        {
            var service = new CurrencyService();
            service.Add(CurrencyType.Cash, new BigNumber(100d));
            bool spent = service.TrySpend(CurrencyType.Cash, new BigNumber(40d));
            Assert.IsTrue(spent);
            Assert.AreEqual(0, service.GetBalance(CurrencyType.Cash).CompareTo(new BigNumber(60d)));
        }

        [Test]
        public void SaveAndRestore_RoundTripsBalances()
        {
            var service = new CurrencyService();
            service.Add(CurrencyType.Cash, new BigNumber(12345d));
            service.Add(CurrencyType.Gem, new BigNumber(7d));

            string json = service.CaptureState();

            var restored = new CurrencyService();
            restored.RestoreState(json);

            Assert.AreEqual(0, restored.GetBalance(CurrencyType.Cash).CompareTo(new BigNumber(12345d)));
            Assert.AreEqual(0, restored.GetBalance(CurrencyType.Gem).CompareTo(new BigNumber(7d)));
        }

        [Test]
        public void BalanceChanged_FiresOnAdd()
        {
            var service = new CurrencyService();
            CurrencyType? seenType = null;
            service.BalanceChanged += (type, newBalance, delta) => seenType = type;

            service.Add(CurrencyType.Cash, new BigNumber(10d));

            Assert.AreEqual(CurrencyType.Cash, seenType);
        }
    }
}
