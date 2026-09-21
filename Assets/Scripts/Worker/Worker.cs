using Farm.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Farm.Worker
{
    /// <summary>A delivery worker: collects from a reserved supplier and carries the payout to whichever order claimed it, then returns home.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Worker : MonoBehaviour
    {
        private sealed class IdleState : IState<Worker>
        {
            public void Enter(Worker w)
            {
                w._agent.ResetPath();
                if (w._home != null) w.transform.rotation = w._home.rotation;
                w.SetLocomotion(moving: false, carrying: false);
            }

            public void Tick(Worker w) { }
            public void Exit(Worker w) { }
        }

        private sealed class ToSupplierState : IState<Worker>
        {
            public void Enter(Worker w)
            {
                if (w._supplier != null) w._agent.SetDestination(w._supplier.PickupPosition);
                w.SetLocomotion(moving: true, carrying: false);
            }

            public void Tick(Worker w)
            {
                if (w.HasArrived()) w._fsm.ChangeState(w._collectingState);
            }

            public void Exit(Worker w) { }
        }

        private sealed class CollectingState : IState<Worker>
        {
            public void Enter(Worker w)
            {
                w.CollectAvailableStock();
            }

            public void Tick(Worker w)
            {
                w.CollectAvailableStock();
            }

            public void Exit(Worker w) { }
        }

        private sealed class ToOrderState : IState<Worker>
        {
            public void Enter(Worker w)
            {
                if (w._order != null) w._agent.SetDestination(w._order.DeliveryPosition);
                w.SetLocomotion(moving: true, carrying: true);
            }

            public void Tick(Worker w)
            {
                if (w.HasArrived()) w.HandleArrivedAtOrder();
            }

            public void Exit(Worker w) { }
        }

        private sealed class ReturningState : IState<Worker>
        {
            public void Enter(Worker w)
            {
                if (w._home != null) w._agent.SetDestination(w._home.position);
                w.SetLocomotion(moving: true, carrying: false);
            }

            public void Tick(Worker w)
            {
                if (w.HasArrived()) w._fsm.ChangeState(w._idleState);
            }

            public void Exit(Worker w) { }
        }

        private static readonly int IsMoveHash = Animator.StringToHash("IsMove");
        private static readonly int IsCarryHash = Animator.StringToHash("IsCarry");

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _carryAnchor;

        private readonly IdleState _idleState = new IdleState();
        private readonly ToSupplierState _toSupplierState = new ToSupplierState();
        private readonly CollectingState _collectingState = new CollectingState();
        private readonly ToOrderState _toOrderState = new ToOrderState();
        private readonly ReturningState _returningState = new ReturningState();

        private StateMachine<Worker> _fsm;
        private CarryVisualController _carryVisuals;
        private NavMeshAgent _agent;
        private Transform _home;
        private ISupplier _supplier;
        private IOrder _order;
        private BigNumber _payout;
        private int _collectedCount;

        public event System.Action<Worker> OrderDelivered;

        public bool IsIdle => _fsm != null && _fsm.CurrentState == _idleState;
        public bool IsReturning => _fsm != null && _fsm.CurrentState == _returningState;
        public bool CanAcceptJob => IsIdle || IsReturning;
        public Transform Home => _home;

        public void SetHome(Transform home)
        {
            _home = home;
        }

        public void SetMoveSpeed(float speed)
        {
            if (_agent != null && speed > 0f) _agent.speed = speed;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent != null && _agent.stoppingDistance < 0.25f) _agent.stoppingDistance = 0.25f;
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            _carryVisuals = new CarryVisualController(_carryAnchor);
            _fsm = new StateMachine<Worker>(this);
        }

        public void Initialize(Transform home)
        {
            _home = home;
            _agent.Warp(home.position);
            _collectedCount = 0;
            _payout = BigNumber.Zero;
            _carryVisuals.Hide();
            _fsm.ChangeState(_idleState);
        }

        public void AssignJob(ISupplier supplier, IOrder order)
        {
            _supplier = supplier;
            _order = order;
            _collectedCount = 0;
            _payout = BigNumber.Zero;
            _carryVisuals.Hide();

            _fsm.ChangeState(_toSupplierState);
        }

        private void Update()
        {
            _fsm.Tick();
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

            _fsm.ChangeState(_toOrderState);
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

            if (_fsm.CurrentState == _toSupplierState) return;

            _fsm.ChangeState(_returningState);
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

            _fsm.ChangeState(_returningState);
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
