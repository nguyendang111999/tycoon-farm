using System;

namespace Farm.Core
{
    /// <summary>Aggregated effects of every purchased management upgrade; queried live, never cached by callers.</summary>
    public interface IManagementBonuses
    {
        int BonusCustomerCapacity { get; }
        int BonusWorkers { get; }

        event Action Changed;

        float GetCropProfitMultiplier(CropConfig crop);
    }
}
