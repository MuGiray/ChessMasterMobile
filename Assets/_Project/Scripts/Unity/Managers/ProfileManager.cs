using UnityEngine;
using System.IO;
using Chess.Core.Models;

namespace Chess.Unity.Managers
{
    public static class ProfileManager
    {
        private static UserProfile _currentProfile;
        private static string SavePath => Path.Combine(Application.persistentDataPath, "user_profile.json");

        public static UserProfile GetProfile()
        {
            if (_currentProfile == null) LoadProfile();
            return _currentProfile;
        }

        public static void LoadProfile()
        {
            if (File.Exists(SavePath))
            {
                try 
                {
                    string json = File.ReadAllText(SavePath);
                    _currentProfile = JsonUtility.FromJson<UserProfile>(json);
                }
                catch 
                { 
                    _currentProfile = new UserProfile(); 
                }
            }
            else
            {
                _currentProfile = new UserProfile();
                SaveProfile();
            }
        }

        public static void SaveProfile()
        {
            if (_currentProfile == null) return;
            string json = JsonUtility.ToJson(_currentProfile, true);
            File.WriteAllText(SavePath, json);
        }

        // --- ELO SİSTEMİ ---
        // Basitleştirilmiş ELO hesabı.
        // result: 1 (Win), 0 (Draw), -1 (Loss)
        public static void UpdateStats(int result, int aiDifficultyLevel)
        {
            if (_currentProfile == null) LoadProfile();

            _currentProfile.MatchesPlayed++;

            // K Değeri: Puanın ne kadar hızlı değişeceği (30 standarttır)
            int K = 30; 
            int change = 0;

            if (result == 1) // KAZANDI
            {
                _currentProfile.Wins++;
                // Zor AI yendiyse daha çok puan, kolay AI yendiyse az puan
                change = K + (aiDifficultyLevel * 5); 
            }
            else if (result == -1) // KAYBETTİ
            {
                _currentProfile.Losses++;
                // Zor AI'ya yenildiyse az puan düşer, kolaya yenildiyse çok düşer
                change = -(K - (aiDifficultyLevel * 5));
            }
            else // BERABERE
            {
                _currentProfile.Draws++;
                // Beraberlikte hafif artış (Teşvik)
                change = 2; 
            }

            _currentProfile.ELO += change;
            
            // ELO 0'ın altına düşmesin
            if (_currentProfile.ELO < 100) _currentProfile.ELO = 100;

            Debug.Log($"Stats Updated. Result: {result}, New ELO: {_currentProfile.ELO}");
            SaveProfile();
        }
    }
}