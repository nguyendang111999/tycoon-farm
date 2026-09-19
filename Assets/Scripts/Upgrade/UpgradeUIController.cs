using System.Collections.Generic;
using Farm.Core;
using Farm.Money;
using UnityEngine;
using UnityEngine.UI;

namespace Farm.Upgrade
{
    /// <summary>Populates the Management upgrade list from <see cref="UpgradeConfig"/> and refreshes it on purchase/balance changes.</summary>
    public sealed class UpgradeUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _viewRoot;
        [SerializeField] private Transform _itemsParent;
        [SerializeField] private UpgradeItemView _itemPrefab;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;

        private readonly List<UpgradeItemView> _rows = new List<UpgradeItemView>();
        private ICurrencyService _currency;

        private void Awake()
        {
            _currency = MoneyManager.Instance.Currency;

            if (_openButton != null) _openButton.onClick.AddListener(Open);
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);

            BuildRows();
            Close();
        }

        private void OnEnable()
        {
            UpgradeManager.Instance.Service.Changed += RefreshAll;
            _currency.BalanceChanged += HandleBalanceChanged;
        }

        private void OnDisable()
        {
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.Service.Changed -= RefreshAll;
            _currency.BalanceChanged -= HandleBalanceChanged;
        }

        private void HandleBalanceChanged(CurrencyType type, BigNumber newBalance, BigNumber delta) => RefreshAll();

        private void Open()
        {
            RefreshAll();
            _viewRoot.SetActive(true);
        }

        private void Close() => _viewRoot.SetActive(false);

        private void BuildRows()
        {
            foreach (UpgradeEntry entry in UpgradeManager.Instance.Config.Entries)
            {
                _rows.Add(Instantiate(_itemPrefab, _itemsParent));
            }
        }

        private void RefreshAll()
        {
            UpgradeService service = UpgradeManager.Instance.Service;
            IReadOnlyList<UpgradeEntry> entries = UpgradeManager.Instance.Config.Entries;

            for (int i = 0; i < _rows.Count && i < entries.Count; i++)
            {
                UpgradeEntry entry = entries[i];
                bool purchased = service.IsPurchased(entry);
                bool canAfford = !purchased && _currency.GetBalance(entry.CostCurrency) >= entry.Cost;

                _rows[i].Bind(entry, purchased, canAfford, () => TryBuy(entry));
            }
        }

        private void TryBuy(UpgradeEntry entry) => UpgradeManager.Instance.Service.TryPurchase(entry, _currency);
    }
}
