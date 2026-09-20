using Farm.Core;
using UnityEngine;

namespace Farm.Money
{
    /// <summary>Manual test helper for Play mode: press keys to add/spend currency and watch bound UI update.</summary>
    public sealed class CurrencyDebugTester : MonoBehaviour
    {
        [SerializeField] private CurrencyType _currencyType = CurrencyType.Cash;
        [SerializeField] private long _amount = 100;
        [SerializeField] private KeyCode _addKey = KeyCode.Equals;
        [SerializeField] private KeyCode _spendKey = KeyCode.Minus;

        private void Update()
        {
            if (MoneyManager.Instance == null) return;

            if (Input.GetKeyDown(_addKey))
            {
                MoneyManager.Instance.Currency.Add(_currencyType, new BigNumber(_amount));
            }

            if (Input.GetKeyDown(_spendKey))
            {
                bool spent = MoneyManager.Instance.Currency.TrySpend(_currencyType, new BigNumber(_amount));
                if (!spent) Debug.Log("[CurrencyDebugTester] Not enough balance to spend.");
            }
        }

        [ContextMenu("Add Amount")]
        private void AddAmount()
        {
            if (MoneyManager.Instance == null) return;
            MoneyManager.Instance.Currency.Add(_currencyType, new BigNumber(_amount));
        }

        [ContextMenu("Spend Amount")]
        private void SpendAmount()
        {
            if (MoneyManager.Instance == null) return;
            bool spent = MoneyManager.Instance.Currency.TrySpend(_currencyType, new BigNumber(_amount));
            if (!spent) Debug.Log("[CurrencyDebugTester] Not enough balance to spend.");
        }

        [ContextMenu("Wipe All Save Data")]
        private void WipeAllSaveData()
        {
            GameSaveService.WipeAllData();
        }
    }
}
