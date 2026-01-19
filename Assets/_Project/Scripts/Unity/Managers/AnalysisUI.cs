using UnityEngine;
using TMPro; // TextMeshPro
using Chess.Core.AI;

namespace Chess.Unity.Managers
{
    public class AnalysisUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;
        
        [Header("Statistics")]
        [SerializeField] private TextMeshProUGUI _blunderCountText;
        [SerializeField] private TextMeshProUGUI _mistakeCountText;
        [SerializeField] private TextMeshProUGUI _inaccuracyCountText;

        [Header("Comments List")]
        [SerializeField] private Transform _contentContainer; // ScrollView Content'i
        [SerializeField] private GameObject _commentPrefab;   // Listeye eklenecek satır

        private void Start()
        {
            _panel.SetActive(false);
        }

        public void ShowReport(GameReport report)
        {
            // İstatistikleri Yaz
            _blunderCountText.text = report.Blunders.ToString();
            _mistakeCountText.text = report.Mistakes.ToString();
            _inaccuracyCountText.text = report.Inaccuracies.ToString();

            // Eski yorumları temizle
            foreach (Transform child in _contentContainer)
            {
                Destroy(child.gameObject);
            }

            // Yeni yorumları listele
            foreach (var comment in report.Comments)
            {
                GameObject item = Instantiate(_commentPrefab, _contentContainer);
                
                TextMeshProUGUI textComp = item.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                {
                    textComp.text = comment.Message;
                    
                    // --- GÜNCELLENEN RENK MANTIĞI (TÜRKÇE) ---
                    if (comment.Message.Contains("BÜYÜK HATA")) 
                    {
                        textComp.color = Color.red; // Kırmızı
                    }
                    else if (comment.Message.Contains("HATA")) 
                    {
                        // "BÜYÜK HATA" zaten "HATA" içerdiği için sıralama önemli.
                        // Ancak yukarıdaki if bloğu "BÜYÜK HATA"yı yakalayacağı için sorun yok.
                        // Yine de güvenli olması için tam eşleşme veya else-if yapısı kullanıyoruz.
                        textComp.color = new Color(1f, 0.5f, 0f); // Turuncu
                    }
                    else 
                    {
                        textComp.color = Color.yellow; // Sarı (Eksiklik)
                    }
                }
            }

            _panel.SetActive(true);
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }
    }
}