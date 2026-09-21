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
            var entry = new UpgradeEntry("all", effects: null, costMantissa: 1.5, costExponent: 3);

            Assert.AreEqual(1500d, entry.Cost.ToDouble(), 1e-6);
        }

        [Test]
        public void TryPurchase_SpendsCurrencyAndMarksOwned()
        {
            var entry = new UpgradeEntry("all", effects: null, costMantissa: 1, costExponent: 2);
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
            var entry = new UpgradeEntry("all", effects: null, costMantissa: 1, costExponent: 1);
            var service = new UpgradeService(CreateConfig(entry));
            var currency = new CurrencyService();
            currency.Add(CurrencyType.Cash, new BigNumber(1000));

            Assert.IsTrue(service.TryPurchase(entry, currency));
            Assert.IsFalse(service.TryPurchase(entry, currency));
        }

        [Test]
        public void TryPurchase_RegistersEachEffectAsAModifierOnGameStatsGlobal()
        {
            CropConfig tomato = ScriptableObject.CreateInstance<CropConfig>();
            StatDefinition cropProfit = ScriptableObject.CreateInstance<StatDefinition>();

            try
            {
                var allCropEntry = new UpgradeEntry("all", new[]
                {
                    new StatModifier(cropProfit, ModifierKind.Multiply, 2f)
                }, costMantissa: 1, costExponent: 1);

                var singleCropEntry = new UpgradeEntry("tomato", new[]
                {
                    new StatModifier(cropProfit, ModifierKind.Multiply, 5f, tomato)
                }, costMantissa: 3, costExponent: 1);

                var service = new UpgradeService(CreateConfig(allCropEntry, singleCropEntry));
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
        public void TryPurchase_SupportsMultipleEffectsOnOneEntry()
        {
            StatDefinition customerCap = ScriptableObject.CreateInstance<StatDefinition>();
            StatDefinition workerCount = ScriptableObject.CreateInstance<StatDefinition>();

            try
            {
                // One entry granting both +1 customer capacity and +1 worker at once.
                var comboEntry = new UpgradeEntry("combo", new[]
                {
                    new StatModifier(customerCap, ModifierKind.Flat, 1f),
                    new StatModifier(workerCount, ModifierKind.Flat, 1f)
                }, costMantissa: 1, costExponent: 1);

                var service = new UpgradeService(CreateConfig(comboEntry));
                var currency = new CurrencyService();
                currency.Add(CurrencyType.Cash, new BigNumber(1000));

                service.TryPurchase(comboEntry, currency);

                Assert.AreEqual(1f, GameStats.Global.Evaluate(customerCap, 0f), 1e-4f);
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
                var entryA = new UpgradeEntry("a", new[] { new StatModifier(cropProfit, ModifierKind.Multiply, 2f) }, costMantissa: 1, costExponent: 1);
                var entryB = new UpgradeEntry("b", new[] { new StatModifier(workerCount, ModifierKind.Flat, 1f) }, costMantissa: 1, costExponent: 1);
                var config = CreateConfig(entryA, entryB);
                var service = new UpgradeService(config);
                var currency = new CurrencyService();
                currency.Add(CurrencyType.Cash, new BigNumber(1000));

                service.TryPurchase(entryA, currency);
                string saved = service.CaptureState();

                // Fresh service restoring from JSON should re-apply entryA's modifier to GameStats.Global
                var restored = new UpgradeService(config);
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

