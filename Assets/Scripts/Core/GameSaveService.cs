namespace Farm.Core
{
    /// <summary>
    /// One shared save file for every independent system. Each system registers itself during its own
    /// bootstrap (Awake) and calls <see cref="LoadOnce"/> from Start(), so registration order never matters
    /// because Unity runs all Awake calls before any Start call.
    /// </summary>
    public static class GameSaveService
    {
        private static readonly SaveManager Manager = new SaveManager();
        private static bool _loaded;

        public static void Register(ISaveable saveable) => Manager.Register(saveable);

        public static void Unregister(ISaveable saveable) => Manager.Unregister(saveable);

        public static void LoadOnce()
        {
            if (_loaded) return;
            _loaded = true;
            Manager.Load();
        }

        public static void Save() => Manager.Save();
    }
}
