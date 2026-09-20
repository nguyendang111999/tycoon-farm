using System.Collections.Generic;
using UnityEngine;

namespace Farm.Customer
{
    /// <summary>Level furniture data: where customers enter/exit and the dock slots they can wait at.</summary>
    public sealed class Market : MonoBehaviour
    {
        [SerializeField] private Transform _customerStart;
        [SerializeField] private Transform _customerEnd;
        [Tooltip("List of resting home spots for workers. If empty, falls back to _deliveryHome.")]
        [SerializeField] private Transform[] _deliveryHomes;
        [SerializeField] private DockSlot[] _dockSlots;

        public Transform CustomerStart => _customerStart;
        public Transform CustomerEnd => _customerEnd;
        public Transform DeliveryHome => _deliveryHomes != null && _deliveryHomes.Length > 0 ? _deliveryHomes[0] : null;
        public IReadOnlyList<DockSlot> DockSlots => _dockSlots;

        public IReadOnlyList<Transform> DeliveryHomes
        {
            get
            {
                if (_deliveryHomes != null && _deliveryHomes.Length > 0)
                {
                    return _deliveryHomes;
                }

                return System.Array.Empty<Transform>();
            }
        }

        public Transform GetDeliveryHome(int index)
        {
            var homes = DeliveryHomes;
            if (homes.Count == 0) return transform;
            return homes[Mathf.Abs(index) % homes.Count];
        }
    }
}
