using UnityEngine;
using System.Collections.Generic;
using Chess.Unity.ScriptableObjects;
using Chess.Core.Models;
using CoreVector2Int = Chess.Core.Models.Vector2Int;

namespace Chess.Unity.Views
{
    public class BoardView : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private TileView _tilePrefab;
        [SerializeField] private PieceView _piecePrefab;
        [SerializeField] private PieceTheme _currentTheme;

        [Header("Highlights")]
        [SerializeField] private GameObject _highlightPrefab;
        [SerializeField] private GameObject _capturePrefab;
        [SerializeField] private GameObject _lastMovePrefab;
        
        [Header("Settings")]
        [SerializeField] private Color _lightColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color _darkColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Transform _boardContainer;
        [SerializeField] private Transform _piecesContainer;

        // Optimization: Dictionary Lookup (O(1) access)
        private Dictionary<CoreVector2Int, PieceView> _activePieces = new Dictionary<CoreVector2Int, PieceView>();

        // Pooling
        private List<GameObject> _highlightPool = new List<GameObject>();
        private List<GameObject> _capturePool = new List<GameObject>();
        private GameObject[] _lastMoveObjects = new GameObject[2];

        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            UpdateCameraSize();
        }

        private void UpdateCameraSize()
        {
            if (_cam == null) return;

            _cam.transform.position = new Vector3(3.5f, 3.5f, -10f);
            float boardSize = 8f;
            float verticalPadding = 2.0f; 
            float horizontalPadding = 0.5f;

            float targetHeight = boardSize + verticalPadding;
            float targetWidth = boardSize + horizontalPadding;

            float screenRatio = (float)Screen.width / Screen.height;
            float targetRatio = targetWidth / targetHeight;

            if (screenRatio >= targetRatio)
                _cam.orthographicSize = targetHeight / 2f;
            else
                _cam.orthographicSize = (targetHeight / 2f) * (targetRatio / screenRatio);
        }

        public void GenerateBoard()
        {
            foreach (Transform child in _boardContainer) Destroy(child.gameObject);

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    TileView tile = Instantiate(_tilePrefab, _boardContainer);
                    tile.Init(x, y);
                    bool isLight = (x + y) % 2 != 0; 
                    tile.SetColor(isLight ? _lightColor : _darkColor);
                }
            }
        }

        public void ClearPieces()
        {
            foreach (var kvp in _activePieces)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }
            _activePieces.Clear();
            
            // Fallback temizlik (Hiyerarşide kalan varsa)
            foreach (Transform child in _piecesContainer) Destroy(child.gameObject);
        }

        public void PlacePiece(CoreVector2Int coords, Piece piece)
        {
            if (piece.Type == PieceType.None) return;

            // Zaten varsa önce sil (Güvenlik)
            if (_activePieces.ContainsKey(coords))
            {
                Destroy(_activePieces[coords].gameObject);
                _activePieces.Remove(coords);
            }

            Sprite sprite = _currentTheme.GetSprite(piece.Type, piece.Color);
            PieceView newPiece = Instantiate(_piecePrefab, _piecesContainer);
            newPiece.Init(piece.Type, piece.Color, sprite);
            newPiece.SetPositionImmediate(coords);

            _activePieces[coords] = newPiece;
        }

        public void MovePieceVisual(CoreVector2Int from, CoreVector2Int to)
        {
            if (_activePieces.TryGetValue(from, out PieceView pieceView))
            {
                pieceView.MoveTo(to);
                
                // Dictionary güncellemesi
                _activePieces.Remove(from);
                
                // Hedefte bir şey varsa (Yeme işlemi UI tarafında yapıldı ama Dict'ten düşmeli)
                if (_activePieces.ContainsKey(to))
                {
                    // Normalde RemovePieceVisual çağrılmış olmalı, ama garanti olsun
                    _activePieces.Remove(to);
                }
                
                _activePieces[to] = pieceView;
            }
            else
            {
                Debug.LogWarning($"Visual consistency error: No piece found at {from.x},{from.y}");
            }
        }

        public void RemovePieceVisual(CoreVector2Int coords)
        {
            if (_activePieces.TryGetValue(coords, out PieceView pieceView))
            {
                Destroy(pieceView.gameObject);
                _activePieces.Remove(coords);
            }
        }

        #region Highlights

        public void HighlightMoves(List<CoreVector2Int> moves, List<CoreVector2Int> captures)
        {
            HideHighlights();

            foreach (var move in moves)
            {
                GameObject hl = GetObjectFromPool(_highlightPool, _highlightPrefab);
                hl.SetActive(true);
                hl.transform.position = new Vector3(move.x, move.y, -2);
            }

            foreach (var cap in captures)
            {
                GameObject capObj = GetObjectFromPool(_capturePool, _capturePrefab);
                capObj.SetActive(true);
                capObj.transform.position = new Vector3(cap.x, cap.y, -2);
            }
        }

        public void HighlightLastMove(CoreVector2Int from, CoreVector2Int to)
        {
            if (_lastMoveObjects[0] == null)
            {
                _lastMoveObjects[0] = Instantiate(_lastMovePrefab, _boardContainer);
                _lastMoveObjects[1] = Instantiate(_lastMovePrefab, _boardContainer);
            }

            _lastMoveObjects[0].SetActive(true);
            _lastMoveObjects[1].SetActive(true);
            _lastMoveObjects[0].transform.position = new Vector3(from.x, from.y, 0);
            _lastMoveObjects[1].transform.position = new Vector3(to.x, to.y, 0);
        }

        public void HideHighlights()
        {
            foreach (var hl in _highlightPool) hl.SetActive(false);
            foreach (var cap in _capturePool) cap.SetActive(false);
        }

        public void HideLastMoveHighlights()
        {
            if (_lastMoveObjects[0] != null) _lastMoveObjects[0].SetActive(false);
            if (_lastMoveObjects[1] != null) _lastMoveObjects[1].SetActive(false);
        }

        private GameObject GetObjectFromPool(List<GameObject> pool, GameObject prefab)
        {
            foreach (var item in pool)
            {
                if (!item.activeInHierarchy) return item;
            }
            GameObject newItem = Instantiate(prefab, _boardContainer);
            pool.Add(newItem);
            return newItem;
        }

        #endregion
    }
}