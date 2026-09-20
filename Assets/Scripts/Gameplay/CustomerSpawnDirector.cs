using System.Collections;
using System.Collections.Generic;
using Farm.Construction;
using Farm.Core;
using Farm.Customer;
using UnityEngine;

namespace Farm.Gameplay
{
    /// <summary>Composition root: periodically asks Construction for a buildable crop and Customer to spawn an order for it.</summary>
    public sealed class CustomerSpawnDirector : MonoBehaviour
    {
        [SerializeField] private float _retryInterval = 2f;
        [SerializeField] private int _minOrderQuantity = 1;
        [SerializeField] private int _maxOrderQuantity = 3;

        private void Start()
        {
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            var wait = new WaitForSeconds(_retryInterval);
            while (true)
            {
                TrySpawnOne();
                yield return wait;
            }
        }

        private void TrySpawnOne()
        {
            if (CustomerManager.Instance == null || ConstructionManager.Instance == null) return;

            IReadOnlyList<CropConfig> builtCrops = ConstructionManager.Instance.BuiltCropTypes;
            if (builtCrops.Count == 0) return;

            CropConfig requestedCrop = builtCrops[Random.Range(0, builtCrops.Count)];
            int quantity = Random.Range(_minOrderQuantity, _maxOrderQuantity + 1);
            CustomerManager.Instance.TrySpawnCustomer(requestedCrop, quantity);
        }
    }
}
