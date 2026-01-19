using System;

namespace Chess.Core.Models
{
    [Serializable]
    public class UserProfile
    {
        public string Username = "Player";
        public int ELO = 1200; // Başlangıç seviyesi (Orta)
        public int MatchesPlayed = 0;
        public int Wins = 0;
        public int Losses = 0;
        public int Draws = 0;

        // Kazanma Oranı (%)
        public float WinRate => MatchesPlayed == 0 ? 0 : (float)Wins / MatchesPlayed * 100f;
    }
}