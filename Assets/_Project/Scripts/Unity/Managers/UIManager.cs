using UnityEngine;
using TMPro; // TextMeshPro kullanacağız (Unity'nin modern text motoru)
using UnityEngine.UI;

namespace Chess.Unity.Managers
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _winnerText;
        [SerializeField] private Button _restartButton;

        private void Start()
        {
            // Başlangıçta paneli gizle
            _gameOverPanel.SetActive(false);
            
            // Butona tıklandığında GameManager'ın Restart metodunu çağır
            _restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
        }

        public void ShowGameOver(string winner)
        {
            _gameOverPanel.SetActive(true);
            _winnerText.text = $"{winner} WINS!";
        }

        public void HideGameOver()
        {
            _gameOverPanel.SetActive(false);
        }
    }
}