using System.Collections;
using Farm.Core;
using Farm.Money;
using UnityEngine;

namespace Farm.Construction
{
    /// <summary>The built, growing crop: regenerates harvestable stock over time and can be leveled up for more profit per unit.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Construction : MonoBehaviour
    {
        [SerializeField] private CropConfig _config;
        [Tooltip("Name of the child transform whose children are stock anchor points (e.g. 'Tomato', 'Pumpkin') — set per crop prefab.")]
        [SerializeField] private string _stockAnchorRootName = "Tomato";
        [Tooltip("Instantiated once per stock anchor and toggled on/off; never spawned at runtime.")]
        [SerializeField] private GameObject _productPrefab;

        private Transform[] _stockAnchors;
        private GameObject[] _productInstances;
        private int _stock;
        private int _level = 1;
        private Coroutine _regenRoutine;

        public CropConfig Config => _config;
        public int Level => _level;
        public int Stock => _stock;
        public int MaxStock => _config.MaxStock;
        public bool IsMaxLevel => ConstructionMath.IsMaxLevel(_config, _level);
        public BigNumber NextUpgradeCost => ConstructionMath.CalculateUpgradeCost(_config, _level);

        private void Awake()
        {
            if (GetComponent<Collider>() == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.center = new Vector3(0f, 1f, 0f);
                boxCollider.size = new Vector3(1.4f, 2f, 1.4f);
            }

            // Stock capacity is a design value from CropConfig; the anchor root only decides where visuals are placed.
            Transform anchorRoot = transform.Find(_stockAnchorRootName);
            int anchorCount = anchorRoot != null ? anchorRoot.childCount : 0;
            if (anchorCount < _config.MaxStock)
            {
                Debug.LogWarning($"{name}: only {anchorCount} visual anchors under '{_stockAnchorRootName}' but CropConfig.MaxStock is {_config.MaxStock}.", this);
            }

            int usableAnchors = Mathf.Min(anchorCount, _config.MaxStock);
            _stockAnchors = new Transform[usableAnchors];
            _productInstances = new GameObject[usableAnchors];

            for (int i = 0; i < usableAnchors; i++)
            {
                Transform anchor = anchorRoot.GetChild(i);
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
            _regenRoutine = StartCoroutine(RegenLoop());
        }

        private void OnDisable()
        {
            if (_regenRoutine != null) StopCoroutine(_regenRoutine);
        }

        private void OnMouseDown()
        {
            ConstructionUIController.Instance?.ShowUpgradeView(this);
        }

        private IEnumerator RegenLoop()
        {
            var wait = new WaitForSeconds(_config.GrowDuration);
            while (true)
            {
                yield return wait;
                if (_stock < MaxStock) SetStock(_stock + 1);
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

            payout = ConstructionMath.CalculateHarvestPrice(_config, _level);
            SetStock(_stock - 1);
            return true;
        }

        private void SetStock(int newStock)
        {
            _stock = Mathf.Clamp(newStock, 0, MaxStock);
            for (int i = 0; i < _productInstances.Length; i++)
            {
                if (_productInstances[i] != null) _productInstances[i].SetActive(i < _stock);
            }
        }
    }
}
