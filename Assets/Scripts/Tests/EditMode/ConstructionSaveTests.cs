using System.Collections.Generic;
using Farm.Construction;
using Farm.Core;
using NUnit.Framework;
using UnityEngine;

namespace Farm.Tests
{
    public class ConstructionSaveTests
    {
        [Test]
        public void ConstructionSaveData_RoundTripsJson()
        {
            var original = new ConstructionSaveData();
            original.entries.Add(new ConstructionSaveEntry
            {
                plotId = "plot_tomato_1",
                state = new CropPlot.ConstructionState
                {
                    IsBuilt = true,
                    Level = 3,
                    Stock = 2,
                    GrowProgress = 4.5f
                }
            });
            original.entries.Add(new ConstructionSaveEntry
            {
                plotId = "plot_cabbage_1",
                state = new CropPlot.ConstructionState
                {
                    IsBuilt = false,
                    Level = 1,
                    Stock = 0,
                    GrowProgress = 0f
                }
            });

            string json = JsonUtility.ToJson(original);
            Assert.IsFalse(string.IsNullOrEmpty(json));

            var restored = JsonUtility.FromJson<ConstructionSaveData>(json);
            Assert.IsNotNull(restored);
            Assert.AreEqual(2, restored.entries.Count);

            Assert.AreEqual("plot_tomato_1", restored.entries[0].plotId);
            Assert.IsTrue(restored.entries[0].state.IsBuilt);
            Assert.AreEqual(3, restored.entries[0].state.Level);
            Assert.AreEqual(2, restored.entries[0].state.Stock);
            Assert.AreEqual(4.5f, restored.entries[0].state.GrowProgress, 1e-4f);

            Assert.AreEqual("plot_cabbage_1", restored.entries[1].plotId);
            Assert.IsFalse(restored.entries[1].state.IsBuilt);
            Assert.AreEqual(1, restored.entries[1].state.Level);
            Assert.AreEqual(0, restored.entries[1].state.Stock);
            Assert.AreEqual(0f, restored.entries[1].state.GrowProgress, 1e-4f);
        }

        [Test]
        public void ConstructionManager_CaptureAndRestoreState_RestoresMatchingPlots()
        {
            var managerGo = new GameObject("ConstructionManager");
            var plotGoA = new GameObject("PlotA");
            var plotGoB = new GameObject("PlotB");
            var dummyConfig = ScriptableObject.CreateInstance<CropConfig>();

            try
            {
                var manager = managerGo.AddComponent<ConstructionManager>();

                // Add collider first to satisfy [RequireComponent(typeof(Collider))]
                plotGoA.AddComponent<BoxCollider>();
                var plotA = plotGoA.AddComponent<CropPlot>();

                plotGoB.AddComponent<BoxCollider>();
                var plotB = plotGoB.AddComponent<CropPlot>();

                // Supply dummy config to prevent NRE in SetStock/MaxStock
                typeof(CropPlot)
                    .GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(plotA, dummyConfig);
                typeof(CropPlot)
                    .GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(plotB, dummyConfig);

                manager.RegisterPlot(plotA);
                manager.RegisterPlot(plotB);

                plotA.RestoreState(new CropPlot.ConstructionState
                {
                    IsBuilt = true,
                    Level = 4,
                    Stock = 1,
                    GrowProgress = 2.0f
                });

                plotB.RestoreState(new CropPlot.ConstructionState
                {
                    IsBuilt = false,
                    Level = 1,
                    Stock = 0,
                    GrowProgress = 0f
                });

                string json = manager.CaptureState();

                // Reset states to verify restoration
                plotA.RestoreState(new CropPlot.ConstructionState { IsBuilt = false, Level = 1 });
                plotB.RestoreState(new CropPlot.ConstructionState { IsBuilt = true, Level = 9 });

                manager.RestoreState(json);

                CropPlot.ConstructionState restoredA = plotA.CaptureState();
                Assert.IsTrue(restoredA.IsBuilt);
                Assert.AreEqual(4, restoredA.Level);

                CropPlot.ConstructionState restoredB = plotB.CaptureState();
                Assert.IsFalse(restoredB.IsBuilt);
                Assert.AreEqual(1, restoredB.Level);
            }
            finally
            {
                Object.DestroyImmediate(dummyConfig);
                Object.DestroyImmediate(plotGoB);
                Object.DestroyImmediate(plotGoA);
                Object.DestroyImmediate(managerGo);
            }
        }

        [Test]
        public void ConstructionManager_RestoreState_NullOrEmpty_ResetsPlotsToDefault()
        {
            var managerGo = new GameObject("ConstructionManager");
            var plotGo = new GameObject("Plot");
            var dummyConfig = ScriptableObject.CreateInstance<CropConfig>();

            try
            {
                var manager = managerGo.AddComponent<ConstructionManager>();
                plotGo.AddComponent<BoxCollider>();
                var plot = plotGo.AddComponent<CropPlot>();

                typeof(CropPlot)
                    .GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(plot, dummyConfig);

                manager.RegisterPlot(plot);

                plot.RestoreState(new CropPlot.ConstructionState
                {
                    IsBuilt = true,
                    Level = 5,
                    Stock = 3,
                    GrowProgress = 1f
                });

                // Passing null/empty should reset plots back to unbuilt Lv.1
                manager.RestoreState(null);

                CropPlot.ConstructionState state = plot.CaptureState();
                Assert.IsFalse(state.IsBuilt);
                Assert.AreEqual(1, state.Level);
                Assert.AreEqual(0, state.Stock);
            }
            finally
            {
                Object.DestroyImmediate(dummyConfig);
                Object.DestroyImmediate(plotGo);
                Object.DestroyImmediate(managerGo);
            }
        }
    }
}
