namespace Farm.Core
{
    /// <summary>Implemented by any system whose state must survive between play sessions.</summary>
    public interface ISaveable
    {
        string SaveKey { get; }
        string CaptureState();
        void RestoreState(string json);
    }
}
