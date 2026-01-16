using System;

namespace Chess.Core.Models
{
    [Serializable]
    public class SaveData
    {
        public string FenString; // Tahta durumu
        public GameMode CurrentMode; // Oyun modu (AI vs Human)
        
        // İleride eklenebilecekler:
        // public float TimerWhite;
        // public float TimerBlack;
        // public bool IsSoundMuted;
    }
}