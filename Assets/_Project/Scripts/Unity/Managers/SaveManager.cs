using UnityEngine;
using System.IO;
using Chess.Core.Models;

namespace Chess.Unity.Managers
{
    public static class SaveManager
    {
        // Dosya yolu: Android/iOS kalıcı veri klasörü
        private static string SavePath => Path.Combine(Application.persistentDataPath, "chess_autosave.json");

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"Game Saved to: {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Save Failed: {e.Message}");
            }
        }

        public static SaveData Load()
        {
            if (!File.Exists(SavePath)) return null;

            try
            {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Load Failed: {e.Message}");
                return null;
            }
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("Save file deleted.");
            }
        }

        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }
    }
}