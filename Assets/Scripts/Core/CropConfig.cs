using UnityEngine;

namespace Farm.Core
{
    /// <summary>Per-crop economy tuning: build cost, growth timing, and level-based profit/upgrade-cost curves.</summary>
    [CreateAssetMenu(menuName = "Farm/Construction/Crop Config", fileName = "CropConfig")]
    public sealed class CropConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _cropId = "tomato";
        [SerializeField] private string _displayName = "Tomato";
        [SerializeField] private Sprite _icon;

        [Header("Build")]
        [SerializeField] private long _buildCost = 50;
        [SerializeField] private float _boxOpenDuration = 1f;

        [Header("Growth")]
        [SerializeField] private float _growDuration = 8f;
        [SerializeField] private long _baseHarvestPrice = 10;
        [SerializeField] private int _maxStock = 3;

        [Header("Level Upgrade")]
        [SerializeField] private int _maxLevel = 10;
        [Tooltip("X = level, Y = profit multiplier. Default: +10% per level (Level2=1.1, Level3=1.2, ...).")]
        [SerializeField] private AnimationCurve _profitMultiplierByLevel = AnimationCurve.Linear(1f, 1f, 10f, 1.9f);
        [Tooltip("Cash cost to upgrade from level 1 to level 2.")]
        [SerializeField] private long _baseUpgradeCost = 100;
        [Tooltip("Cost multiplier applied per level: cost(level) = baseUpgradeCost * growthRate^(level-1).")]
        [SerializeField] private float _upgradeCostGrowthRate = 1.3f;

        public string CropId => _cropId;
        public string DisplayName => _displayName;
        public long BuildCost => _buildCost;
        public float BoxOpenDuration => _boxOpenDuration;
        public float GrowDuration => _growDuration;
        public long BaseHarvestPrice => _baseHarvestPrice;
        public int MaxStock => _maxStock;
        public int MaxLevel => _maxLevel;
        public Sprite Icon => _icon;

        public float GetProfitMultiplier(int level) => _profitMultiplierByLevel.Evaluate(level);

        public BigNumber GetUpgradeCost(int currentLevel)
        {
            BigNumber cost = new BigNumber(_baseUpgradeCost);
            // Multiplies BigNumber-by-BigNumber instead of Math.Pow(double, double) so cost can't overflow before reaching BigNumber's exponent.
            for (int i = 1; i < currentLevel; i++)
            {
                cost *= _upgradeCostGrowthRate;
            }

            return cost;
        }
    }
}
