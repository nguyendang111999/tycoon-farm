using Farm.Core;
using Farm.Money;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Farm.Construction
{
    /// <summary>
    /// Owns the single, already-placed Build/Upgrade view instances and populates them for whichever
    /// plot/crop was clicked, so no UI prefab is ever instantiated or destroyed at runtime.
    /// </summary>
    public sealed class ConstructionUIController : MonoBehaviour
    {
        public static ConstructionUIController Instance { get; private set; }

        [Header("Build View")]
        [SerializeField] private GameObject _buildViewRoot;
        [SerializeField] private TMP_Text _buildNameText;
        [SerializeField] private TMP_Text _buildCostText;
        [SerializeField] private Button _buildUnlockButton;
        [SerializeField] private Button _buildCloseButton;

        [Header("Upgrade View")]
        [SerializeField] private GameObject _upgradeViewRoot;
        [SerializeField] private TMP_Text _upgradeLevelText;
        [SerializeField] private TMP_Text _upgradeProductText;
        [SerializeField] private TMP_Text _upgradeCostText;
        [SerializeField] private Slider _upgradeProgressSlider;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private GameObject _upgradeMaxState;
        [SerializeField] private Button _upgradeCloseButton;

        private ICurrencyService _currency;
        private ConstructionPlot _activePlot;
        private Construction _activeConstruction;

        private void Awake()
        {
            Instance = this;
            _currency = MoneyManager.Instance.Currency;

            _buildCloseButton.onClick.AddListener(HideBuildView);
            _buildUnlockButton.onClick.AddListener(HandleBuildConfirmed);
            _upgradeCloseButton.onClick.AddListener(HideUpgradeView);
            _upgradeButton.onClick.AddListener(HandleUpgradeConfirmed);

            HideBuildView();
            HideUpgradeView();
        }

        public void ShowBuildView(ConstructionPlot plot)
        {
            _activePlot = plot;
            _buildNameText.text = plot.Config.DisplayName;
            _buildCostText.text = NumberFormatter.Format(plot.BuildCost);
            _buildViewRoot.SetActive(true);
        }

        private void HandleBuildConfirmed()
        {
            if (_activePlot != null && _activePlot.TryBuild(_currency)) HideBuildView();
        }

        private void HideBuildView()
        {
            _buildViewRoot.SetActive(false);
            _activePlot = null;
        }

        public void ShowUpgradeView(Construction construction)
        {
            _activeConstruction = construction;
            RefreshUpgradeView();
            _upgradeViewRoot.SetActive(true);
        }

        private void RefreshUpgradeView()
        {
            if (_activeConstruction == null) return;

            _upgradeLevelText.text = $"Lv. {_activeConstruction.Level}";
            _upgradeProductText.text = _activeConstruction.Config.DisplayName;

            if (_upgradeProgressSlider != null)
            {
                _upgradeProgressSlider.value = (float)_activeConstruction.Level / _activeConstruction.Config.MaxLevel;
            }

            bool maxed = _activeConstruction.IsMaxLevel;
            _upgradeMaxState.SetActive(maxed);
            _upgradeButton.gameObject.SetActive(!maxed);
            if (!maxed) _upgradeCostText.text = NumberFormatter.Format(_activeConstruction.NextUpgradeCost);
        }

        private void HandleUpgradeConfirmed()
        {
            if (_activeConstruction != null && _activeConstruction.TryUpgrade(_currency)) RefreshUpgradeView();
        }

        private void HideUpgradeView()
        {
            _upgradeViewRoot.SetActive(false);
            _activeConstruction = null;
        }
    }
}
