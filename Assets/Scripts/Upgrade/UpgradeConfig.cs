using System.Collections.Generic;
using UnityEngine;

namespace Farm.Upgrade
{
    /// <summary>Design data for every management upgrade offered in the game.</summary>
    [CreateAssetMenu(menuName = "Farm/Upgrade/Upgrade Config", fileName = "UpgradeConfig")]
    public sealed class UpgradeConfig : ScriptableObject
    {
        [SerializeField] private UpgradeEntry[] _entries = new UpgradeEntry[0];

        public IReadOnlyList<UpgradeEntry> Entries => _entries;
    }
}
