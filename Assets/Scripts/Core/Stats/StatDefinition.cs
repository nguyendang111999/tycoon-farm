using UnityEngine;

namespace Farm.Core
{
    /// <summary>Identity key for a stat (e.g. CropProfit, WorkerCount). Create one asset per stat and reference it directly instead of a hardcoded enum, so adding a stat never requires a code change.</summary>
    [CreateAssetMenu(menuName = "Farm/Core/Stat Definition", fileName = "StatDefinition")]
    public sealed class StatDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Stat";

        public string DisplayName => _displayName;
    }
}
