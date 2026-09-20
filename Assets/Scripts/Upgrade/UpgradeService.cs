using System;
using System.Collections.Generic;
using Farm.Core;
using Farm.Money;
using UnityEngine;

namespace Farm.Upgrade
{
    /// <summary>Tracks which one-time <see cref="UpgradeEntry"/> purchases are owned and aggregates them into <see cref="IManagementBonuses"/> queries.</summary>
    public sealed class UpgradeService : IManagementBonuses, ISaveable
    {
        private readonly UpgradeConfig _config;
        private readonly HashSet<string> _purchased = new HashSet<string>();

        public UpgradeService(UpgradeConfig config)
        {
            _config = config;
        }

        public event Action Changed;

        public string SaveKey => "upgrades";

        public bool IsPurchased(UpgradeEntry entry) => _purchased.Contains(entry.Id);

        public bool TryPurchase(UpgradeEntry entry, ICurrencyService currency)
        {
            if (IsPurchased(entry)) return false;
            if (!currency.TrySpend(entry.CostCurrency, entry.Cost)) return false;

            _purchased.Add(entry.Id);
            Changed?.Invoke();
            GameSaveService.Save();
            return true;
        }

        public float GetCropProfitMultiplier(CropConfig crop)
        {
            float multiplier = 1f;
            foreach (UpgradeEntry entry in _config.Entries)
            {
                if (!IsPurchased(entry)) continue;

                bool applies = entry.Type == UpgradeType.AllCropProfit
                    || (entry.Type == UpgradeType.SingleCropProfit && entry.TargetCrop == crop);
                if (applies) multiplier *= entry.EffectAmount;
            }

            return multiplier;
        }

        public int BonusCustomerCapacity => SumBonus(UpgradeType.AddCustomer);
        public int BonusWorkers => SumBonus(UpgradeType.AddWorker);

        private int SumBonus(UpgradeType type)
        {
            int total = 0;
            foreach (UpgradeEntry entry in _config.Entries)
            {
                if (entry.Type != type || !IsPurchased(entry)) continue;
                total += Mathf.RoundToInt(entry.EffectAmount);
            }

            return total;
        }

        public string CaptureState()
        {
            var data = new UpgradeSaveData { ids = new List<string>(_purchased) };
            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string json)
        {
            _purchased.Clear();
            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<UpgradeSaveData>(json);
                if (data.ids != null)
                {
                    foreach (string id in data.ids) _purchased.Add(id);
                }
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
