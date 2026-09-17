using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm.Construction
{
    /// <summary>Scene registry of currently built crop types, so other systems can query "what's buildable" without referencing every Construction instance.</summary>
    public sealed class ConstructionManager : MonoBehaviour
    {
        public static ConstructionManager Instance { get; private set; }

        private readonly List<CropConfig> _builtCropTypes = new List<CropConfig>();

        public event Action BuiltCropTypesChanged;

        public IReadOnlyList<CropConfig> BuiltCropTypes => _builtCropTypes;

        private void Awake()
        {
            Instance = this;
        }

        public void Register(Construction construction)
        {
            CropConfig config = construction.Config;
            if (config == null || _builtCropTypes.Contains(config)) return;

            _builtCropTypes.Add(config);
            BuiltCropTypesChanged?.Invoke();
        }
    }
}
