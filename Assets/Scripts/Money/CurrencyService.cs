using System;
using System.Collections.Generic;
using Farm.Core;
using UnityEngine;

namespace Farm.Money
{
    /// <summary>Holds and mutates the player's currency balances; the only place balances are allowed to change.</summary>
    public sealed class CurrencyService : ICurrencyService, ISaveable
    {
        private readonly Dictionary<CurrencyType, BigNumber> _balances = new Dictionary<CurrencyType, BigNumber>();

        public event Action<CurrencyType, BigNumber, BigNumber> BalanceChanged;

        public string SaveKey => "currency";

        public BigNumber GetBalance(CurrencyType type)
        {
            return _balances.TryGetValue(type, out BigNumber balance) ? balance : BigNumber.Zero;
        }

        public void Add(CurrencyType type, BigNumber amount)
        {
            if (amount.CompareTo(BigNumber.Zero) <= 0) return;

            BigNumber newBalance = GetBalance(type) + amount;
            _balances[type] = newBalance;
            BalanceChanged?.Invoke(type, newBalance, amount);
        }

        public bool TrySpend(CurrencyType type, BigNumber amount)
        {
            if (amount.CompareTo(BigNumber.Zero) <= 0) return false;

            BigNumber current = GetBalance(type);
            if (current.CompareTo(amount) < 0) return false;

            BigNumber newBalance = current - amount;
            _balances[type] = newBalance;
            BalanceChanged?.Invoke(type, newBalance, -amount);
            return true;
        }

        public string CaptureState()
        {
            var data = new CurrencySaveData();
            foreach (KeyValuePair<CurrencyType, BigNumber> pair in _balances)
            {
                data.types.Add(pair.Key.ToString());
                data.amounts.Add(pair.Value.ToRawString());
            }

            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string json)
        {
            _balances.Clear();
            if (string.IsNullOrEmpty(json)) return;

            var data = JsonUtility.FromJson<CurrencySaveData>(json);
            int count = Math.Min(data.types.Count, data.amounts.Count);
            for (int i = 0; i < count; i++)
            {
                if (Enum.TryParse(data.types[i], out CurrencyType type))
                {
                    _balances[type] = BigNumber.Parse(data.amounts[i]);
                }
            }
        }

        [Serializable]
        private sealed class CurrencySaveData
        {
            public List<string> types = new List<string>();
            public List<string> amounts = new List<string>();
        }
    }
}
