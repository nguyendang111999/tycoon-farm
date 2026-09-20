using UnityEngine;

namespace Farm.Core
{
    /// <summary>A crop production source a worker can collect harvested units from.</summary>
    public interface ISupplier
    {
        CropConfig Crop { get; }
        Vector3 PickupPosition { get; }
        GameObject ProductPrefab { get; }
        int AvailableStock { get; }
        bool IsClaimed { get; }

        bool TryClaim();
        void ReleaseClaim();
        bool TryCollect(int count, out BigNumber payout);
    }
}
