using Farm.Core;
using Farm.Money;
using UnityEngine;

namespace Farm.Construction
{
    /// <summary>Manual test helper: press a key to harvest+sell one unit before Worker/Customer systems exist.</summary>
    public sealed class ConstructionDebugHarvester : MonoBehaviour
    {
        [SerializeField] private CropPlot _target;
        [SerializeField] private KeyCode _harvestKey = KeyCode.H;

        private void Update()
        {
            if (_target == null || MoneyManager.Instance == null) return;
            if (!Input.GetKeyDown(_harvestKey)) return;

            if (_target.TryHarvestOne(out BigNumber payout))
            {
                MoneyManager.Instance.Currency.Add(CurrencyType.Cash, payout);
                Debug.Log($"[ConstructionDebugHarvester] Harvested for {NumberFormatter.Format(payout)}.");
            }
            else
            {
                Debug.Log("[ConstructionDebugHarvester] Nothing to harvest yet.");
            }
        }
    }
}
