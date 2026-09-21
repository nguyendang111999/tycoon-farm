using System;
using System.Collections.Generic;
using Farm.Core;
using Farm.Money;
using UnityEngine;

namespace Farm.Upgrade
{
    /// <summary>
    /// Tracks which one-time <see cref="UpgradeEntry"/> purchases are owned and pushes their effects into
    /// <see cref="GameStats.Global"/>. TEMPORARY: still branches on <see cref="UpgradeType"/> as a translation
    /// shim - a later pass replaces UpgradeEntry's type+amount with direct StatModifier data and removes this branch.
    /// </summary>
    public sealed class UpgradeService : ISaveable
    {
        private readonly UpgradeConfig _config;
        private readonly HashSet<string> _purchased = new HashSet<string>();
        private readonly StatDefinition _cropProfitStat;
        private readonly StatDefinition _customerCapacityStat;
        private readonly StatDefinition _workerCountStat;

        public UpgradeService(UpgradeConfig config, StatDefinition cropProfitStat, StatDefinition customerCapacityStat, StatDefinition workerCountStat)
        {
            _config = config;
            _cropProfitStat = cropProfitStat;
            _customerCapacityStat = customerCapacityStat;
            _workerCountStat = workerCountStat;
        }

        public event Action Changed;

        public string SaveKey => "upgrades";

        public bool IsPurchased(UpgradeEntry entry) => _purchased.Contains(entry.Id);

        public bool TryPurchase(UpgradeEntry entry, ICurrencyService currency)
        {
            if (IsPurchased(entry)) return false;
            if (!currency.TrySpend(entry.CostCurrency, entry.Cost)) return false;

            _purchased.Add(entry.Id);
            ApplyToGameStats(entry);
            Changed?.Invoke();
            GameSaveService.Save();
            return true;
        }

        private void ApplyToGameStats(UpgradeEntry entry)
        {
            switch (entry.Type)
            {
                case UpgradeType.SingleCropProfit:
                    if (_cropProfitStat != null)
                    {
                        GameStats.Global.AddModifier(new StatModifier(_cropProfitStat, ModifierKind.Multiply, entry.EffectAmount, entry.TargetCrop), entry.Id);
                    }
                    break;

                case UpgradeType.AllCropProfit:
                    if (_cropProfitStat != null)
                    {
                        GameStats.Global.AddModifier(new StatModifier(_cropProfitStat, ModifierKind.Multiply, entry.EffectAmount), entry.Id);
                    }
                    break;

                case UpgradeType.AddCustomer:
                    if (_customerCapacityStat != null)
                    {
                        GameStats.Global.AddModifier(new StatModifier(_customerCapacityStat, ModifierKind.Flat, entry.EffectAmount), entry.Id);
                    }
                    break;

                case UpgradeType.AddWorker:
                    if (_workerCountStat != null)
                    {
                        GameStats.Global.AddModifier(new StatModifier(_workerCountStat, ModifierKind.Flat, entry.EffectAmount), entry.Id);
                    }
                    break;
            }
        }

        public string CaptureState()
        {
            var data = new UpgradeSaveData { ids = new List<string>(_purchased) };
            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string json)
        {
            // Entries may have been purchased in a previous session; clear their stat contributions before re-applying.
            foreach (UpgradeEntry entry in _config.Entries)
            {
                GameStats.Global.RemoveModifiersFrom(entry.Id);
            }

            _purchased.Clear();
            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<UpgradeSaveData>(json);
                if (data.ids != null)
                {
                    foreach (string id in data.ids) _purchased.Add(id);
                }
            }

            foreach (UpgradeEntry entry in _config.Entries)
            {
                if (IsPurchased(entry)) ApplyToGameStats(entry);
            }

            Changed?.Invoke();
        }

        [Serializable]
        private sealed class UpgradeSaveData
        {
            public List<string> ids = new List<string>();
        }
    }
}
