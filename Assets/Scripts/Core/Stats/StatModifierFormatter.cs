namespace Farm.Core
{
    /// <summary>Formats a StatModifier into a short display string, e.g. "+5 Worker Count", "+10% Crop Profit", "x2 Crop Profit".</summary>
    public static class StatModifierFormatter
    {
        public static string Describe(StatModifier modifier)
        {
            string statName = modifier.Stat != null ? modifier.Stat.DisplayName : "?";
            string cropSuffix = modifier.CropFilter != null ? $" ({modifier.CropFilter.DisplayName})" : string.Empty;

            switch (modifier.Kind)
            {
                case ModifierKind.Flat:
                    return $"+{modifier.Value:0.##} {statName}{cropSuffix}";
                case ModifierKind.PercentAdd:
                    return $"+{modifier.Value * 100f:0.##}% {statName}{cropSuffix}";
                case ModifierKind.Multiply:
                    return $"x{modifier.Value:0.##} {statName}{cropSuffix}";
                default:
                    return statName;
            }
        }
    }
}
