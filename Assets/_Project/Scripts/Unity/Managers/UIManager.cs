using UnityEngine;
using TMPro; 
using UnityEngine.UI;

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
    }
}