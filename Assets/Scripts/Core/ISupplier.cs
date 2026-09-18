using UnityEngine;

namespace Farm.Core
{
    /// <summary>A crop production source a worker can collect a harvested unit from.</summary>
    public interface ISupplier
    {
        CropConfig Crop { get; }
        Vector3 PickupPosition { get; }
        GameObject ProductPrefab { get; }

        bool TryCollect(out BigNumber payout);
    }
}
