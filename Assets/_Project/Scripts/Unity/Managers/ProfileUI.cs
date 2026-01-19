using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess.Core.Models;

namespace Chess.Unity.Managers
{
    public class ProfileUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _usernameText;
        [SerializeField] private TextMeshProUGUI _eloText;
        [SerializeField] private TextMeshProUGUI _rankText; // Rütbe (Örn: Usta Adayı)
        
        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI _gamesPlayedText;
        [SerializeField] private TextMeshProUGUI _winsText;
        [SerializeField] private TextMeshProUGUI _lossesText;
        [SerializeField] private TextMeshProUGUI _winRateText;

        [Header("Visuals")]
        [SerializeField] private Image _rankIcon; // İleride rütbe ikonu eklemek istersen
        [SerializeField] private Color _winColor = Color.green;
        [SerializeField] private Color _lossColor = Color.red;

        private void Start()
        {
            // Panel başlangıçta kapalı olsun
            if (_panel != null) _panel.SetActive(false);
        }

        public void ShowProfile()
        {
            UpdateVisuals();
            _panel.SetActive(true);
        }

        public void HideProfile()
        {
            _panel.SetActive(false);
        }

        private void UpdateVisuals()
        {
            // Veriyi Çek
            UserProfile profile = ProfileManager.GetProfile();

            // Temel Bilgiler
            _usernameText.text = profile.Username;
            _eloText.text = $"ELO: {profile.ELO}";
            
            // Rütbe Belirle
            _rankText.text = GetRankTitle(profile.ELO);
            _rankText.color = GetRankColor(profile.ELO);

            // İstatistikler
            _gamesPlayedText.text = profile.MatchesPlayed.ToString();
            _winsText.text = profile.Wins.ToString();
            _lossesText.text = profile.Losses.ToString();
            
            // Kazanma Oranı (Örn: %55.4)
            _winRateText.text = $"%{profile.WinRate:F1}";
        }

        // ELO'ya göre Rütbe Sistemi
        private string GetRankTitle(int elo)
        {
            if (elo < 1000) return "ÇIRAK";             // Apprentice
            if (elo < 1200) return "ACEMİ";             // Novice
            if (elo < 1400) return "OYUNCU";            // Player
            if (elo < 1600) return "USTA ADAYI";        // Candidate Master
            if (elo < 1800) return "USTA";              // Master
            if (elo < 2000) return "ULUSLARARASI USTA"; // IM
            return "BÜYÜK USTA";                        // Grandmaster (GM)
        }

        private Color GetRankColor(int elo)
        {
            if (elo < 1000) return Color.gray;
            if (elo < 1200) return Color.white;
            if (elo < 1400) return Color.green;
            if (elo < 1600) return Color.cyan;
            if (elo < 1800) return new Color(1f, 0.5f, 0f); // Turuncu
            if (elo < 2000) return Color.red;
            return new Color(0.8f, 0.2f, 1f); // Mor (Efsanevi)
        }
    }
}