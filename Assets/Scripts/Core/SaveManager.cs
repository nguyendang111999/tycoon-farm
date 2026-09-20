using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Farm.Core
{
    /// <summary>Persists every registered <see cref="ISaveable"/> system into one JSON file on disk.</summary>
    public sealed class SaveManager
    {
        private readonly string _filePath;
        private readonly List<ISaveable> _saveables = new List<ISaveable>();

        public SaveManager(string fileName = "save.json")
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public string FilePath => _filePath;

        public void Register(ISaveable saveable)
        {
            if (!_saveables.Contains(saveable))
            {
                _saveables.Add(saveable);
            }
        }

        public void Unregister(ISaveable saveable) => _saveables.Remove(saveable);

        public void DeleteSaveFile()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                    Debug.Log($"[SaveManager] Save file deleted: {_filePath}");
                }
            }
            catch (IOException exception)
            {
                Debug.LogError($"[SaveManager] Failed to delete save file: {exception.Message}");
            }
        }

        public void WipeAllData()
        {
            DeleteSaveFile();
            foreach (ISaveable saveable in _saveables)
            {
                saveable.RestoreState(null);
            }

            Debug.Log("[SaveManager] All registered systems reset to initial/empty state.");
        }

        public void Save()
        {
            var root = new SaveRoot();
            foreach (ISaveable saveable in _saveables)
            {
                root.entries.Add(new SaveEntry { key = saveable.SaveKey, json = saveable.CaptureState() });
            }

            try
            {
                File.WriteAllText(_filePath, JsonUtility.ToJson(root, true));
            }
            catch (IOException exception)
            {
                Debug.LogError($"[SaveManager] Failed to write save file: {exception.Message}");
            }
        }

        public void Load()
        {
            if (!File.Exists(_filePath))
            {
                foreach (ISaveable saveable in _saveables)
                {
                    saveable.RestoreState(null);
                }

                return;
            }

            SaveRoot root;
            try
            {
                root = JsonUtility.FromJson<SaveRoot>(File.ReadAllText(_filePath));
            }
            catch (IOException exception)
            {
                Debug.LogError($"[SaveManager] Failed to read save file: {exception.Message}");
                return;
            }

            var stateByKey = new Dictionary<string, string>();
            foreach (SaveEntry entry in root.entries)
            {
                stateByKey[entry.key] = entry.json;
            }

            foreach (ISaveable saveable in _saveables)
            {
                stateByKey.TryGetValue(saveable.SaveKey, out string json);
                saveable.RestoreState(json);
            }
        }

        [Serializable]
        private sealed class SaveRoot
        {
            public List<SaveEntry> entries = new List<SaveEntry>();
        }

        [Serializable]
        private sealed class SaveEntry
        {
            public string key;
            public string json;
        }
    }
}
