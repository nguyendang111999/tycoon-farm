using UnityEngine;

namespace Farm.Customer
{
    /// <summary>One customer service position: where a customer waits, where a worker hands off goods, and where the pay effect anchors.</summary>
    public sealed class DockSlot : MonoBehaviour
    {
        [SerializeField] private Transform _waitPoint;
        [SerializeField] private Transform _deliveryPoint;
        [SerializeField] private Transform _currencyAnchor;

        public Transform WaitPoint => _waitPoint;
        public Transform DeliveryPoint => _deliveryPoint;
        public Transform CurrencyAnchor => _currencyAnchor;
    }
}
