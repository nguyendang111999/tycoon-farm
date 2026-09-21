using System;
using System.Collections.Generic;
using Farm.Core;
using Farm.Money;
using UnityEngine;

namespace Farm.Upgrade
{
    /// <summary>One standalone, one-time purchasable management upgrade: fixed cost, a list of stat modifiers it grants. Independent of any other entry.</summary>
    [Serializable]
    public sealed class UpgradeEntry
    {
        [Header("Identity")]
        [SerializeField] private string _id = "upgrade";
        [SerializeField] private string _displayName = "Upgrade";
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private Sprite _icon;

        [Header("Cost")]
        [SerializeField] private CurrencyType _costCurrency = CurrencyType.Cash;
        [Tooltip("Cost = costMantissa * 10^costExponent (e.g. mantissa 1.5, exponent 3 = 1500). Supports arbitrarily large values, same representation as BigNumber.")]
        [SerializeField] private double _costMantissa = 1d;
        [SerializeField] private int _costExponent = 2;

        [Header("Effects")]
        [Tooltip("One or more stat modifiers granted once this upgrade is purchased.")]
        [SerializeField] private StatModifier[] _effects = new StatModifier[0];

        public UpgradeEntry()
        {
        }

        /// <summary>Programmatic constructor for tests/tooling; Inspector-authored entries use the parameterless constructor + field defaults instead.</summary>
        public UpgradeEntry(
            string id,
            StatModifier[] effects,
            double costMantissa = 1d,
            int costExponent = 2,
            CurrencyType costCurrency = CurrencyType.Cash,
            string displayName = null,
            string description = null)
        {
            _id = id;
            _effects = effects ?? new StatModifier[0];
            _costMantissa = costMantissa;
            _costExponent = costExponent;
            _costCurrency = costCurrency;
            _displayName = displayName ?? id;
            _description = description ?? "";
        }

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public CurrencyType CostCurrency => _costCurrency;
        public IReadOnlyList<StatModifier> Effects => _effects;
        public BigNumber Cost => new BigNumber(_costMantissa, _costExponent);
    }
}

