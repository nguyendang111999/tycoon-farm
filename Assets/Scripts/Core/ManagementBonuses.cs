using System;

namespace Farm.Core
{
    /// <summary>Global access point for management-upgrade effects; defaults to a no-op until Farm.Upgrade's manager sets Current.</summary>
    public static class ManagementBonuses
    {
        public static IManagementBonuses Current { get; set; } = new NoBonuses();

        private sealed class NoBonuses : IManagementBonuses
        {
            public int BonusCustomerCapacity => 0;
            public int BonusWorkers => 0;

            public event Action Changed { add { } remove { } }

            public float GetCropProfitMultiplier(CropConfig crop) => 1f;
        }
    }
}
