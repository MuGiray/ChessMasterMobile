using UnityEngine;
using UnityEngine.InputSystem; // YENİ KÜTÜPHANE
using UnityEngine.EventSystems;
using Chess.Unity.Managers;
using CoreVector2Int = Chess.Core.Models.Vector2Int;

namespace Chess.Unity.Controllers
{
    public class BoardInputHandler : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            // 1. İşaretçi var mı? (Mouse veya Parmak)
            if (Pointer.current == null) return;

            // 2. Basıldı mı? (WasPressedThisFrame: Hem tık hem dokunmayı algılar)
            if (Pointer.current.press.wasPressedThisFrame)
            {
                // UI'a mı tıklandı? (UI Engelleyici)
                if (IsPointerOverUI()) return;

                // Pozisyonu al (Hem mouse hem touch için ortaktır)
                Vector2 pointerPos = Pointer.current.position.ReadValue();
                
                HandleInput(pointerPos);
            }
        }

        private void HandleInput(Vector2 screenPosition)
        {
            // Screen -> World dönüşümü
            Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
            
            // Grid koordinatına yuvarla
            int x = Mathf.RoundToInt(worldPosition.x);
            int y = Mathf.RoundToInt(worldPosition.y);

            // Tahta sınırları içinde mi?
            if (x >= 0 && x < 8 && y >= 0 && y < 8)
            {
                GameManager.Instance.OnSquareSelected(new CoreVector2Int(x, y));
            }
            else
            {
                // Boşluğa tıklanırsa seçimi kaldır
                GameManager.Instance.DeselectPiece();
            }
        }

        // UI Tıklama Kontrolü (Yeni Sistem Uyumlu)
        private bool IsPointerOverUI()
        {
            // EventSystem kontrolü standarttır, ancak mobilde pointer ID gerekebilir.
            // Yeni sistemde genellikle bu basit kontrol yeterlidir.
            if (EventSystem.current == null) return false;

            // Mobilde (Touch) bazen IsPointerOverGameObject() parametresiz çalışmaz.
            // Ancak Pointer.current mantığıyla genelde mouse simülasyonu yapar.
            // Eğer mobilde UI arkasına tıklama sorunu yaşarsan burayı güncelleriz.
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}