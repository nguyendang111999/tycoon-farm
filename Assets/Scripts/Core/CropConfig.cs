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
        [Tooltip("Cost = costMantissa * 10^costExponent (e.g. mantissa 5, exponent 1 = 50). Supports arbitrarily large values, same representation as BigNumber.")]
        [SerializeField] private double _buildCostMantissa = 5d;
        [SerializeField] private int _buildCostExponent = 1;
        [SerializeField] private float _boxOpenDuration = 1f;

        [Header("Growth")]
        [SerializeField] private float _growDuration = 8f;
        [Tooltip("Cost = costMantissa * 10^costExponent (e.g. mantissa 1, exponent 1 = 10). Supports arbitrarily large values, same representation as BigNumber.")]
        [SerializeField] private double _baseHarvestPriceMantissa = 1d;
        [SerializeField] private int _baseHarvestPriceExponent = 1;
        [SerializeField] private int _maxStock = 3;

        [Header("Level Upgrade")]
        [SerializeField] private int _maxLevel = 10;
        [Tooltip("X = level, Y = profit multiplier. Default: +10% per level (Level2=1.1, Level3=1.2, ...).")]
        [SerializeField] private AnimationCurve _profitMultiplierByLevel = AnimationCurve.Linear(1f, 1f, 10f, 1.9f);
        [Tooltip("Cash cost to upgrade from level 1 to level 2. Cost = costMantissa * 10^costExponent.")]
        [SerializeField] private double _baseUpgradeCostMantissa = 1d;
        [SerializeField] private int _baseUpgradeCostExponent = 2;
        [Tooltip("Cost multiplier applied per level: cost(level) = baseUpgradeCost * growthRate^(level-1).")]
        [SerializeField] private float _upgradeCostGrowthRate = 1.3f;

        public string CropId => _cropId;
        public string DisplayName => _displayName;
        public BigNumber BuildCost => new BigNumber(_buildCostMantissa, _buildCostExponent);
        public float BoxOpenDuration => _boxOpenDuration;
        public float GrowDuration => _growDuration;
        public BigNumber BaseHarvestPrice => new BigNumber(_baseHarvestPriceMantissa, _baseHarvestPriceExponent);
        public int MaxStock => _maxStock;
        public int MaxLevel => _maxLevel;
        public Sprite Icon => _icon;

        public float GetProfitMultiplier(int level) => _profitMultiplierByLevel.Evaluate(level);

        public BigNumber GetUpgradeCost(int currentLevel)
        {
            BigNumber cost = new BigNumber(_baseUpgradeCostMantissa, _baseUpgradeCostExponent);
            // Multiplies BigNumber-by-BigNumber instead of Math.Pow(double, double) so cost can't overflow before reaching BigNumber's exponent.
            for (int i = 1; i < currentLevel; i++)
            {
                cost *= _upgradeCostGrowthRate;
            }

            return cost;
        }
    }
}
