using UnityEngine;
using Chess.Unity.Managers; // GameManager'a erişim gerekebilir
using Chess.Unity.ScriptableObjects; // Namespace'i eklemeyi unutma
using Chess.Core.Models;
using System.Collections.Generic; // EKSİK OLAN SATIR BU

// ÇÖZÜM: Core içindeki Vector2Int için alias.
using CoreVector2Int = Chess.Core.Models.Vector2Int;

namespace Chess.Unity.Views
{
    public class BoardView : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private TileView _tilePrefab; // Prefab referansı
        [SerializeField] private PieceView _piecePrefab; // YENİ
        [SerializeField] private PieceTheme _currentTheme; // YENİ
        [SerializeField] private GameObject _highlightPrefab; // YENİ: Yeşil nokta prefabı
        
        [Header("Settings")]
        [SerializeField] private Color _lightColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color _darkColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Transform _boardContainer; // Hiyerarşide kirlilik olmasın diye parent
        [SerializeField] private Transform _piecesContainer; // YENİ: Taşları ayrı bir objede toplayalım

        // POOLING SİSTEMİ: Çöp üretmemek için objeleri saklıyoruz.
        private List<GameObject> _highlightPool = new List<GameObject>();

        private void Update()
        {
            // Bu satırın varlığından emin ol. Eğer silindiyse kamera ayarlama yapmaz.
            UpdateCameraSize();
        }

        private void UpdateCameraSize()
        {
            Vector3 centerPosition = new Vector3(3.5f, 3.5f, -10f);
            Camera.main.transform.position = centerPosition;

            float boardSize = 8f;
            
            // --- AYARLAR ---
            // Dikey modda (Portrait) üst/alt boşluk (UI için)
            float verticalPadding = 2.0f; 
            // Yatay modda (Landscape) yan boşluk
            float horizontalPadding = 0.5f;

            float targetHeight = boardSize + verticalPadding;
            float targetWidth = boardSize + horizontalPadding;

            float screenRatio = (float)Screen.width / Screen.height;
            float targetRatio = targetWidth / targetHeight;

            if (screenRatio >= targetRatio)
            {
                // LANDSCAPE (Yatay): Ekran geniş, Yüksekliği baz al.
                // Eğer burası çalışıyor ama tahta küçükse, targetHeight değeriyle oyna.
                Camera.main.orthographicSize = targetHeight / 2f;
            }
            else
            {
                // PORTRAIT (Dikey): Ekran dar, Genişliği baz al.
                float differenceInSize = targetRatio / screenRatio;
                Camera.main.orthographicSize = (targetHeight / 2f) * differenceInSize;
            }
        }

        // YENİ METOD: Dışarıdan (GameManager) çağrılacak
        public void PlacePiece(CoreVector2Int coords, Piece piece)
        {
            if (piece.Type == PieceType.None) return;

            Sprite sprite = _currentTheme.GetSprite(piece.Type, piece.Color);
            if (sprite == null) 
            {
                Debug.LogError($"Sprite not found for {piece.Color} {piece.Type}");
                return;
            }

            PieceView newPiece = Instantiate(_piecePrefab, _piecesContainer);
            newPiece.Init(piece.Type, piece.Color, sprite);
            newPiece.SetPositionImmediate(coords); // SetPosition yerine bunu kullan
        }

        // YENİ METOD: Bir taşı görsel olarak hareket ettir
        public void MovePieceVisual(CoreVector2Int from, CoreVector2Int to)
        {
            // Kaynak karedeki görsel objeyi bulmamız lazım.
            // Bunun için basit bir Raycast veya Dictionary kullanabiliriz.
            // Şimdilik en basit yöntem: Koordinata göre ara (Performanslı değil ama Logic sağlamlaşana kadar yeterli)
            
            // Not: İleride Dictionary<Vector2Int, PieceView> kullanacağız.
            foreach (Transform child in _piecesContainer)
            {
                if (Mathf.Abs(child.position.x - from.x) < 0.1f && Mathf.Abs(child.position.y - from.y) < 0.1f)
                {
                    PieceView view = child.GetComponent<PieceView>();
                    view.MoveTo(to);
                    return;
                }
            }
        }

        // YENİ METOD: Bir taşı görsel olarak yok et (Yeme işlemi)
        public void RemovePieceVisual(CoreVector2Int coords)
        {
             foreach (Transform child in _piecesContainer)
            {
                if (Mathf.Abs(child.position.x - coords.x) < 0.1f && Mathf.Abs(child.position.y - coords.y) < 0.1f)
                {
                    Destroy(child.gameObject);
                    return;
                }
            }
        }

        // Tahtayı temizlerken taşları da silelim (GenerateBoard içine eklenecek)
        public void ClearPieces()
        {
            foreach (Transform child in _piecesContainer)
            {
                Destroy(child.gameObject);
            }
        }

        public void GenerateBoard()
        {
            // Eski tahta varsa temizle (Editör modunda test ederken faydalı)
            foreach (Transform child in _boardContainer)
            {
                Destroy(child.gameObject);
            }

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    // Object Pooling burada devreye girebilir ama 64 kare için Instantiate performans sorunu yaratmaz.
                    TileView tile = Instantiate(_tilePrefab, _boardContainer);
                    tile.Init(x, y);

                    // Satranç tahtası renklendirme formülü: (x + y) tek ise koyu, çift ise açık.
                    bool isLight = (x + y) % 2 != 0; 
                    tile.SetColor(isLight ? _lightColor : _darkColor);
                }
            }
        }

        // YENİ METOD: Hamleleri ekranda göster
        public void HighlightMoves(List<CoreVector2Int> moves)
        {
            HideHighlights(); // Önce eskileri gizle

            foreach (var move in moves)
            {
                GameObject hl = GetHighlightObject();
                hl.SetActive(true);
                // Z = -2 yaparak taşların (-1) da üzerinde görünmesini sağlıyoruz.
                hl.transform.position = new Vector3(move.x, move.y, -2); 
            }
        }

        // YENİ METOD: Hepsini havuza geri gönder (Pasif yap)
        public void HideHighlights()
        {
            foreach (var hl in _highlightPool)
            {
                hl.SetActive(false);
            }
        }

        // YENİ METOD: Havuz Yönetimi
        private GameObject GetHighlightObject()
        {
            // 1. Havuzda pasif duran var mı? Varsa onu ver.
            foreach (var hl in _highlightPool)
            {
                if (!hl.activeInHierarchy) return hl;
            }

            // 2. Yoksa yeni üret ve havuza ekle.
            GameObject newHl = Instantiate(_highlightPrefab, _boardContainer); // TilesContainer içinde dursun
            _highlightPool.Add(newHl);
            return newHl;
        }
    }
}