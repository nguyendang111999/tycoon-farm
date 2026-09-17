using System.Collections;
using System.Collections.Generic;
using Farm.Construction;
using Farm.Core;
using Farm.Money;
using UnityEngine;

namespace Farm.Customer
{
    /// <summary>Owns the customer pool; spawns up to capacity whenever a dock slot and a built crop type are both available.</summary>
    public sealed class CustomerManager : MonoBehaviour
    {
        public static CustomerManager Instance { get; private set; }

        [SerializeField] private Market _market;
        [SerializeField] private PrefabPool _customerPool;
        [SerializeField] private int _startingCapacity = 1;
        [SerializeField] private float _retryInterval = 2f;

        private readonly List<Customer> _active = new List<Customer>();
        private readonly Dictionary<DockSlot, Customer> _slotOccupants = new Dictionary<DockSlot, Customer>();
        private int _capacity;

        public IReadOnlyList<Customer> ActiveCustomers => _active;

        private void Awake()
        {
            Instance = this;
            _capacity = _startingCapacity;
        }

        private void Start()
        {
            StartCoroutine(SpawnLoop());
        }

        public void IncreaseCapacity(int delta)
        {
            _capacity = Mathf.Clamp(_capacity + delta, _capacity, _market.DockSlots.Count);
        }

        private IEnumerator SpawnLoop()
        {
            var wait = new WaitForSeconds(_retryInterval);
            while (true)
            {
                DockSlot freeSlot = _active.Count < _capacity ? FindFreeSlot() : null;
                IReadOnlyList<CropConfig> builtCrops = ConstructionManager.Instance != null
                    ? ConstructionManager.Instance.BuiltCropTypes
                    : null;

                if (freeSlot == null || builtCrops == null || builtCrops.Count == 0)
                {
                    yield return wait;
                    continue;
                }

                CropConfig requestedCrop = builtCrops[Random.Range(0, builtCrops.Count)];
                SpawnCustomer(requestedCrop, freeSlot);
            }
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
            if(slot == null)
            {
                Debug.Log($"No slot specified for customer requesting {requestedCrop.DisplayName}.");
            }
            if (requestedCrop == null)
            {
                Debug.Log($"No requested crop specified for customer at slot {slot.name}.");
            }
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
