using UnityEngine;

namespace Farm.Core
{
    /// <summary>A pending request a worker can deliver a matching crop to.</summary>
    public interface IOrder
    {
        CropConfig RequestedCrop { get; }
        int RequestedQuantity { get; }
        Vector3 DeliveryPosition { get; }

        bool Fulfill(CropConfig deliveredCrop, GameObject productPrefab, BigNumber payout);
        void ReleaseClaim();
    }
}
