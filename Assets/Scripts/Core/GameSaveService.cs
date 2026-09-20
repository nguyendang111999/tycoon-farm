using UnityEngine;

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

        public static string SaveFilePath => Manager.FilePath;

        public static void DeleteSaveFile() => Manager.DeleteSaveFile();

        public static void WipeAllData() => Manager.WipeAllData();

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Farm/Save Data/Wipe All Save Data", priority = 100)]
        public static void EditorWipeAllData()
        {
            WipeAllData();
            Debug.Log("[GameSaveService] Save data wiped clean. Ready to play as new player.");
        }

        [UnityEditor.MenuItem("Farm/Save Data/Open Save Folder", priority = 101)]
        public static void EditorOpenSaveFolder()
        {
            string dir = System.IO.Path.GetDirectoryName(SaveFilePath);
            if (System.IO.Directory.Exists(dir))
            {
                UnityEditor.EditorUtility.RevealInFinder(SaveFilePath);
            }
            else
            {
                Debug.LogWarning($"[GameSaveService] Save folder does not exist yet: {dir}");
            }
        }
#endif
    }
}
