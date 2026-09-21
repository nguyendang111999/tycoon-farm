using Farm.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Farm.Upgrade
{
    /// <summary>One row in the Management upgrade list; pure view, no purchase logic of its own.</summary>
    public sealed class UpgradeItemView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private Button _buyButton;

        public void Bind(UpgradeEntry entry, bool isPurchased, bool canAfford, System.Action onBuy)
        {
            if (_icon != null && entry.Icon != null) _icon.sprite = entry.Icon;
            if (_titleText != null) _titleText.text = entry.DisplayName;
            if (_descriptionText != null) _descriptionText.text = entry.Description;
            if (_costText != null) _costText.text = isPurchased ? "OWNED" : NumberFormatter.Format(entry.Cost);

            _buyButton.interactable = !isPurchased && canAfford;
            _buyButton.onClick.RemoveAllListeners();
            if (!isPurchased) _buyButton.onClick.AddListener(() => onBuy());
        }
    }
}

