using System.Reflection;
using Farm.Core;
using Farm.Money;
using Farm.Upgrade;
using NUnit.Framework;
using UnityEngine;

namespace Farm.Tests
{
    public class UpgradeServiceTests
    {
        private static UpgradeConfig CreateConfig(params UpgradeEntry[] entries)
        {
            var config = ScriptableObject.CreateInstance<UpgradeConfig>();
            typeof(UpgradeConfig)
                .GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(config, entries);
            return config;
        }

        [Test]
        public void Entry_Cost_MatchesMantissaAndExponent()
        {
            var entry = new UpgradeEntry("all", UpgradeType.AllCropProfit, 2f, costMantissa: 1.5, costExponent: 3);

            Assert.AreEqual(1500d, entry.Cost.ToDouble(), 1e-6);
        }

        [Test]
        public void TryPurchase_SpendsCurrencyAndMarksOwned()
        {
            var entry = new UpgradeEntry("all", UpgradeType.AllCropProfit, 2f, costMantissa: 1, costExponent: 2);
            var service = new UpgradeService(CreateConfig(entry));
            var currency = new CurrencyService();
            currency.Add(CurrencyType.Cash, new BigNumber(50));

            Assert.IsFalse(service.TryPurchase(entry, currency));
            Assert.IsFalse(service.IsPurchased(entry));

            currency.Add(CurrencyType.Cash, new BigNumber(50));

            Assert.IsTrue(service.TryPurchase(entry, currency));
            Assert.IsTrue(service.IsPurchased(entry));
            Assert.AreEqual(0d, currency.GetBalance(CurrencyType.Cash).ToDouble(), 1e-6);
        }

        [Test]
        public void TryPurchase_FailsWhenAlreadyOwned()
        {
            var entry = new UpgradeEntry("all", UpgradeType.AllCropProfit, 2f, costMantissa: 1, costExponent: 1);
            var service = new UpgradeService(CreateConfig(entry));
            var currency = new CurrencyService();
            currency.Add(CurrencyType.Cash, new BigNumber(1000));

            Assert.IsTrue(service.TryPurchase(entry, currency));
            Assert.IsFalse(service.TryPurchase(entry, currency));
        }

        [Test]
        public void GetCropProfitMultiplier_StacksAllAndSingleCropMultiplicatively()
        {
            CropConfig tomato = ScriptableObject.CreateInstance<CropConfig>();
            try
            {
                var allCropEntry = new UpgradeEntry("all", UpgradeType.AllCropProfit, 2f, costMantissa: 1, costExponent: 1);
                var singleCropEntry = new UpgradeEntry("tomato", UpgradeType.SingleCropProfit, 5f, costMantissa: 3, costExponent: 1, targetCrop: tomato);
                var service = new UpgradeService(CreateConfig(allCropEntry, singleCropEntry));
                var currency = new CurrencyService();
                currency.Add(CurrencyType.Cash, new BigNumber(1000));

                Assert.AreEqual(1f, service.GetCropProfitMultiplier(tomato), 1e-4f);

                service.TryPurchase(allCropEntry, currency);
                service.TryPurchase(singleCropEntry, currency);

                Assert.AreEqual(2f * 5f, service.GetCropProfitMultiplier(tomato), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(tomato);
            }
        }

        [Test]
        public void BonusCustomerCapacityAndWorkers_SumPurchasedEntries()
        {
            var customerEntryA = new UpgradeEntry("customerA", UpgradeType.AddCustomer, 1f, costMantissa: 1, costExponent: 1);
            var customerEntryB = new UpgradeEntry("customerB", UpgradeType.AddCustomer, 2f, costMantissa: 1, costExponent: 1);
            var workerEntry = new UpgradeEntry("worker", UpgradeType.AddWorker, 1f, costMantissa: 1, costExponent: 1);
            var service = new UpgradeService(CreateConfig(customerEntryA, customerEntryB, workerEntry));
            var currency = new CurrencyService();
            currency.Add(CurrencyType.Cash, new BigNumber(1000));

            service.TryPurchase(customerEntryA, currency);
            service.TryPurchase(customerEntryB, currency);
            service.TryPurchase(workerEntry, currency);

            Assert.AreEqual(3, service.BonusCustomerCapacity);
            Assert.AreEqual(1, service.BonusWorkers);
        }

        [Test]
        public void SaveAndRestore_RoundTripsOwnedUpgrades()
        {
            var entryA = new UpgradeEntry("a", UpgradeType.AllCropProfit, 2f, costMantissa: 1, costExponent: 1);
            var entryB = new UpgradeEntry("b", UpgradeType.AddWorker, 1f, costMantissa: 1, costExponent: 1);
            var config = CreateConfig(entryA, entryB);
            var service = new UpgradeService(config);
            var currency = new CurrencyService();
            currency.Add(CurrencyType.Cash, new BigNumber(1000));

            service.TryPurchase(entryA, currency);
            string saved = service.CaptureState();

            var restored = new UpgradeService(config);
            restored.RestoreState(saved);

            Assert.IsTrue(restored.IsPurchased(entryA));
            Assert.IsFalse(restored.IsPurchased(entryB));
        }
    }
}
