using Farm.Construction;
using Farm.Core;
using Farm.Money;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace Farm.Customer
{
    /// <summary>A customer: walks to a free dock, waits for its exact requested crop, then leaves once paid.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Customer : MonoBehaviour
    {
        private enum CustomerState { MovingToDock, Waiting, Leaving }

        [SerializeField] private Canvas _orderCanvas;
        [SerializeField] private TMP_Text _orderText;
        [SerializeField] private Animator _animator;
        [SerializeField] private PrefabPool _payEffectPool;

        private NavMeshAgent _agent;
        private CustomerState _state;
        private Transform _exitPoint;

        public CropConfig RequestedCrop { get; private set; }
        public DockSlot AssignedSlot { get; private set; }
        public bool IsClaimed { get; private set; }
        public bool IsWaiting => _state == CustomerState.Waiting;
        public Transform DeliveryPoint => AssignedSlot != null ? AssignedSlot.DeliveryPoint : null;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_orderCanvas == null)
            {
                _orderCanvas = GetComponentInChildren<Canvas>();
                _orderCanvas.worldCamera = Camera.main;
            }
        }

        public void Initialize(CropConfig requestedCrop, DockSlot slot, Transform exitPoint)
        {
            RequestedCrop = requestedCrop;
            AssignedSlot = slot;
            _exitPoint = exitPoint;
            IsClaimed = false;

            if (_orderText != null) _orderText.text = requestedCrop.DisplayName;

            // Pooled agents can go stale relative to their new transform; Warp re-syncs them onto the NavMesh.
            _agent.Warp(transform.position);
            _agent.SetDestination(slot.WaitPoint.position);
            _state = CustomerState.MovingToDock;
            SetMoving(true);
        }

        private void Update()
        {
            if (_state == CustomerState.MovingToDock && HasArrived())
            {
                _state = CustomerState.Waiting;
                SetMoving(false);
            }
            else if (_state == CustomerState.Leaving && HasArrived())
            {
                CustomerManager.Instance.OnCustomerReachedExit(this);
            }
        }

        private bool HasArrived()
        {
            return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance;
        }

        public bool TryClaim()
        {
            if (!IsWaiting || IsClaimed) return false;

            IsClaimed = true;
            return true;
        }

        public void ReleaseClaim()
        {
            IsClaimed = false;
        }

        private void SetMoving(bool moving)
        {
            if (_animator != null) _animator.SetBool("IsMove", moving);
        }

        public bool TryFulfillOrder(CropConfig deliveredCrop, BigNumber payout, ICurrencyService currency)
        {
            if (_state != CustomerState.Waiting || deliveredCrop != RequestedCrop) return false;

            currency.Add(CurrencyType.Cash, payout);

            if (_payEffectPool != null && AssignedSlot != null)
            {
                GameObject effect = _payEffectPool.Rent(AssignedSlot.CurrencyAnchor.position, Quaternion.identity);
                _payEffectPool.Return(effect, 2f);
            }

            _state = CustomerState.Leaving;
            _agent.SetDestination(_exitPoint.position);
            SetMoving(true);
            return true;
        }
    }
}
