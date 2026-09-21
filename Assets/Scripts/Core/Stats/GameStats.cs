namespace Farm.Core
{
    /// <summary>Global, management-level stat sheet. Systems query it live via Evaluate() and react to Changed instead of caching values.</summary>
    public static class GameStats
    {
        public static StatSheet Global { get; } = new StatSheet();
    }
}
