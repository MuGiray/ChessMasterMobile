using UnityEngine;
using System.Collections.Generic;
using Chess.Core.Models;
using Chess.Architecture.Commands;
using Chess.Core.Logic;
using Vector2Int = Chess.Core.Models.Vector2Int;

namespace Chess.Unity.Managers
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private Board _board;
        private Stack<ICommand> _commandHistory;

        [SerializeField] private Views.BoardView _boardView;

        // STATE
        private Vector2Int _selectedSquare = new Vector2Int(-1, -1);
        private List<Vector2Int> _validMoves = new List<Vector2Int>(); // Seçili taşın gidebileceği yerler

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            InitializeGame();
        }

        private void InitializeGame()
        {
            _board = new Board();
            _commandHistory = new Stack<ICommand>();

            if (_boardView != null)
                _boardView.GenerateBoard();

            // YENİ BAŞLANGIÇ: FEN YÜKLEME
            LoadGame(FenUtility.StartFen);
            
            Debug.Log("Game Core Initialized. Standard Board Loaded.");
        }

        private void LoadGame(string fen)
        {
            // 1. Logic (Model) Yükle
            FenUtility.LoadPositionFromFen(_board, fen);

            // 2. View (Görsel) Yükle
            // Önce eski taşları temizle
            _boardView.ClearPieces();

            // Modeli tarayıp taşları oluştur
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece piece = _board.GetPieceAt(new Vector2Int(x, y));
                    if (piece.Type != PieceType.None)
                    {
                        _boardView.PlacePiece(new Vector2Int(x, y), piece);
                    }
                }
            }
        }

        private void SpawnPiece(Vector2Int pos, Piece piece)
        {
            _board.SetPieceAt(pos, piece);
            _boardView.PlacePiece(pos, piece);
        }

        // --- INPUT HANDLING ---

        public void OnSquareSelected(Vector2Int coords)
        {
            // 1. Durum: Hiçbir taş seçili değilse -> SEÇ
            if (_selectedSquare.x == -1)
            {
                SelectPiece(coords);
            }
            // 2. Durum: Zaten bir taş seçiliyse -> KARAR VER
            else
            {
                // A. Kendi taşına tekrar tıkladı -> Seçimi İptal Et veya Değiştir
                Piece clickedPiece = _board.GetPieceAt(coords);
                if (clickedPiece.Color == _board.Turn)
                {
                    SelectPiece(coords); // Seçimi değiştir
                    return;
                }

                // B. Geçerli bir hamle karesine tıkladı -> OYNA!
                if (IsMoveValid(coords))
                {
                    ExecuteMove(_selectedSquare, coords);
                    DeselectPiece();
                }
                else
                {
                    // C. Geçersiz yere tıkladı -> İptal et
                    DeselectPiece();
                }
            }
        }

        private void SelectPiece(Vector2Int coords)
        {
            Piece piece = _board.GetPieceAt(coords);

            // Sadece sırası gelen oyuncunun taşını seçebilirsin
            if (piece.Type == PieceType.None || piece.Color != _board.Turn) return;

            _selectedSquare = coords;
            
            // Logic'ten hamleleri al ve sakla
            _validMoves = MoveGenerator.GetPseudoLegalMoves(_board, coords);
            
            _boardView.HighlightMoves(_validMoves);
            Debug.Log($"Selected: {coords.x},{coords.y}");
        }

        public void DeselectPiece()
        {
            _selectedSquare = new Vector2Int(-1, -1);
            if (_validMoves != null) _validMoves.Clear(); // Null check eklemek her zaman güvenlidir
            _boardView.HideHighlights();
        }

        private bool IsMoveValid(Vector2Int target)
        {
            foreach (var move in _validMoves)
            {
                if (move.x == target.x && move.y == target.y) return true;
            }
            return false;
        }

        public void ExecuteMove(Vector2Int from, Vector2Int to)
        {
            // 1. Rakip taş var mı? Varsa görsel olarak sil.
            Piece targetPiece = _board.GetPieceAt(to);
            if (targetPiece.Type != PieceType.None)
            {
                _boardView.RemovePieceVisual(to);
            }

            // 2. View'ı güncelle (Animasyon)
            _boardView.MovePieceVisual(from, to);

            // 3. Command Pattern ile Model'i güncelle
            ICommand move = new MoveCommand(_board, from, to);
            move.Execute();
            _commandHistory.Push(move);

            Debug.Log($"Move Executed. Turn is now: {_board.Turn}");
        }
    }
}