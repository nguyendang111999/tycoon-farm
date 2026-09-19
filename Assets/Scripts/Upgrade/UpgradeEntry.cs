using System;
using Farm.Core;
using Farm.Money;
using UnityEngine;

namespace Farm.Upgrade
{
    /// <summary>One standalone, one-time purchasable management upgrade: fixed cost, fixed effect. Independent of any other entry.</summary>
    [Serializable]
    public sealed class UpgradeEntry
    {
        [Header("Identity")]
        [SerializeField] private string _id = "upgrade";
        [SerializeField] private UpgradeType _type;
        [SerializeField] private string _displayName = "Upgrade";
        [SerializeField] private Sprite _icon;
        [Tooltip("Only used when Type == SingleCropProfit.")]
        [SerializeField] private CropConfig _targetCrop;

        [Header("Cost")]
        [SerializeField] private CurrencyType _costCurrency = CurrencyType.Cash;
        [Tooltip("Cost = costMantissa * 10^costExponent (e.g. mantissa 1.5, exponent 3 = 1500). Supports arbitrarily large values, same representation as BigNumber.")]
        [SerializeField] private double _costMantissa = 1d;
        [SerializeField] private int _costExponent = 2;

        [Header("Effect")]
        [Tooltip("Profit types: the multiplier this upgrade grants once purchased (e.g. 2 = x2 revenue). Capacity types: the flat count granted once purchased (e.g. 3 = +3 workers).")]
        [SerializeField] private float _effectAmount = 1f;

        public UpgradeEntry()
        {
        }

        /// <summary>Programmatic constructor for tests/tooling; Inspector-authored entries use the parameterless constructor + field defaults instead.</summary>
        public UpgradeEntry(
            string id,
            UpgradeType type,
            float effectAmount,
            double costMantissa = 1d,
            int costExponent = 2,
            CropConfig targetCrop = null,
            CurrencyType costCurrency = CurrencyType.Cash,
            string displayName = null)
        {
            _id = id;
            _type = type;
            _effectAmount = effectAmount;
            _costMantissa = costMantissa;
            _costExponent = costExponent;
            _targetCrop = targetCrop;
            _costCurrency = costCurrency;
            _displayName = displayName ?? id;
        }

        public string Id => _id;
        public UpgradeType Type => _type;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public CropConfig TargetCrop => _targetCrop;
        public CurrencyType CostCurrency => _costCurrency;
        public float EffectAmount => _effectAmount;
        public BigNumber Cost => new BigNumber(_costMantissa, _costExponent);
    }
}
