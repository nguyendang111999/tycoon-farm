using System;
using System.Collections;
using Farm.Core;
using Farm.Money;
using UnityEngine;

namespace Farm.Construction
{
    /// <summary>
    /// A single farm plot: starts Empty (Box) and builds in-place into a growing/upgradeable crop (Built).
    /// One stable GameObject for the plot's whole life — no runtime Instantiate/Destroy on build.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Construction : MonoBehaviour, ISupplier
    {
        private enum PlotState { Empty, Built }

        [Serializable]
        public struct ConstructionState
        {
            public bool IsBuilt;
            public int Level;
            public int Stock;
            public float GrowProgress;
        }

        [Header("Shared")]
        [SerializeField] private CropConfig _config;
        [Tooltip("World Space Canvas positioned above this plot; the Build/Upgrade popup is reparented into it when shown.")]
        [SerializeField] private Transform _uiTarget;

        [Header("Empty State (Box)")]
        [SerializeField] private GameObject _emptyStateRoot;
        [SerializeField] private string _openClipName = "BoxOpen";
        [SerializeField] private PrefabPool _buildDoneEffectPool;

        [Header("Built State (Crop)")]
        [Tooltip("Each direct child of this root is a stock anchor point.")]
        [SerializeField] private GameObject _builtStateRoot;
        [Tooltip("Instantiated once per stock anchor and toggled on/off; never spawned at runtime.")]
        [SerializeField] private GameObject _productPrefab;

        private PlotState _state = PlotState.Empty;
        private Animation _boxAnimation;
        private Transform[] _stockAnchors;
        private GameObject[] _productInstances;
        private int _stock;
        private int _level = 1;
        private float _growElapsed;
        private Coroutine _regenRoutine;

        public CropConfig Config => _config;
        public Transform UITarget => _uiTarget;
        public GameObject ProductPrefab => _productPrefab;
        public bool IsBuilt => _state == PlotState.Built;
        public BigNumber BuildCost => new BigNumber(_config.BuildCost);
        public int Level => _level;
        public int Stock => _stock;
        public int AvailableStock => _stock;
        public int MaxStock => _config.MaxStock;
        public bool IsMaxLevel => ConstructionMath.IsMaxLevel(_config, _level);
        public BigNumber NextUpgradeCost => ConstructionMath.CalculateUpgradeCost(_config, _level);
        public bool IsClaimed { get; private set; }

        CropConfig ISupplier.Crop => _config;
        Vector3 ISupplier.PickupPosition => transform.position;
        GameObject ISupplier.ProductPrefab => _productPrefab;
        int ISupplier.AvailableStock => _stock;
        bool ISupplier.IsClaimed => IsClaimed;

        public bool TryClaim()
        {
            if (_state != PlotState.Built || IsClaimed) return false;

            IsClaimed = true;
            return true;
        }

        public void ReleaseClaim()
        {
            IsClaimed = false;
        }

        bool ISupplier.TryClaim() => TryClaim();
        void ISupplier.ReleaseClaim() => ReleaseClaim();

        private void Awake()
        {
            if (GetComponent<Collider>() == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.center = new Vector3(0f, 1f, 0f);
                boxCollider.size = new Vector3(1.4f, 2f, 1.4f);
            }

            if (_emptyStateRoot != null) _boxAnimation = _emptyStateRoot.GetComponentInChildren<Animation>();

            SetupStockAnchors();
            ApplyState();
        }

        private void SetupStockAnchors()
        {
            // Stock capacity is a design value from CropConfig; the built root's children only decide where visuals are placed.
            int anchorCount = _builtStateRoot != null ? _builtStateRoot.transform.childCount : 0;
            if (anchorCount < _config.MaxStock)
            {
                Debug.LogWarning($"{name}: only {anchorCount} visual anchors under '{_builtStateRoot?.name}' but CropConfig.MaxStock is {_config.MaxStock}.", this);
            }

            int usableAnchors = Mathf.Min(anchorCount, _config.MaxStock);
            _stockAnchors = new Transform[usableAnchors];
            _productInstances = new GameObject[usableAnchors];

            for (int i = 0; i < usableAnchors; i++)
            {
                Transform anchor = _builtStateRoot.transform.GetChild(i);
                _stockAnchors[i] = anchor;

                if (_productPrefab != null)
                {
                    GameObject instance = Instantiate(_productPrefab, anchor);
                    instance.transform.localPosition = Vector3.zero;
                    instance.SetActive(false);
                    _productInstances[i] = instance;
                }
            }
        }

        private void OnEnable()
        {
            if (_state == PlotState.Built) StartRegen();
        }

        private void OnDisable()
        {
            StopRegen();
        }

        private void OnMouseDown()
        {
            if (_state == PlotState.Empty) ConstructionUIController.Instance?.ShowBuildView(this);
            else ConstructionUIController.Instance?.ShowUpgradeView(this);
        }

        public bool TryBuild(ICurrencyService currency)
        {
            if (_state != PlotState.Empty) return false;
            if (!currency.TrySpend(CurrencyType.Cash, BuildCost)) return false;

            StartCoroutine(OpenAndBuild());
            return true;
        }

        private IEnumerator OpenAndBuild()
        {
            float duration = _config.BoxOpenDuration;
            if (_boxAnimation != null)
            {
                _boxAnimation.Play(_openClipName);
                AnimationClip clip = _boxAnimation.clip;
                if (clip != null) duration = clip.length;
            }

            yield return new WaitForSeconds(duration);

            if (_buildDoneEffectPool != null)
            {
                GameObject effect = _buildDoneEffectPool.Rent(transform.position, Quaternion.identity);
                _buildDoneEffectPool.Return(effect, 2f);
            }

            _state = PlotState.Built;
            ApplyState();
            StartRegen();
            ConstructionManager.Instance?.Register(this);
        }

        private void ApplyState()
        {
            bool built = _state == PlotState.Built;
            if (_emptyStateRoot != null) _emptyStateRoot.SetActive(!built);
            if (_builtStateRoot != null) _builtStateRoot.SetActive(built);
        }

        private void StartRegen()
        {
            StopRegen();
            _regenRoutine = StartCoroutine(RegenLoop());
        }

        private void StopRegen()
        {
            if (_regenRoutine != null) StopCoroutine(_regenRoutine);
            _regenRoutine = null;
        }

        private IEnumerator RegenLoop()
        {
            while (true)
            {
                yield return null;
                if (_stock >= MaxStock) continue;

                _growElapsed += Time.deltaTime;
                if (_growElapsed < _config.GrowDuration) continue;

                _growElapsed = 0f;
                SetStock(_stock + 1);
            }
        }

        public bool TryUpgrade(ICurrencyService currency)
        {
            if (IsMaxLevel) return false;
            if (!currency.TrySpend(CurrencyType.Cash, NextUpgradeCost)) return false;

            _level++;
            return true;
        }

        public bool TryHarvestOne(out BigNumber payout)
        {
            payout = BigNumber.Zero;
            if (_stock <= 0) return false;

            float management = ManagementBonuses.Current.GetCropProfitMultiplier(_config);
            payout = ConstructionMath.CalculateHarvestPrice(_config, _level, management);
            SetStock(_stock - 1);
            return true;
        }

        public bool TryCollect(int count, out BigNumber payout)
        {
            payout = BigNumber.Zero;
            if (count <= 0 || _stock < count) return false;

            float management = ManagementBonuses.Current.GetCropProfitMultiplier(_config);
            BigNumber unitPrice = ConstructionMath.CalculateHarvestPrice(_config, _level, management);
            payout = unitPrice * (double)count;
            SetStock(_stock - count);
            return true;
        }

        bool ISupplier.TryCollect(int count, out BigNumber payout) => TryCollect(count, out payout);

        private void SetStock(int newStock)
        {
            _stock = Mathf.Clamp(newStock, 0, MaxStock);

            for (int i = 0; i < _productInstances.Length; i++)
            {
                if (_productInstances[i] != null) _productInstances[i].SetActive(i < _stock);
            }
        }

        public ConstructionState CaptureState()
        {
            return new ConstructionState
            {
                IsBuilt = _state == PlotState.Built,
                Level = _level,
                Stock = _stock,
                GrowProgress = _growElapsed
            };
        }

        public void RestoreState(ConstructionState state)
        {
            _state = state.IsBuilt ? PlotState.Built : PlotState.Empty;
            _level = Mathf.Max(1, state.Level);
            _growElapsed = state.GrowProgress;
            IsClaimed = false;
            ApplyState();
            SetStock(state.Stock);

            StopRegen();
            if (_state == PlotState.Built)
            {
                ConstructionManager.Instance?.Register(this);
                if (isActiveAndEnabled) StartRegen();
            }
        }
    }
}
