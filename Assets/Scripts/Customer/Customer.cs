using Farm.Core;
using Farm.Money;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Farm.Customer
{
    /// <summary>A customer: walks to a free dock, waits for its exact requested crop, then leaves once paid.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Customer : MonoBehaviour, IOrder
    {
        private enum CustomerState { MovingToDock, Waiting, Leaving }

        private static readonly int IsMoveHash = Animator.StringToHash("IsMove");
        private static readonly int IsCarryHash = Animator.StringToHash("IsCarry");

        [SerializeField] private Canvas _orderCanvas;
        [SerializeField] private Image _imgIcon;
        [SerializeField] private TMP_Text _orderText;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _carryAnchor;
        [SerializeField] private PrefabPool _payEffectPool;

        private CarryVisualController _carryVisuals;

        private NavMeshAgent _agent;
        private CustomerState _state;
        private Transform _exitPoint;

        public CropConfig RequestedCrop { get; private set; }
        public int RequestedQuantity { get; private set; }
        public DockSlot AssignedSlot { get; private set; }
        public bool IsClaimed { get; private set; }
        public bool IsWaiting => _state == CustomerState.Waiting;
        public Transform DeliveryPoint => AssignedSlot != null ? AssignedSlot.DeliveryPoint : null;

        Vector3 IOrder.DeliveryPosition => DeliveryPoint.position;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_orderCanvas == null)
            {
                _orderCanvas = GetComponentInChildren<Canvas>();
                _orderCanvas.worldCamera = Camera.main;
            }

            _carryVisuals = new CarryVisualController(_carryAnchor);
        }

        public void Initialize(CropConfig requestedCrop, int quantity, DockSlot slot, Transform exitPoint)
        {
            RequestedCrop = requestedCrop;
            RequestedQuantity = quantity;
            AssignedSlot = slot;
            _exitPoint = exitPoint;
            IsClaimed = false;
            _carryVisuals.Hide();

            _orderCanvas.enabled = false;
            if (_imgIcon != null) _imgIcon.sprite = requestedCrop.Icon;
            if (_orderText != null) _orderText.text = $"x{quantity}";

            // Pooled agents can go stale relative to their new transform; Warp re-syncs them onto the NavMesh.
            _agent.Warp(transform.position);
            _agent.updateRotation = true;
            _agent.SetDestination(slot.WaitPoint.position);
            _state = CustomerState.MovingToDock;
            SetLocomotion(moving: true, carrying: false);
        }

        private void Update()
        {
            if (_state == CustomerState.MovingToDock && HasArrived())
            {
                _state = CustomerState.Waiting;
                _orderCanvas.enabled = true;
                SetLocomotion(moving: false, carrying: false);

                // Face the dock's authored orientation instead of whatever direction we arrived from.
                _agent.updateRotation = false;
                transform.rotation = AssignedSlot.WaitPoint.rotation;
            }
            else if (_state == CustomerState.Leaving && HasArrived())
            {
                CustomerManager.Instance.OnCustomerReachedExit(this);
                _orderCanvas.enabled = false;
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

        private void SetLocomotion(bool moving, bool carrying)
        {
            if (_animator == null) return;

            _animator.SetBool(IsMoveHash, moving);
            _animator.SetBool(IsCarryHash, carrying);
        }

        public bool TryFulfillOrder(CropConfig deliveredCrop, GameObject productPrefab, BigNumber payout)
        {
            if (_state != CustomerState.Waiting || deliveredCrop != RequestedCrop) return false;

            MoneyManager.Instance.Currency.Add(CurrencyType.Cash, payout);

            if (_payEffectPool != null && AssignedSlot != null)
            {
                GameObject effect = _payEffectPool.Rent(AssignedSlot.CurrencyAnchor.position, Quaternion.identity);
                _payEffectPool.Return(effect, 2f);
            }

            _carryVisuals.Show(productPrefab, RequestedQuantity);
            _state = CustomerState.Leaving;
            _agent.updateRotation = true;
            _agent.SetDestination(_exitPoint.position);
            SetLocomotion(moving: true, carrying: true);
            return true;
        }

        bool IOrder.Fulfill(CropConfig deliveredCrop, GameObject productPrefab, BigNumber payout) => TryFulfillOrder(deliveredCrop, productPrefab, payout);
    }
}
