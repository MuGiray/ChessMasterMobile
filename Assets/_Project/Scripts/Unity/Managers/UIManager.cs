using UnityEngine;
using TMPro; 
using UnityEngine.UI;
using Chess.Core.Models;

namespace Chess.Unity.Managers
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _pausePanel; // YENİ: Durdurma Paneli

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _winnerText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _pauseRestartButton; // Pause panelindeki Restart butonu

        [Header("Chess Clock UI")]
        [SerializeField] private TextMeshProUGUI _whiteTimerText;
        [SerializeField] private TextMeshProUGUI _blackTimerText;
        [SerializeField] private GameObject _whiteTimerBg; // Aktiflik efekti için arka plan
        [SerializeField] private GameObject _blackTimerBg;

        // Renkler (Modern Tasarım)
        private Color _activeColor = new Color(1f, 1f, 1f, 1f); // Parlak
        private Color _inactiveColor = new Color(1f, 1f, 1f, 0.5f); // Soluk
        
        // YENİ BUTONLAR
        [SerializeField] private Button _pauseButton;      // Oyunu durduran buton (Sağ üstte)
        [SerializeField] private Button _resumeButton;     // Devam et butonu (Panelde)
        [SerializeField] private Button _mainMenuButton;   // Menüye dön butonu (Panelde)

        private void Start()
        {
            _gameOverPanel.SetActive(false);
            _pausePanel.SetActive(false); // Başlangıçta kapalı
            
            // --- BUTON BAĞLANTILARI ---
            
            _restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            
            // Pause Butonu -> GameManager'daki TogglePause'u çağırır
            _pauseButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
            
            // Resume Butonu -> Aynı TogglePause metodunu çağırır (Açıkken kapatır)
            _resumeButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
            
            // Main Menu Butonu -> GameManager üzerinden menüye döner
            _mainMenuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMainMenu());

            if (_pauseRestartButton != null)
            {
                _pauseRestartButton.onClick.AddListener(() => 
                {
                    GameManager.Instance.RestartGame();
                });
            }
        }

        public void ShowGameOver(string message)
        {
            _gameOverPanel.SetActive(true);
            _winnerText.text = message;
        }

        public void HideGameOver()
        {
            _gameOverPanel.SetActive(false);
        }

        // YENİ METODLAR
        public void ShowPause() => _pausePanel.SetActive(true);
        public void HidePause() => _pausePanel.SetActive(false);

        // YENİ METOD: Süreleri Ekrana Yaz
        public void UpdateTimerUI(float whiteTime, float blackTime, PieceColor currentTurn)
        {
            _whiteTimerText.text = FormatTime(whiteTime);
            _blackTimerText.text = FormatTime(blackTime);

            // Aktif saati vurgula
            if (currentTurn == PieceColor.White)
            {
                _whiteTimerText.color = _activeColor;
                _blackTimerText.color = _inactiveColor;
                // Arka plan görselleri varsa onları da açıp kapatabilirsin
            }
            else
            {
                _whiteTimerText.color = _inactiveColor;
                _blackTimerText.color = _activeColor;
            }
        }

        // Saniyeyi Dakika:Saniye formatına çevirir (Örn: 09:59)
        private string FormatTime(float timeInSeconds)
        {
            if (timeInSeconds < 0) timeInSeconds = 0;
            
            int minutes = Mathf.FloorToInt(timeInSeconds / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);
            
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}