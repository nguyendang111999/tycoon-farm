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

        private static UpgradeService CreateService(UpgradeConfig config, StatDefinition cropProfit = null, StatDefinition customerCap = null, StatDefinition workerCount = null)
        {
            return new UpgradeService(config, cropProfit, customerCap, workerCount);
        }

        [SetUp]
        public void SetUp()
        {
            GameStats.Global.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            GameStats.Global.Clear();
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
            var service = CreateService(CreateConfig(entry));
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
            var service = CreateService(CreateConfig(entry));
            var currency = new CurrencyService();
            currency.Add(CurrencyType.Cash, new BigNumber(1000));

            Assert.IsTrue(service.TryPurchase(entry, currency));
            Assert.IsFalse(service.TryPurchase(entry, currency));
        }

        [Test]
        public void TryPurchase_PushesCropProfitModifiersToGameStatsGlobal()
        {
            CropConfig tomato = ScriptableObject.CreateInstance<CropConfig>();
            StatDefinition cropProfit = ScriptableObject.CreateInstance<StatDefinition>();

            try
            {
                var allCropEntry = new UpgradeEntry("all", UpgradeType.AllCropProfit, 2f, costMantissa: 1, costExponent: 1);
                var singleCropEntry = new UpgradeEntry("tomato", UpgradeType.SingleCropProfit, 5f, costMantissa: 3, costExponent: 1, targetCrop: tomato);
                var service = CreateService(CreateConfig(allCropEntry, singleCropEntry), cropProfit: cropProfit);
                var currency = new CurrencyService();
                currency.Add(CurrencyType.Cash, new BigNumber(1000));

                Assert.AreEqual(1f, GameStats.Global.Evaluate(cropProfit, 1f, tomato), 1e-4f);

                service.TryPurchase(allCropEntry, currency);
                service.TryPurchase(singleCropEntry, currency);

                // Multipliers stack: 2 * 5 = 10
                Assert.AreEqual(10f, GameStats.Global.Evaluate(cropProfit, 1f, tomato), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(cropProfit);
                Object.DestroyImmediate(tomato);
            }
        }

        [Test]
        public void TryPurchase_PushesCapacityAndWorkerModifiersToGameStatsGlobal()
        {
            StatDefinition customerCap = ScriptableObject.CreateInstance<StatDefinition>();
            StatDefinition workerCount = ScriptableObject.CreateInstance<StatDefinition>();

            try
            {
                var customerEntryA = new UpgradeEntry("customerA", UpgradeType.AddCustomer, 1f, costMantissa: 1, costExponent: 1);
                var customerEntryB = new UpgradeEntry("customerB", UpgradeType.AddCustomer, 2f, costMantissa: 1, costExponent: 1);
                var workerEntry = new UpgradeEntry("worker", UpgradeType.AddWorker, 1f, costMantissa: 1, costExponent: 1);
                var service = CreateService(CreateConfig(customerEntryA, customerEntryB, workerEntry), customerCap: customerCap, workerCount: workerCount);
                var currency = new CurrencyService();
                currency.Add(CurrencyType.Cash, new BigNumber(1000));

                service.TryPurchase(customerEntryA, currency);
                service.TryPurchase(customerEntryB, currency);
                service.TryPurchase(workerEntry, currency);

                // Starting from base 0, flat modifiers sum
                Assert.AreEqual(3f, GameStats.Global.Evaluate(customerCap, 0f), 1e-4f);
                Assert.AreEqual(1f, GameStats.Global.Evaluate(workerCount, 0f), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(workerCount);
                Object.DestroyImmediate(customerCap);
            }
        }

        [Test]
        public void SaveAndRestore_RoundTripsOwnedUpgradesAndReappliesModifiers()
        {
            StatDefinition cropProfit = ScriptableObject.CreateInstance<StatDefinition>();
            StatDefinition workerCount = ScriptableObject.CreateInstance<StatDefinition>();

            try
            {
                var entryA = new UpgradeEntry("a", UpgradeType.AllCropProfit, 2f, costMantissa: 1, costExponent: 1);
                var entryB = new UpgradeEntry("b", UpgradeType.AddWorker, 1f, costMantissa: 1, costExponent: 1);
                var config = CreateConfig(entryA, entryB);
                var service = CreateService(config, cropProfit: cropProfit, workerCount: workerCount);
                var currency = new CurrencyService();
                currency.Add(CurrencyType.Cash, new BigNumber(1000));

                service.TryPurchase(entryA, currency);
                string saved = service.CaptureState();

                // Fresh service restoring from JSON should re-apply entryA's modifier to GameStats.Global
                var restored = CreateService(config, cropProfit: cropProfit, workerCount: workerCount);
                restored.RestoreState(saved);

                Assert.IsTrue(restored.IsPurchased(entryA));
                Assert.IsFalse(restored.IsPurchased(entryB));
                Assert.AreEqual(2f, GameStats.Global.Evaluate(cropProfit, 1f), 1e-4f);
                Assert.AreEqual(0f, GameStats.Global.Evaluate(workerCount, 0f), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(workerCount);
                Object.DestroyImmediate(cropProfit);
            }
        }
    }
}
