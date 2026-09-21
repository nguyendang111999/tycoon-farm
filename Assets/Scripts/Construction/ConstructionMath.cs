using Farm.Core;

namespace Farm.Construction
{
    /// <summary>Pure pricing/level rules kept free of MonoBehaviour state so they're directly unit-testable.</summary>
    public static class ConstructionMath
    {
        public static BigNumber CalculateHarvestPrice(CropConfig config, int level)
        {
            return config.BaseHarvestPrice * config.GetProfitMultiplier(level);
        }

        /// <summary>Calculates harvest price combining a plot's own stat sheet (e.g. its level source) and the global stat sheet.</summary>
        public static BigNumber CalculateHarvestPrice(CropConfig config, StatDefinition profitStat, StatSheet plotStats, StatSheet globalStats)
        {
            if (config == null) return BigNumber.Zero;
            if (profitStat == null) return config.BaseHarvestPrice;

            float plotMult = plotStats != null ? plotStats.Evaluate(profitStat, 1f, config) : 1f;
            float globalMult = globalStats != null ? globalStats.Evaluate(profitStat, 1f, config) : 1f;

            return config.BaseHarvestPrice * (double)(plotMult * globalMult);
        }

        public static BigNumber CalculateUpgradeCost(CropConfig config, int currentLevel)
        {
            return config.GetUpgradeCost(currentLevel);
        }

        public static bool IsMaxLevel(CropConfig config, int level) => level >= config.MaxLevel;
    }
}
