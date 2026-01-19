using UnityEngine;
using System.IO;
using Chess.Core.Models;

namespace Chess.Unity.Managers
{
    public static class SaveManager
    {
        private static string GetSavePath(GameMode mode)
        {
            return Path.Combine(Application.persistentDataPath, $"save_{mode}.json");
        }

        public static void Save(SaveData data)
        {
            try
            {
                string path = GetSavePath(data.CurrentMode);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
                // Log kalabalığı yapmasın diye commentledim, istersen açabilirsin
                // Debug.Log($"Game Saved ({data.CurrentMode}) to: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Save Failed: {e.Message}");
            }
        }

        public static SaveData Load(GameMode mode)
        {
            string path = GetSavePath(mode);
            
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Load Failed: {e.Message}");
                return null;
            }
        }

        public static void DeleteSave(GameMode mode)
        {
            string path = GetSavePath(mode);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"Save file deleted for mode: {mode}");
            }
        }

        public static bool HasSave(GameMode mode)
        {
            return File.Exists(GetSavePath(mode));
        }
    }
}