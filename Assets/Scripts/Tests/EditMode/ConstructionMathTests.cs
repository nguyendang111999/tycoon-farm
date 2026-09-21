using System;
using Farm.Construction;
using Farm.Core;
using NUnit.Framework;
using UnityEngine;

namespace Farm.Tests
{
    public class ConstructionMathTests
    {
        private static CropConfig CreateConfig() => ScriptableObject.CreateInstance<CropConfig>();

        [Test]
        public void ProfitMultiplier_MatchesSpecPercentages()
        {
            CropConfig config = CreateConfig();
            Assert.AreEqual(1.0f, config.GetProfitMultiplier(1), 1e-4f);
            Assert.AreEqual(1.1f, config.GetProfitMultiplier(2), 1e-4f);
            Assert.AreEqual(1.2f, config.GetProfitMultiplier(3), 1e-4f);
        }

        [Test]
        public void CalculateHarvestPrice_AppliesCurrentLevelMultiplier()
        {
            CropConfig config = CreateConfig();
            BigNumber priceAtLevel1 = ConstructionMath.CalculateHarvestPrice(config, 1);
            BigNumber priceAtLevel3 = ConstructionMath.CalculateHarvestPrice(config, 3);

            Assert.AreEqual(config.BaseHarvestPrice.ToDouble(), priceAtLevel1.ToDouble(), 1e-6);
            Assert.AreEqual(config.BaseHarvestPrice.ToDouble() * 1.2d, priceAtLevel3.ToDouble(), 1e-4);
        }

        [Test]
        public void CalculateHarvestPrice_WithManagementMultiplier_ScalesResult()
        {
            CropConfig config = CreateConfig();
            BigNumber basePrice = ConstructionMath.CalculateHarvestPrice(config, 1);
            BigNumber boostedPrice = ConstructionMath.CalculateHarvestPrice(config, 1, 2f);

            Assert.AreEqual(basePrice.ToDouble() * 2d, boostedPrice.ToDouble(), 1e-4);
        }

        [Test]
        public void IsMaxLevel_TrueOnlyAtConfiguredCap()
        {
            CropConfig config = CreateConfig();
            Assert.IsFalse(ConstructionMath.IsMaxLevel(config, config.MaxLevel - 1));
            Assert.IsTrue(ConstructionMath.IsMaxLevel(config, config.MaxLevel));
        }

        [Test]
        public void CalculateUpgradeCost_IncreasesWithLevel()
        {
            CropConfig config = CreateConfig();
            BigNumber costEarly = ConstructionMath.CalculateUpgradeCost(config, 1);
            BigNumber costLate = ConstructionMath.CalculateUpgradeCost(config, config.MaxLevel - 1);

            Assert.Greater(costLate.ToDouble(), costEarly.ToDouble());
        }

        [Test]
        public void CalculateUpgradeCost_MatchesExponentialFormula()
        {
            CropConfig config = CreateConfig();
            BigNumber costAtLevel1 = ConstructionMath.CalculateUpgradeCost(config, 1);
            BigNumber costAtLevel3 = ConstructionMath.CalculateUpgradeCost(config, 3);

            Assert.AreEqual(100d, costAtLevel1.ToDouble(), 1e-6);
            Assert.AreEqual(100d * Math.Pow(1.3d, 2), costAtLevel3.ToDouble(), 1e-2);
        }

        [Test]
        public void CalculateUpgradeCost_StaysFiniteFarBeyondDoubleRange()
        {
            CropConfig config = CreateConfig();
            BigNumber costAtLevel5000 = ConstructionMath.CalculateUpgradeCost(config, 5000);

            Assert.IsFalse(double.IsNaN(costAtLevel5000.Mantissa));
            Assert.IsFalse(double.IsInfinity(costAtLevel5000.Mantissa));
            Assert.Greater(costAtLevel5000.Exponent, 0);
        }
    }
}
