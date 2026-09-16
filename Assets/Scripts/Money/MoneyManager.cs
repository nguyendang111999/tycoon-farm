using Farm.Core;
using UnityEngine;

namespace Farm.Money
{
    /// <summary>Scene bootstrap that owns the single <see cref="CurrencyService"/> instance and its save lifecycle.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        public ICurrencyService Currency { get; private set; }

        [Header("Starting Balance (only applied on a brand new save)")]
        [SerializeField] private CurrencyType _startingCurrency = CurrencyType.Cash;
        [SerializeField] private long _startingAmount = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            var service = new CurrencyService();
            Currency = service;
            GameSaveService.Register(service);
        }

        private void Start()
        {
            GameSaveService.LoadOnce();

            if (_startingAmount > 0 && Currency.GetBalance(_startingCurrency).IsZero)
            {
                Currency.Add(_startingCurrency, new BigNumber(_startingAmount));
            }
        }

        private void OnApplicationQuit() => GameSaveService.Save();

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) GameSaveService.Save();
        }
    }
}
