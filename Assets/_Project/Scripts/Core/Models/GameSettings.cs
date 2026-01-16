namespace Chess.Core.Models
{
    public enum GameMode
    {
        HumanVsAI,      // İnsan vs Yapay Zeka
        HumanVsHuman    // İnsan vs İnsan (Sıcak Koltuk)
    }

    public static class GameSettings
    {
        // Varsayılan olarak AI seçili olsun
        public static GameMode CurrentMode = GameMode.HumanVsAI;
    }
}