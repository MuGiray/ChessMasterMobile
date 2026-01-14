using UnityEngine;
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
            // Mobil ve Editor uyumlu tıklama kontrolü
            if (Input.GetMouseButtonDown(0))
            {
                HandleInput(Input.mousePosition);
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                HandleInput(Input.GetTouch(0).position);
            }
        }

        private void HandleInput(Vector3 screenPosition)
        {
            // Ekran koordinatını Dünya koordinatına çevir
            Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
            
            // Satranç tahtası tam sayı (integer) koordinatlarda olduğu için yuvarlıyoruz (Floor değil Round)
            // Örn: 3.4 -> 3, 3.8 -> 4
            int x = Mathf.RoundToInt(worldPosition.x);
            int y = Mathf.RoundToInt(worldPosition.y);

            // Tahta sınırları içinde mi?
            if (x >= 0 && x < 8 && y >= 0 && y < 8)
            {
                // GameManager'a bildir
                GameManager.Instance.OnSquareSelected(new CoreVector2Int(x, y));
            }
            else
            {
                // Boşluğa tıklandı, seçimi kaldır
                GameManager.Instance.DeselectPiece();
            }
        }
    }
}