using UnityEngine;

namespace Chess.Core.Models
{
    // Eğer GameMode başka bir dosyada tanımlıysa burayı silebilirsin, 
    // ama "bulunamadı" hatası aldığın için buraya eklemek en güvenlisi.
    public enum GameMode
    {
        HumanVsHuman,
        HumanVsAI
    }

    public static class GameSettings
    {
        // Mevcut Veri
        public static GameMode CurrentMode = GameMode.HumanVsAI;

        // --- KALICI AYARLAR (PLAYER PREFS) ---

        // Müzik (0: Kapalı, 1: Açık - Varsayılan: 1)
        public static bool MusicEnabled
        {
            get => PlayerPrefs.GetInt("Settings_Music", 1) == 1;
            set
            {
                PlayerPrefs.SetInt("Settings_Music", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        // Ses Efektleri (SFX)
        public static bool SfxEnabled
        {
            get => PlayerPrefs.GetInt("Settings_SFX", 1) == 1;
            set
            {
                PlayerPrefs.SetInt("Settings_SFX", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        // Titreşim (Haptics)
        public static bool HapticsEnabled
        {
            get => PlayerPrefs.GetInt("Settings_Haptics", 1) == 1;
            set
            {
                PlayerPrefs.SetInt("Settings_Haptics", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}