using Farm.Core;
using TMPro;
using UnityEngine;

namespace Farm.Money
{
    /// <summary>Binds a TMP label to a live currency balance. Attach to the Text (TMP) object under a currency's UI row.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CurrencyDisplayBinding : MonoBehaviour
    {
        [SerializeField] private CurrencyType _currencyType = CurrencyType.Cash;

        private TMP_Text _label;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (MoneyManager.Instance == null) return;

            MoneyManager.Instance.Currency.BalanceChanged += HandleBalanceChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (MoneyManager.Instance == null) return;

            MoneyManager.Instance.Currency.BalanceChanged -= HandleBalanceChanged;
        }

        private void HandleBalanceChanged(CurrencyType type, BigNumber newBalance, BigNumber delta)
        {
            if (type == _currencyType) _label.text = NumberFormatter.Format(newBalance);
        }

        private void Refresh()
        {
            _label.text = NumberFormatter.Format(MoneyManager.Instance.Currency.GetBalance(_currencyType));
        }
    }
}
