using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Chess.Unity.Managers
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _gameOverPanel;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _winnerText;
        [SerializeField] private Button _restartButton;

        private void Start()
        {
            _gameOverPanel.SetActive(false);
            
            _restartButton.onClick.AddListener(() => 
            {
                GameManager.Instance.RestartGame();
            });
        }

        // DEĞİŞİKLİK: Artık "winner" değil "message" alıyor ve sonuna ekleme yapmıyor.
        public void ShowGameOver(string message)
        {
            _gameOverPanel.SetActive(true);
            _winnerText.text = message; // Direkt gelen mesajı yaz
        }

        public void HideGameOver()
        {
            _gameOverPanel.SetActive(false);
        }
    }
}