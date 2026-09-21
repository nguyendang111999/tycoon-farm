using Farm.Core;
using UnityEngine;

namespace Farm.Upgrade
{
    /// <summary>Scene bootstrap that owns the single <see cref="UpgradeService"/> instance.</summary>
    [DefaultExecutionOrder(-90)]
    public sealed class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] private UpgradeConfig _config;

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

            Service = new UpgradeService(_config);
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
