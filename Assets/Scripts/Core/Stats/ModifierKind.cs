namespace Farm.Core
{
    /// <summary>How a modifier's Value combines into a stat's final result.</summary>
    public enum ModifierKind
    {
        /// <summary>Added directly to the base value before percentages/multipliers apply.</summary>
        Flat,
        /// <summary>Summed with every other PercentAdd modifier into one +% bucket (e.g. +10% and +20% = +30%, not +10% then +20% compounded).</summary>
        PercentAdd,
        /// <summary>Stacks multiplicatively with every other Multiply modifier.</summary>
        Multiply
    }
}
