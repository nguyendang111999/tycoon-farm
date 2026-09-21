using System;
using System.Collections.Generic;
using Farm.Core;
using Farm.Money;
using UnityEngine;

namespace Farm.Upgrade
{
    /// <summary>Tracks which one-time <see cref="UpgradeEntry"/> purchases are owned and registers/removes their StatModifiers in <see cref="GameStats.Global"/>.</summary>
    public sealed class UpgradeService : ISaveable
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
            ApplyEffects(entry);
            Changed?.Invoke();
            GameSaveService.Save();
            return true;
        }

        private static void ApplyEffects(UpgradeEntry entry)
        {
            foreach (StatModifier modifier in entry.Effects)
            {
                GameStats.Global.AddModifier(modifier, entry.Id);
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
                if (IsPurchased(entry)) ApplyEffects(entry);
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
