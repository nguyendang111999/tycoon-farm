using System.Collections;
using System.Collections.Generic;
using Farm.Construction;
using Farm.Core;
using Farm.Customer;
using Farm.Worker;
using UnityEngine;

namespace Farm.Gameplay
{
    /// <summary>Composition root: owns the worker pool and matches idle workers to a waiting, unclaimed customer with reservable stock.</summary>
    public sealed class WorkerManager : MonoBehaviour
    {
        public static WorkerManager Instance { get; private set; }

        [SerializeField] private Market _market;
        [SerializeField] private PrefabPool _workerPool;
        [SerializeField] private int _workerCount = 1;
        [SerializeField] private float _matchInterval = 0.5f;

        private readonly List<Worker.Worker> _workers = new List<Worker.Worker>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            for (int i = 0; i < _workerCount; i++)
            {
                SpawnWorker();
            }

            StartCoroutine(MatchLoop());
        }

        public void AddWorkers(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnWorker();
            }
        }

        private void SpawnWorker()
        {
            GameObject instance = _workerPool.Rent(_market.DeliveryHome.position, _market.DeliveryHome.rotation);
            var worker = instance.GetComponent<Worker.Worker>();
            worker.Initialize(_market.DeliveryHome);
            _workers.Add(worker);
        }

        private IEnumerator MatchLoop()
        {
            var wait = new WaitForSeconds(_matchInterval);
            while (true)
            {
                foreach (Worker.Worker worker in _workers)
                {
                    if (!worker.IsIdle) continue;
                    if (!TryFindJob(out ISupplier supplier, out IOrder order)) break;

                    worker.AssignJob(supplier, order);
                }

                yield return wait;
            }
        }

        private static bool TryFindJob(out ISupplier supplier, out IOrder order)
        {
            supplier = null;
            order = null;

            if (CustomerManager.Instance == null || ConstructionManager.Instance == null) return false;

            foreach (Customer.Customer candidate in CustomerManager.Instance.ActiveCustomers)
            {
                if (!candidate.IsWaiting || candidate.IsClaimed) continue;

                Construction.Construction match = ConstructionManager.Instance.FindAvailableConstruction(candidate.RequestedCrop);
                if (match == null || !match.TryReserveStock()) continue;

                if (!candidate.TryClaim())
                {
                    match.ReleaseReservation();
                    continue;
                }

                supplier = match;
                order = candidate;
                return true;
            }

            return false;
        }
    }
}

