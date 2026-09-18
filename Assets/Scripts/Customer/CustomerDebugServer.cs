using Farm.Core;
using UnityEngine;

namespace Farm.Customer
{
    /// <summary>Manual test hook: simulates a Phase 4 Worker delivering a crop to whichever waiting customer requested it.</summary>
    public sealed class CustomerDebugServer : MonoBehaviour
    {
        [SerializeField] private KeyCode _deliverKey = KeyCode.J;
        [SerializeField] private CropConfig _testCrop;
        [SerializeField] private long _testPayout = 50;

        private void Update()
        {
            if (!Input.GetKeyDown(_deliverKey)) return;
            if (CustomerManager.Instance == null || _testCrop == null) return;

            BigNumber payout = new BigNumber(_testPayout);

            foreach (Customer customer in CustomerManager.Instance.ActiveCustomers)
            {
                if (customer.TryFulfillOrder(_testCrop, payout))
                {
                    Debug.Log($"[CustomerDebugServer] Delivered {_testCrop.DisplayName} to a waiting customer.", customer);
                    return;
                }
            }

            Debug.Log($"[CustomerDebugServer] No waiting customer currently requests {_testCrop.DisplayName}.");
        }
    }
}
