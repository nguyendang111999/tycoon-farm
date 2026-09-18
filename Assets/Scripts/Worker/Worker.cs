using System.Collections.Generic;
using Farm.Core;
using Farm.Money;
using Farm.Construction;
using Farm.Customer;
using UnityEngine;
using UnityEngine.AI;

namespace Farm.Worker
{
    /// <summary>A delivery worker: harvests a reserved crop and carries the payout to the customer who claimed it, then returns home.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Worker : MonoBehaviour
    {
        private enum WorkerState { Idle, ToConstruction, ToCustomer, Returning }

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _carryAnchor;

        private readonly Dictionary<GameObject, GameObject> _carriedVisuals = new Dictionary<GameObject, GameObject>();

        private NavMeshAgent _agent;
        private WorkerState _state = WorkerState.Idle;
        private Transform _home;
        private ICurrencyService _currency;
        private Construction.Construction _construction;
        private Customer.Customer _customer;
        private BigNumber _payout;
        private GameObject _activeVisual;

        public bool IsIdle => _state == WorkerState.Idle;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        public void Initialize(Transform home, ICurrencyService currency)
        {
            _home = home;
            _currency = currency;
            _agent.Warp(home.position);
            _state = WorkerState.Idle;
        }

        public void AssignJob(Construction.Construction construction, Customer.Customer customer)
        {
            _construction = construction;
            _customer = customer;

            _agent.SetDestination(construction.transform.position);
            _state = WorkerState.ToConstruction;
            SetLocomotion(moving: true, carrying: false);
        }

        private void Update()
        {
            switch (_state)
            {
                case WorkerState.ToConstruction:
                    if (HasArrived()) HandleArrivedAtConstruction();
                    break;

                case WorkerState.ToCustomer:
                    if (HasArrived()) HandleArrivedAtCustomer();
                    break;

                case WorkerState.Returning:
                    if (HasArrived()) _state = WorkerState.Idle;
                    break;
            }
        }

        private void HandleArrivedAtConstruction()
        {
            if (_construction.TryCollectReserved(out _payout))
            {
                ShowCarriedVisual(_construction.ProductPrefab);
                _agent.SetDestination(_customer.DeliveryPoint.position);
                _state = WorkerState.ToCustomer;
                SetLocomotion(moving: true, carrying: true);
            }
            else
            {
                // Stock disappeared out from under the reservation; abandon the job cleanly.
                _customer.ReleaseClaim();
                ReturnHome();
            }
        }

        private void HandleArrivedAtCustomer()
        {
            _customer.TryFulfillOrder(_construction.Config, _payout, _currency);
            HideCarriedVisual();
            ReturnHome();
        }

        private void ReturnHome()
        {
            _construction = null;
            _customer = null;
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
