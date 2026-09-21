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
        public void CalculateHarvestPrice_WithStatSheets_CombinesPlotAndGlobalModifiers()
        {
            CropConfig config = CreateConfig();
            StatDefinition profitStat = ScriptableObject.CreateInstance<StatDefinition>();
            var plotStats = new StatSheet();
            var globalStats = new StatSheet();

            try
            {
                // Plot level contributes x1.2 (via Multiply), global upgrade contributes +50% (via PercentAdd)
                plotStats.AddModifier(new StatModifier(profitStat, ModifierKind.Multiply, 1.2f, config), "level");
                globalStats.AddModifier(new StatModifier(profitStat, ModifierKind.PercentAdd, 0.5f), "upgrade");

                BigNumber price = ConstructionMath.CalculateHarvestPrice(config, profitStat, plotStats, globalStats);

                // Base 10 * 1.2 (plot) * 1.5 (global) = 18
                double expected = config.BaseHarvestPrice.ToDouble() * 1.2d * 1.5d;
                Assert.AreEqual(expected, price.ToDouble(), 1e-4);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profitStat);
                UnityEngine.Object.DestroyImmediate(config);
            }
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
