using System;
using UnityEngine;

namespace Farm.Core
{
    /// <summary>One additive/percent/multiplicative contribution to a stat, optionally restricted to a single crop.</summary>
    [Serializable]
    public struct StatModifier
    {
        [SerializeField] private StatDefinition _stat;
        [SerializeField] private ModifierKind _kind;
        [SerializeField] private float _value;
        [Tooltip("Only applies when evaluating this exact crop. Leave empty to apply to every crop, or for non-crop stats.")]
        [SerializeField] private CropConfig _cropFilter;

        public StatModifier(StatDefinition stat, ModifierKind kind, float value, CropConfig cropFilter = null)
        {
            _stat = stat;
            _kind = kind;
            _value = value;
            _cropFilter = cropFilter;
        }

        public StatDefinition Stat => _stat;
        public ModifierKind Kind => _kind;
        public float Value => _value;
        public CropConfig CropFilter => _cropFilter;

        public bool AppliesTo(CropConfig crop) => _cropFilter == null || _cropFilter == crop;
    }
}
