using System.Collections.Generic;
using UnityEngine;

namespace Farm.Customer
{
    /// <summary>Level furniture data: where customers enter/exit and the dock slots they can wait at.</summary>
    public sealed class Market : MonoBehaviour
    {
        [SerializeField] private Transform _customerStart;
        [SerializeField] private Transform _customerEnd;
        [SerializeField] private DockSlot[] _dockSlots;

        public Transform CustomerStart => _customerStart;
        public Transform CustomerEnd => _customerEnd;
        public IReadOnlyList<DockSlot> DockSlots => _dockSlots;
    }
}
