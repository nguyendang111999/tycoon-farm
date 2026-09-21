using System;
using System.Collections.Generic;

namespace Farm.Core
{
    /// <summary>
    /// Aggregates modifiers from any number of sources for any number of stats. A stat's final value is
    /// (base + sum(Flat)) * (1 + sum(PercentAdd)) * product(Multiply). Crop-filtered modifiers only apply
    /// when the crop passed to Evaluate matches (or the modifier has no filter). Adding a new stat or a new
    /// modifier source never requires touching this class - only new StatDefinition assets / StatModifier data.
    /// </summary>
    public sealed class StatSheet
    {
        private readonly struct Entry
        {
            public readonly StatModifier Modifier;
            public readonly object Source;

            public Entry(StatModifier modifier, object source)
            {
                Modifier = modifier;
                Source = source;
            }
        }

        private readonly Dictionary<StatDefinition, List<Entry>> _entriesByStat = new Dictionary<StatDefinition, List<Entry>>();

        /// <summary>Fired with the affected stat whenever a modifier is added or removed.</summary>
        public event Action<StatDefinition> Changed;

        public void AddModifier(StatModifier modifier, object source)
        {
            if (modifier.Stat == null) return;

            if (!_entriesByStat.TryGetValue(modifier.Stat, out List<Entry> entries))
            {
                entries = new List<Entry>();
                _entriesByStat[modifier.Stat] = entries;
            }

            entries.Add(new Entry(modifier, source));
            Changed?.Invoke(modifier.Stat);
        }

        /// <summary>Removes every modifier previously added with this exact source reference (e.g. an upgrade id, a plot's level-source token).</summary>
        public void RemoveModifiersFrom(object source)
        {
            if (source == null) return;

            foreach (KeyValuePair<StatDefinition, List<Entry>> pair in _entriesByStat)
            {
                int removed = pair.Value.RemoveAll(entry => Equals(entry.Source, source));
                if (removed > 0) Changed?.Invoke(pair.Key);
            }
        }

        public void Clear()
        {
            if (_entriesByStat.Count == 0) return;

            var affectedStats = new List<StatDefinition>(_entriesByStat.Keys);
            _entriesByStat.Clear();
            foreach (StatDefinition stat in affectedStats) Changed?.Invoke(stat);
        }

        public float Evaluate(StatDefinition stat, float baseValue, CropConfig crop = null)
        {
            if (stat == null || !_entriesByStat.TryGetValue(stat, out List<Entry> entries) || entries.Count == 0)
            {
                return baseValue;
            }

            float flatSum = 0f;
            float percentSum = 0f;
            float multiplyProduct = 1f;

            foreach (Entry entry in entries)
            {
                if (!entry.Modifier.AppliesTo(crop)) continue;

                switch (entry.Modifier.Kind)
                {
                    case ModifierKind.Flat:
                        flatSum += entry.Modifier.Value;
                        break;
                    case ModifierKind.PercentAdd:
                        percentSum += entry.Modifier.Value;
                        break;
                    case ModifierKind.Multiply:
                        multiplyProduct *= entry.Modifier.Value;
                        break;
                }
            }

            return (baseValue + flatSum) * (1f + percentSum) * multiplyProduct;
        }
    }
}
