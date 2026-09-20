using System;
using System.Collections.Generic;
using Farm.Core;
using UnityEngine;

namespace Farm.Construction
{
    /// <summary>Scene registry of farm plots and built crop types, responsible for persisting construction state across sessions.</summary>
    [DefaultExecutionOrder(-95)]
    public sealed class ConstructionManager : MonoBehaviour, ISaveable
    {
        public static ConstructionManager Instance { get; private set; }

        private readonly List<CropPlot> _allPlots = new List<CropPlot>();
        private readonly List<CropPlot> _built = new List<CropPlot>();
        private readonly List<CropConfig> _builtCropTypes = new List<CropConfig>();

        public event Action BuiltCropTypesChanged;

        public IReadOnlyList<CropConfig> BuiltCropTypes => _builtCropTypes;
        public IReadOnlyList<CropPlot> AllPlots => _allPlots;

        public string SaveKey => "constructions";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            GameSaveService.Register(this);
        }

        private void Start()
        {
            GameSaveService.LoadOnce();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                GameSaveService.Unregister(this);
                Instance = null;
            }
        }

        private void OnApplicationQuit() => GameSaveService.Save();

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) GameSaveService.Save();
        }

        public void RegisterPlot(CropPlot plot)
        {
            if (plot == null || _allPlots.Contains(plot)) return;

            foreach (CropPlot existing in _allPlots)
            {
                if (existing != null && existing.PlotId == plot.PlotId)
                {
                    Debug.LogWarning($"[ConstructionManager] Duplicate PlotId '{plot.PlotId}' on '{plot.name}' and '{existing.name}'. Each plot should have a unique ID for save/load.");
                    break;
                }
            }

            _allPlots.Add(plot);
        }

        public void UnregisterPlot(CropPlot plot)
        {
            if (plot == null) return;
            _allPlots.Remove(plot);
            _built.Remove(plot);
        }

        public void Register(CropPlot construction)
        {
            if (!_built.Contains(construction)) _built.Add(construction);

            CropConfig config = construction.Config;
            if (config == null || _builtCropTypes.Contains(config)) return;

            _builtCropTypes.Add(config);
            BuiltCropTypesChanged?.Invoke();
        }

        public CropPlot FindAvailableConstruction(CropConfig crop, int minAvailableStock)
        {
            foreach (CropPlot construction in _built)
            {
                if (construction.IsBuilt && !construction.IsClaimed && construction.Config == crop && construction.AvailableStock >= minAvailableStock)
                {
                    return construction;
                }
            }

            return null;
        }

        public string CaptureState()
        {
            var data = new ConstructionSaveData();
            foreach (CropPlot plot in _allPlots)
            {
                if (plot == null) continue;
                data.entries.Add(new ConstructionSaveEntry
                {
                    plotId = plot.PlotId,
                    state = plot.CaptureState()
                });
            }

            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string json)
        {
            _built.Clear();
            _builtCropTypes.Clear();

            if (string.IsNullOrEmpty(json))
            {
                foreach (CropPlot plot in _allPlots)
                {
                    if (plot == null) continue;
                    plot.RestoreState(new CropPlot.ConstructionState
                    {
                        IsBuilt = false,
                        Level = 1,
                        Stock = 0,
                        GrowProgress = 0f
                    });
                }

                BuiltCropTypesChanged?.Invoke();
                return;
            }

            var data = JsonUtility.FromJson<ConstructionSaveData>(json);
            if (data == null || data.entries == null) return;

            var entryById = new Dictionary<string, CropPlot.ConstructionState>();
            foreach (ConstructionSaveEntry entry in data.entries)
            {
                if (!string.IsNullOrEmpty(entry.plotId))
                {
                    entryById[entry.plotId] = entry.state;
                }
            }

            foreach (CropPlot plot in _allPlots)
            {
                if (plot == null) continue;

                if (entryById.TryGetValue(plot.PlotId, out CropPlot.ConstructionState state))
                {
                    plot.RestoreState(state);
                }
                else
                {
                    plot.RestoreState(new CropPlot.ConstructionState { IsBuilt = false, Level = 1 });
                }
            }
        }
    }

    [Serializable]
    public sealed class ConstructionSaveData
    {
        public List<ConstructionSaveEntry> entries = new List<ConstructionSaveEntry>();
    }

    [Serializable]
    public sealed class ConstructionSaveEntry
    {
        public string plotId;
        public CropPlot.ConstructionState state;
    }
}
