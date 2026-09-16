using Farm.Core;

namespace Farm.Construction
{
    /// <summary>Pure pricing/level rules kept free of MonoBehaviour state so they're directly unit-testable.</summary>
    public static class ConstructionMath
    {
        public static BigNumber CalculateHarvestPrice(CropConfig config, int level)
        {
            return new BigNumber(config.BaseHarvestPrice) * config.GetProfitMultiplier(level);
        }

        public static BigNumber CalculateUpgradeCost(CropConfig config, int currentLevel)
        {
            return config.GetUpgradeCost(currentLevel);
        }

        public static bool IsMaxLevel(CropConfig config, int level) => level >= config.MaxLevel;
    }
}
