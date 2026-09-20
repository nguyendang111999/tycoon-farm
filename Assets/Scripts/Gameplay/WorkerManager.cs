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
            EnsureWorkerCount(_workerCount + ManagementBonuses.Current.BonusWorkers);
            StartCoroutine(MatchLoop());
        }

        private void OnEnable()
        {
            ManagementBonuses.Current.Changed += HandleBonusesChanged;
        }

        private void OnDisable()
        {
            ManagementBonuses.Current.Changed -= HandleBonusesChanged;
        }

        private void OnDestroy()
        {
            foreach (Worker.Worker worker in _workers)
            {
                if (worker != null)
                {
                    worker.OrderDelivered -= HandleWorkerOrderDelivered;
                }
            }
        }

        private void HandleBonusesChanged()
        {
            EnsureWorkerCount(_workerCount + ManagementBonuses.Current.BonusWorkers);
        }

        /// <summary>Spawns workers until the pool reaches targetCount; never removes existing workers.</summary>
        public void EnsureWorkerCount(int targetCount)
        {
            while (_workers.Count < targetCount)
            {
                SpawnWorker();
            }
        }

        private void SpawnWorker()
        {
            int index = _workers.Count;
            Transform home = _market.GetDeliveryHome(index);
            GameObject instance = _workerPool.Rent(home.position, home.rotation);
            var worker = instance.GetComponent<Worker.Worker>();
            worker.Initialize(home);
            worker.OrderDelivered -= HandleWorkerOrderDelivered;
            worker.OrderDelivered += HandleWorkerOrderDelivered;
            _workers.Add(worker);
        }

        private void HandleWorkerOrderDelivered(Worker.Worker worker)
        {
            if (worker == null) return;

            // Immediately assign new job if available so worker doesn't need to return home
            if (TryFindJob(out ISupplier supplier, out IOrder order))
            {
                worker.AssignJob(supplier, order);
                return;
            }

            // Otherwise, pick an unoccupied resting spot so returning workers don't crowd the same spot
            Transform homeSpot = GetAvailableHomeSpot(worker);
            worker.SetHome(homeSpot);
        }

        private IEnumerator MatchLoop()
        {
            var wait = new WaitForSeconds(_matchInterval);
            while (true)
            {
                foreach (Worker.Worker worker in _workers)
                {
                    if (!worker.CanAcceptJob) continue;
                    if (!TryFindJob(out ISupplier supplier, out IOrder order)) break;

                    worker.AssignJob(supplier, order);
                }

                yield return wait;
            }
        }

        private Transform GetAvailableHomeSpot(Worker.Worker worker)
        {
            IReadOnlyList<Transform> homes = _market.DeliveryHomes;
            if (homes == null || homes.Count == 0)
            {
                return _market.DeliveryHome != null ? _market.DeliveryHome : transform;
            }

            // Prefer an unoccupied home spot not targeted or occupied by another worker
            foreach (Transform spot in homes)
            {
                if (spot == null) continue;
                if (!IsSpotClaimedByOther(spot, worker))
                {
                    return spot;
                }
            }

            // Fallback: pick the spot with the fewest workers targeting it
            Transform bestSpot = homes[0];
            int minOccupants = int.MaxValue;
            foreach (Transform spot in homes)
            {
                if (spot == null) continue;
                int count = CountWorkersTargetingSpot(spot, worker);
                if (count < minOccupants)
                {
                    minOccupants = count;
                    bestSpot = spot;
                }
            }

            return bestSpot != null ? bestSpot : (_market.DeliveryHome != null ? _market.DeliveryHome : transform);
        }

        private bool IsSpotClaimedByOther(Transform spot, Worker.Worker currentWorker)
        {
            foreach (Worker.Worker other in _workers)
            {
                if (other == null || other == currentWorker) continue;
                if ((other.IsIdle || other.IsReturning) && other.Home == spot)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountWorkersTargetingSpot(Transform spot, Worker.Worker currentWorker)
        {
            int count = 0;
            foreach (Worker.Worker other in _workers)
            {
                if (other == null || other == currentWorker) continue;
                if ((other.IsIdle || other.IsReturning) && other.Home == spot)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryFindJob(out ISupplier supplier, out IOrder order)
        {
            supplier = null;
            order = null;

            if (CustomerManager.Instance == null || ConstructionManager.Instance == null) return false;

            foreach (Customer.Customer candidate in CustomerManager.Instance.ActiveCustomers)
            {
                if (!candidate.IsWaiting || candidate.IsClaimed) continue;

                Construction.CropPlot match = ConstructionManager.Instance.FindAvailableConstruction(candidate.RequestedCrop, 1);
                if (match == null || !match.TryClaim()) continue;

                if (!candidate.TryClaim())
                {
                    match.ReleaseClaim();
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

