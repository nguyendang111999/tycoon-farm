using Farm.Core;
using UnityEngine;

namespace Farm.Upgrade
{
    /// <summary>Scene bootstrap that owns the single <see cref="UpgradeService"/> instance and wires its stat effects to <see cref="GameStats.Global"/>.</summary>
    [DefaultExecutionOrder(-90)]
    public sealed class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] private UpgradeConfig _config;

        [Header("Stat Definitions (wires UpgradeType effects to GameStats.Global)")]
        [SerializeField] private StatDefinition _cropProfitStat;
        [SerializeField] private StatDefinition _customerCapacityStat;
        [SerializeField] private StatDefinition _workerCountStat;

        public UpgradeConfig Config => _config;
        public UpgradeService Service { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Service = new UpgradeService(_config, _cropProfitStat, _customerCapacityStat, _workerCountStat);
            GameSaveService.Register(Service);
        }

        private void Start()
        {
            GameSaveService.LoadOnce();
        }

        private void OnApplicationQuit() => GameSaveService.Save();

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) GameSaveService.Save();
        }
    }
}
