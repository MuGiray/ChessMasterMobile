using UnityEngine;
using Chess.Unity.Views;

namespace Chess.Unity.Managers
{
    [DefaultExecutionOrder(10)] // BoardView oluştuktan sonra çalışsın
    public class DynamicLayoutManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private Canvas _mainCanvas;

        [Header("UI Groups (RectTransforms)")]
        [Tooltip("Siyah Süre ve Yenen Taşların olduğu üst panel")]
        [SerializeField] private RectTransform _topPanel; 
        
        [Tooltip("Beyaz Süre ve Yenen Taşların olduğu alt panel")]
        [SerializeField] private RectTransform _bottomPanel;

        [Header("Settings")]
        [SerializeField] private float _padding = 50f; // Tahtadan ne kadar uzak olsun? (Piksel)

        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (_boardView == null || _cam == null || _mainCanvas == null) return;

            UpdatePositions();
        }

        private void UpdatePositions()
        {
            float scaleFactor = _mainCanvas.scaleFactor; // Canvas ölçeklemesini hesaba kat

            // 1. ÜST PANEL KONUMLANDIRMA
            if (_topPanel != null)
            {
                // Tahtanın en tepesinin Dünya koordinatını al
                Vector3 worldTop = new Vector3(3.5f, _boardView.GetBoardTopY(), 0);
                
                // Bunu Ekran koordinatına çevir
                Vector3 screenTop = _cam.WorldToScreenPoint(worldTop);

                // Yüksekliği ayarla (Screen Y + Padding)
                // Canvas ölçeğine bölüyoruz ki her çözünürlükte aynı boşluk kalsın
                float targetY = screenTop.y + (_padding * scaleFactor);
                
                // Paneli oraya taşı (Sadece Y eksenini değiştiriyoruz, X ortada kalsın)
                Vector3 currentPos = _topPanel.position;
                _topPanel.position = new Vector3(currentPos.x, targetY, currentPos.z);
            }

            // 2. ALT PANEL KONUMLANDIRMA
            if (_bottomPanel != null)
            {
                // Tahtanın en altının Dünya koordinatını al
                Vector3 worldBottom = new Vector3(3.5f, _boardView.GetBoardBottomY(), 0);
                
                // Ekran koordinatına çevir
                Vector3 screenBottom = _cam.WorldToScreenPoint(worldBottom);

                // Yüksekliği ayarla (Screen Y - Padding)
                float targetY = screenBottom.y - (_padding * scaleFactor);

                Vector3 currentPos = _bottomPanel.position;
                _bottomPanel.position = new Vector3(currentPos.x, targetY, currentPos.z);
            }
        }
    }
}