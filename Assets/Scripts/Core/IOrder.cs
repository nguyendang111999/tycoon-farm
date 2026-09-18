using UnityEngine;

namespace Farm.Core
{
    /// <summary>A pending request a worker can deliver a matching crop to.</summary>
    public interface IOrder
    {
        CropConfig RequestedCrop { get; }
        Vector3 DeliveryPosition { get; }

        bool Fulfill(CropConfig deliveredCrop, BigNumber payout);
        void ReleaseClaim();
    }
}
