using Farm.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Farm.Worker
{
    /// <summary>A delivery worker: collects from a reserved supplier and carries the payout to whichever order claimed it, then returns home.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Worker : MonoBehaviour
    {
        private enum WorkerState { Idle, ToSupplier, Collecting, ToOrder, Returning }

        private static readonly int IsMoveHash = Animator.StringToHash("IsMove");
        private static readonly int IsCarryHash = Animator.StringToHash("IsCarry");

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _carryAnchor;

        private CarryVisualController _carryVisuals;

        private NavMeshAgent _agent;
        private WorkerState _state = WorkerState.Idle;
        private Transform _home;
        private ISupplier _supplier;
        private IOrder _order;
        private BigNumber _payout;
        private int _collectedCount;

        public event System.Action<Worker> OrderDelivered;

        public bool IsIdle => _state == WorkerState.Idle;
        public bool IsReturning => _state == WorkerState.Returning;
        public bool CanAcceptJob => _state == WorkerState.Idle || _state == WorkerState.Returning;
        public Transform Home => _home;

        public void SetHome(Transform home)
        {
            _home = home;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            _carryVisuals = new CarryVisualController(_carryAnchor);
        }

        public void Initialize(Transform home)
        {
            _home = home;
            _agent.Warp(home.position);
            _state = WorkerState.Idle;
            _collectedCount = 0;
            _payout = BigNumber.Zero;
            _carryVisuals.Hide();
            SetLocomotion(moving: false, carrying: false);
        }

        public void AssignJob(ISupplier supplier, IOrder order)
        {
            _supplier = supplier;
            _order = order;
            _collectedCount = 0;
            _payout = BigNumber.Zero;
            _carryVisuals.Hide();

            _agent.SetDestination(supplier.PickupPosition);
            _state = WorkerState.ToSupplier;
            SetLocomotion(moving: true, carrying: false);
        }

        private void Update()
        {
            switch (_state)
            {
                case WorkerState.ToSupplier:
                    if (HasArrived())
                    {
                        _state = WorkerState.Collecting;
                        CollectAvailableStock();
                    }
                    break;

                case WorkerState.Collecting:
                    CollectAvailableStock();
                    break;

                case WorkerState.ToOrder:
                    if (HasArrived()) HandleArrivedAtOrder();
                    break;

                case WorkerState.Returning:
                    if (HasArrived())
                    {
                        _state = WorkerState.Idle;
                        _agent.ResetPath();
                        if (_home != null) transform.rotation = _home.rotation;
                        SetLocomotion(moving: false, carrying: false);
                    }
                    break;
            }
        }

        private void CollectAvailableStock()
        {
            if (_supplier == null || _order == null)
            {
                ReturnHome();
                return;
            }

            int needed = _order.RequestedQuantity - _collectedCount;
            if (needed <= 0)
            {
                FinishCollectingAndHeadToOrder();
                return;
            }

            int canTake = Mathf.Min(needed, _supplier.AvailableStock);
            if (canTake > 0 && _supplier.TryCollect(canTake, out BigNumber batchPayout))
            {
                _collectedCount += canTake;
                _payout = _payout + batchPayout;
                _carryVisuals.Show(_supplier.ProductPrefab, _collectedCount);
            }

            if (_collectedCount >= _order.RequestedQuantity)
            {
                FinishCollectingAndHeadToOrder();
            }
            else
            {
                SetLocomotion(moving: false, carrying: _collectedCount > 0);
            }
        }

        private void FinishCollectingAndHeadToOrder()
        {
            if (_supplier != null && _supplier.IsClaimed)
            {
                _supplier.ReleaseClaim();
            }

            _agent.SetDestination(_order.DeliveryPosition);
            _state = WorkerState.ToOrder;
            SetLocomotion(moving: true, carrying: true);
        }

        private void HandleArrivedAtOrder()
        {
            if (_order != null && _supplier != null)
            {
                _order.Fulfill(_supplier.Crop, _supplier.ProductPrefab, _payout);
            }

            _supplier = null;
            _order = null;
            _collectedCount = 0;
            _payout = BigNumber.Zero;
            _carryVisuals.Hide();

            OrderDelivered?.Invoke(this);

            if (_state == WorkerState.ToSupplier) return;

            if (_home != null)
            {
                _agent.SetDestination(_home.position);
            }

            _state = WorkerState.Returning;
            SetLocomotion(moving: true, carrying: false);
        }

        private void ReturnHome()
        {
            if (_supplier != null && _supplier.IsClaimed)
            {
                _supplier.ReleaseClaim();
            }

            if (_order != null)
            {
                _order.ReleaseClaim();
            }

            _supplier = null;
            _order = null;
            _collectedCount = 0;
            _payout = BigNumber.Zero;
            _carryVisuals.Hide();

            if (_home != null)
            {
                _agent.SetDestination(_home.position);
            }

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

            _animator.SetBool(IsMoveHash, moving);
            _animator.SetBool(IsCarryHash, carrying);
        }
    }
}
