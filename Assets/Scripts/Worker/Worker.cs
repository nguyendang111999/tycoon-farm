using System.Collections.Generic;
using Farm.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Farm.Worker
{
    /// <summary>A delivery worker: collects from a reserved supplier and carries the payout to whichever order claimed it, then returns home.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Worker : MonoBehaviour
    {
        private enum WorkerState { Idle, ToSupplier, ToOrder, Returning }

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _carryAnchor;

        private readonly Dictionary<GameObject, GameObject> _carriedVisuals = new Dictionary<GameObject, GameObject>();

        private NavMeshAgent _agent;
        private WorkerState _state = WorkerState.Idle;
        private Transform _home;
        private ISupplier _supplier;
        private IOrder _order;
        private BigNumber _payout;
        private GameObject _activeVisual;

        public bool IsIdle => _state == WorkerState.Idle;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        public void Initialize(Transform home)
        {
            _home = home;
            _agent.Warp(home.position);
            _state = WorkerState.Idle;
        }

        public void AssignJob(ISupplier supplier, IOrder order)
        {
            _supplier = supplier;
            _order = order;

            _agent.SetDestination(supplier.PickupPosition);
            _state = WorkerState.ToSupplier;
            SetLocomotion(moving: true, carrying: false);
        }

        private void Update()
        {
            switch (_state)
            {
                case WorkerState.ToSupplier:
                    if (HasArrived()) HandleArrivedAtSupplier();
                    break;

                case WorkerState.ToOrder:
                    if (HasArrived()) HandleArrivedAtOrder();
                    break;

                case WorkerState.Returning:
                    if (HasArrived()) _state = WorkerState.Idle;
                    break;
            }
        }

        private void HandleArrivedAtSupplier()
        {
            if (_supplier.TryCollect(out _payout))
            {
                ShowCarriedVisual(_supplier.ProductPrefab);
                _agent.SetDestination(_order.DeliveryPosition);
                _state = WorkerState.ToOrder;
                SetLocomotion(moving: true, carrying: true);
            }
            else
            {
                // Stock disappeared out from under the reservation; abandon the job cleanly.
                _order.ReleaseClaim();
                ReturnHome();
            }
        }

        private void HandleArrivedAtOrder()
        {
            _order.Fulfill(_supplier.Crop, _payout);
            HideCarriedVisual();
            ReturnHome();
        }

        private void ReturnHome()
        {
            _supplier = null;
            _order = null;
            _agent.SetDestination(_home.position);
            _state = WorkerState.Returning;
            SetLocomotion(moving: true, carrying: false);
        }

        private bool HasArrived()
        {
            return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance;
        }

        private void SetLocomotion(bool moving, bool carrying)
        {
            if (_animator == null) return;

            _animator.SetBool("IsMove", moving && !carrying);
            _animator.SetBool("IsCarryMove", moving && carrying);
            _animator.SetBool("IsEmpty", !carrying);
        }

        private void ShowCarriedVisual(GameObject productPrefab)
        {
            if (productPrefab == null || _carryAnchor == null) return;

            if (!_carriedVisuals.TryGetValue(productPrefab, out GameObject visual))
            {
                visual = Instantiate(productPrefab, _carryAnchor);
                visual.transform.localPosition = Vector3.zero;
                visual.SetActive(false);
                _carriedVisuals[productPrefab] = visual;
            }

            _activeVisual = visual;
            _activeVisual.SetActive(true);
        }

        private void HideCarriedVisual()
        {
            if (_activeVisual != null) _activeVisual.SetActive(false);
            _activeVisual = null;
        }
    }
}
