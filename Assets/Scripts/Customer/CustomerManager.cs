using System.Collections.Generic;
using Farm.Core;
using UnityEngine;

namespace Farm.Customer
{
    /// <summary>Owns the customer pool; spawns an order for a requested crop whenever a dock slot is free and there's room under capacity.</summary>
    public sealed class CustomerManager : MonoBehaviour
    {
        public static CustomerManager Instance { get; private set; }

        [SerializeField] private Market _market;
        [SerializeField] private PrefabPool _customerPool;
        [SerializeField] private int _startingCapacity = 1;

        private readonly List<Customer> _active = new List<Customer>();
        private readonly Dictionary<DockSlot, Customer> _slotOccupants = new Dictionary<DockSlot, Customer>();

        public IReadOnlyList<Customer> ActiveCustomers => _active;

        /// <summary>Starting capacity plus any purchased AddCustomer upgrades, clamped to the number of dock slots.</summary>
        public int Capacity => Mathf.Clamp(_startingCapacity + ManagementBonuses.Current.BonusCustomerCapacity, _startingCapacity, _market.DockSlots.Count);

        private void Awake()
        {
            Instance = this;
        }

        public bool TrySpawnCustomer(CropConfig requestedCrop)
        {
            if (requestedCrop == null || _active.Count >= Capacity) return false;

            DockSlot freeSlot = FindFreeSlot();
            if (freeSlot == null) return false;

            SpawnCustomer(requestedCrop, freeSlot);
            return true;
        }

        private DockSlot FindFreeSlot()
        {
            foreach (DockSlot slot in _market.DockSlots)
            {
                if (!_slotOccupants.ContainsKey(slot)) return slot;
            }

            return null;
        }

        private void SpawnCustomer(CropConfig requestedCrop, DockSlot slot)
        {
            GameObject instance = _customerPool.Rent(_market.CustomerStart.position, _market.CustomerStart.rotation);
            var customer = instance.GetComponent<Customer>();

            _slotOccupants[slot] = customer;
            _active.Add(customer);
            customer.Initialize(requestedCrop, slot, _market.CustomerEnd);
        }

        public void OnCustomerReachedExit(Customer customer)
        {
            if (customer.AssignedSlot != null) _slotOccupants.Remove(customer.AssignedSlot);

            _active.Remove(customer);
            _customerPool.Return(customer.gameObject);
        }
    }
}

