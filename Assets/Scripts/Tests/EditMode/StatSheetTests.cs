using Farm.Core;
using NUnit.Framework;
using UnityEngine;

namespace Farm.Tests
{
    public class StatSheetTests
    {
        private static StatDefinition CreateStat() => ScriptableObject.CreateInstance<StatDefinition>();
        private static CropConfig CreateCrop() => ScriptableObject.CreateInstance<CropConfig>();

        [Test]
        public void Evaluate_NoModifiers_ReturnsBaseValue()
        {
            var sheet = new StatSheet();
            StatDefinition stat = CreateStat();

            try
            {
                Assert.AreEqual(10f, sheet.Evaluate(stat, 10f), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(stat);
            }
        }

        [Test]
        public void Evaluate_FlatModifier_AddsToBase()
        {
            var sheet = new StatSheet();
            StatDefinition stat = CreateStat();

            try
            {
                sheet.AddModifier(new StatModifier(stat, ModifierKind.Flat, 5f), source: "a");

                Assert.AreEqual(15f, sheet.Evaluate(stat, 10f), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(stat);
            }
        }

        [Test]
        public void Evaluate_PercentAddModifiers_SumBeforeApplying()
        {
            var sheet = new StatSheet();
            StatDefinition stat = CreateStat();

            try
            {
                sheet.AddModifier(new StatModifier(stat, ModifierKind.PercentAdd, 0.1f), source: "a");
                sheet.AddModifier(new StatModifier(stat, ModifierKind.PercentAdd, 0.2f), source: "b");

                // +10% and +20% should combine additively into +30%, not compound (1.1 * 1.2).
                Assert.AreEqual(13f, sheet.Evaluate(stat, 10f), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(stat);
            }
        }

        [Test]
        public void Evaluate_MultiplyModifiers_StackMultiplicatively()
        {
            var sheet = new StatSheet();
            StatDefinition stat = CreateStat();

            try
            {
                sheet.AddModifier(new StatModifier(stat, ModifierKind.Multiply, 2f), source: "a");
                sheet.AddModifier(new StatModifier(stat, ModifierKind.Multiply, 3f), source: "b");

                Assert.AreEqual(60f, sheet.Evaluate(stat, 10f), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(stat);
            }
        }

        [Test]
        public void Evaluate_CombinesAllThreeKindsInDefinedOrder()
        {
            var sheet = new StatSheet();
            StatDefinition stat = CreateStat();

            try
            {
                sheet.AddModifier(new StatModifier(stat, ModifierKind.Flat, 5f), source: "a");
                sheet.AddModifier(new StatModifier(stat, ModifierKind.PercentAdd, 0.5f), source: "b");
                sheet.AddModifier(new StatModifier(stat, ModifierKind.Multiply, 2f), source: "c");

                // (10 + 5) * (1 + 0.5) * 2 = 45
                Assert.AreEqual(45f, sheet.Evaluate(stat, 10f), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(stat);
            }
        }

        [Test]
        public void Evaluate_CropFilter_OnlyAppliesToMatchingCrop()
        {
            var sheet = new StatSheet();
            StatDefinition stat = CreateStat();
            CropConfig tomato = CreateCrop();
            CropConfig cabbage = CreateCrop();

            try
            {
                sheet.AddModifier(new StatModifier(stat, ModifierKind.Multiply, 2f, tomato), source: "a");

                Assert.AreEqual(20f, sheet.Evaluate(stat, 10f, tomato), 1e-4f);
                Assert.AreEqual(10f, sheet.Evaluate(stat, 10f, cabbage), 1e-4f);
                Assert.AreEqual(10f, sheet.Evaluate(stat, 10f), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(cabbage);
                Object.DestroyImmediate(tomato);
                Object.DestroyImmediate(stat);
            }
        }

        [Test]
        public void RemoveModifiersFrom_RemovesOnlyThatSourcesModifiers()
        {
            var sheet = new StatSheet();
            StatDefinition stat = CreateStat();

            try
            {
                sheet.AddModifier(new StatModifier(stat, ModifierKind.Flat, 5f), source: "a");
                sheet.AddModifier(new StatModifier(stat, ModifierKind.Flat, 100f), source: "b");

                sheet.RemoveModifiersFrom("b");

                Assert.AreEqual(15f, sheet.Evaluate(stat, 10f), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(stat);
            }
        }

        [Test]
        public void Changed_FiresOnAddAndRemove()
        {
            var sheet = new StatSheet();
            StatDefinition stat = CreateStat();

            try
            {
                int changedCount = 0;
                sheet.Changed += _ => changedCount++;

                sheet.AddModifier(new StatModifier(stat, ModifierKind.Flat, 5f), source: "a");
                Assert.AreEqual(1, changedCount);

                sheet.RemoveModifiersFrom("a");
                Assert.AreEqual(2, changedCount);
            }
            finally
            {
                Object.DestroyImmediate(stat);
            }
        }

        [Test]
        public void Evaluate_DifferentStats_AreIndependent()
        {
            var sheet = new StatSheet();
            StatDefinition statA = CreateStat();
            StatDefinition statB = CreateStat();

            try
            {
                sheet.AddModifier(new StatModifier(statA, ModifierKind.Flat, 5f), source: "a");

                Assert.AreEqual(15f, sheet.Evaluate(statA, 10f), 1e-4f);
                Assert.AreEqual(10f, sheet.Evaluate(statB, 10f), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(statB);
                Object.DestroyImmediate(statA);
            }
        }
    }
}
