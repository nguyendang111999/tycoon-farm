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
        [SerializeField] private Image _buildIcon;

        [Header("Upgrade View")]
        [SerializeField] private GameObject _upgradeViewRoot;
        [SerializeField] private TMP_Text _upgradeLevelText;
        [SerializeField] private TMP_Text _curProfitText;
        [SerializeField] private TMP_Text _upgradeProductText;
        [SerializeField] private TMP_Text _upgradeCostText;
        [SerializeField] private Slider _upgradeProgressSlider;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private GameObject _upgradeMaxState;
        [SerializeField] private Button _upgradeCloseButton;

        private ICurrencyService _currency;
        private CropPlot _active;
        private bool _suppressCloseThisFrame;
        private bool _pressStartedOutsideUI;
        private Transform _buildViewHomeParent;
        private Transform _upgradeViewHomeParent;

        private void Awake()
        {
            Instance = this;
            _currency = MoneyManager.Instance.Currency;

            // Popups get reparented into whichever plot/crop's canvas is showing them; remember where
            // they started so HideXView can return them here before that target is ever destroyed.
            _buildViewHomeParent = _buildViewRoot.transform.parent;
            _upgradeViewHomeParent = _upgradeViewRoot.transform.parent;

            if (_buildViewRoot.TryGetComponent<Canvas>(out var buildViewCanvas))
            {
                // Do something if needed
                buildViewCanvas.worldCamera = Camera.main;
            }
            if (_upgradeViewRoot.TryGetComponent<Canvas>(out var upgradeViewCanvas))
            {
                upgradeViewCanvas.worldCamera = Camera.main;
            }

            if (_buildCloseButton != null)
                _buildCloseButton.onClick.AddListener(HideBuildView);
            if (_buildUnlockButton != null)
                _buildUnlockButton.onClick.AddListener(HandleBuildConfirmed);
            if (_upgradeCloseButton != null)
                _upgradeCloseButton.onClick.AddListener(HideUpgradeView);
            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(HandleUpgradeConfirmed);

            HideBuildView();
            HideUpgradeView();
        }

        public void ShowBuildView(CropPlot plot)
        {
            HideUpgradeView();

            _active = plot;
            ReparentPopup(_buildViewRoot.transform, plot.UITarget);
            _buildNameText.text = plot.Config.DisplayName;
            _buildCostText.text = NumberFormatter.Format(plot.BuildCost);
            _buildIcon.sprite = plot.Config.Icon;
            _buildViewRoot.SetActive(true);
            _suppressCloseThisFrame = true;
        }

        private void HandleBuildConfirmed()
        {
            if (_active != null && _active.TryBuild(_currency)) HideBuildView();
        }

        private void HideBuildView()
        {
            _buildViewRoot.transform.SetParent(_buildViewHomeParent, false);
            _buildViewRoot.SetActive(false);
            _active = null;
        }

        public void ShowUpgradeView(CropPlot construction)
        {
            HideBuildView();

            _active = construction;
            ReparentPopup(_upgradeViewRoot.transform, construction.UITarget);
            RefreshUpgradeView();
            _upgradeViewRoot.SetActive(true);
            _suppressCloseThisFrame = true;
        }

        private void RefreshUpgradeView()
        {
            if (_active == null) return;

            _upgradeLevelText.text = $"Lv. {_active.Level}";
            _upgradeProductText.text = _active.Config.DisplayName;
            _curProfitText.text = NumberFormatter.Format(
                ConstructionMath.CalculateHarvestPrice(_active.Config, _active.Level));

            if (_upgradeProgressSlider != null)
            {
                _upgradeProgressSlider.value = (float)_active.Level / _active.Config.MaxLevel;
            }

            bool maxed = _active.IsMaxLevel;
            _upgradeMaxState.SetActive(maxed);
            _upgradeButton.gameObject.SetActive(!maxed);
            if (!maxed) _upgradeCostText.text = NumberFormatter.Format(_active.NextUpgradeCost);
        }

        private void HandleUpgradeConfirmed()
        {
            if (_active != null && _active.TryUpgrade(_currency)) RefreshUpgradeView();
        }

        private void HideUpgradeView()
        {
            _upgradeViewRoot.transform.SetParent(_upgradeViewHomeParent, false);
            _upgradeViewRoot.SetActive(false);
            _active = null;
        }

        private static void ReparentPopup(Transform popup, Transform uiTarget)
        {
            if (uiTarget == null) return;

            popup.SetParent(uiTarget, false);
            popup.localPosition = Vector3.zero;
            popup.localRotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            // Skip the frame a popup was just opened so that same click doesn't immediately close it.
            if (_suppressCloseThisFrame)
            {
                _suppressCloseThisFrame = false;
                return;
            }

            bool anyOpen = _buildViewRoot.activeSelf || _upgradeViewRoot.activeSelf;
            if (!anyOpen) return;

            if (PointerInput.TryGetDown(out _))
            {
                _pressStartedOutsideUI = !PointerInput.IsOverUI();
            }

            // Close on release, never on press: a uGUI Button only fires its click on pointer-up, so tearing the
            // popup down on pointer-down would swallow every tap on its own buttons.
            if (_pressStartedOutsideUI && PointerInput.TryGetUp())
            {
                _pressStartedOutsideUI = false;
                HideBuildView();
                HideUpgradeView();
            }
        }
    }
}
