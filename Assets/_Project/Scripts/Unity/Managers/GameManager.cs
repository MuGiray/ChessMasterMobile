using UnityEngine;
using System.Collections.Generic;
using Chess.Core.Models;
using Chess.Architecture.Commands;
using Chess.Core.Logic;
using Vector2Int = Chess.Core.Models.Vector2Int;
using System.Threading.Tasks; // Task için gerekli
using Chess.Core.AI; // AI için gerekli

namespace Chess.Unity.Managers
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private Board _board;
        private Stack<ICommand> _commandHistory;
        // YENİ: AI Kontrolcüsü
        private ChessAI _aiOpponent;
        private bool _isAIThinking = false; // AI düşünürken oyuncu hamle yapamasın

        [SerializeField] private Views.BoardView _boardView;
        [SerializeField] private UIManager _uiManager;
        
        private GameState _currentGameState = GameState.InProgress; //Oyun durumunu takip et

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

            _aiOpponent = new ChessAI(); // AI'yı başlat
            
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

            Debug.Log($"FEN Loaded. Rights: {_board.CurrentCastlingRights}, EnPassant: {_board.EnPassantSquare}");
        }

        private void SpawnPiece(Vector2Int pos, Piece piece)
        {
            _board.SetPieceAt(pos, piece);
            _boardView.PlacePiece(pos, piece);
        }

        public void RestartGame()
        {
            // 1. UI'ı gizle
            _uiManager.HideGameOver();
            
            // 2. State'i sıfırla
            _currentGameState = GameState.InProgress;
            _selectedSquare = new Vector2Int(-1, -1);
            _validMoves.Clear();
            _boardView.HideHighlights();
            
            // 3. Oyunu yeniden yükle (FEN)
            LoadGame(FenUtility.StartFen);
        }

        // --- INPUT HANDLING ---

        public void OnSquareSelected(Vector2Int coords)
        {
            // INPUT KİLİDİ GÜNCELLEMESİ:
            // Oyun bitmişse VEYA AI düşünüyorsa dokunmayı engelle.
            if (_currentGameState != GameState.InProgress || _isAIThinking) return;
            
            // Eğer sıra Siyahtaysa (AI'nın sırası) oyuncunun dokunmasını engelle (Çift güvenlik)
            if (_board.Turn == PieceColor.Black) return;

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
            if (piece.Type == PieceType.None || piece.Color != _board.Turn) return;

            _selectedSquare = coords;

            // 1. Tüm yasal hamleleri al
            _validMoves = Arbiter.GetLegalMoves(_board, coords);

            // 2. Hamleleri Ayrıştır (Boş Kareler vs Yeme Hamleleri)
            List<Vector2Int> moveCoords = new List<Vector2Int>();
            List<Vector2Int> captureCoords = new List<Vector2Int>();

            foreach (var move in _validMoves)
            {
                Piece targetPiece = _board.GetPieceAt(move);
                
                // Eğer hedefte taş varsa ve rengi farklıysa -> YEME (Capture)
                // (En Passant özel durumu: Piyon çapraz gidiyorsa ve hedef boşsa bile Capture sayılır)
                bool isEnPassant = (piece.Type == PieceType.Pawn && move.x != coords.x && targetPiece.Type == PieceType.None);

                if (targetPiece.Type != PieceType.None || isEnPassant)
                {
                    captureCoords.Add(move);
                }
                else
                {
                    moveCoords.Add(move);
                }
            }
            
            // 3. View'a iki listeyi de gönder
            _boardView.HighlightMoves(moveCoords, captureCoords);
            
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
            Piece movedPiece = _board.GetPieceAt(from);
            // 1. Rakip taş var mı? Varsa görsel olarak sil.
            Piece targetPiece = _board.GetPieceAt(to);

            bool isCapture = targetPiece.Type != PieceType.None;

            if (targetPiece.Type != PieceType.None)
            {
                _boardView.RemovePieceVisual(to);
            }

            // --- GÖRSEL İŞLEMLER ---
            
            // A. Ana Taşı Oynat
            _boardView.MovePieceVisual(from, to);

            // B. ÖZEL DURUM: ROK (Görsel)
            if (movedPiece.Type == PieceType.King && Mathf.Abs(from.x - to.x) == 2)
            {
                // Rok olduğunu anladık, Kaleyi de görsel olarak taşıyalım
                int rank = from.y; // Şahın bulunduğu satır
                bool isKingSide = to.x > from.x;
                
                Vector2Int rookFrom = isKingSide ? new Vector2Int(7, rank) : new Vector2Int(0, rank);
                Vector2Int rookTo = isKingSide ? new Vector2Int(5, rank) : new Vector2Int(3, rank);
                
                _boardView.MovePieceVisual(rookFrom, rookTo);
            }

            if (isCapture)
            {
                AudioManager.Instance.PlayCapture();
            }
            else
            {
                AudioManager.Instance.PlayMove();
            }

            // --- MANTIKSAL İŞLEMLER ---
            
            // 3. Command Pattern ile Model'i güncelle
            ICommand move = new MoveCommand(_board, from, to);
            move.Execute();
            _commandHistory.Push(move);

            Debug.Log($"Move Executed. Turn is now: {_board.Turn}");
            CheckGameState();
            // HAMLE BİTTİ, SIRA DEĞİŞTİ. ŞİMDİ KİMDE?
            if (_currentGameState == GameState.InProgress && _board.Turn == PieceColor.Black)
            {
                // Sıra Siyah'a (AI) geçti. Düşünmeye başla.
                // UI Thread'i kilitlememek için asenkron çağırıyoruz ama 'await' kullanmak için 
                // bu metodu async yapamayız (Unity Event sistemi sevmez).
                // O yüzden "Fire and Forget" yapacağız ama _isAIThinking flag'i ile koruyacağız.
                StartCoroutine(TriggerAI());
            }
        }

        // YENİ: AI Tetikleyici (Coroutine)
        private System.Collections.IEnumerator TriggerAI()
        {
            _isAIThinking = true;
            Debug.Log("AI Thinking...");

            // AI'nın hamlesini bekle (Arka planda hesaplasın)
            Task<Vector2Int[]> aiTask = _aiOpponent.GetBestMoveAsync(_board);
            
            // Task bitene kadar bekle (Unity donmaz)
            while (!aiTask.IsCompleted)
            {
                yield return null;
            }

            if (aiTask.Result != null)
            {
                Vector2Int aiFrom = aiTask.Result[0];
                Vector2Int aiTo = aiTask.Result[1];
                
                // AI'nın hamlesini oynat!
                // Not: Burası recursion olmaması için ExecuteMove'u dikkatli çağırmalıyız
                // Ama ExecuteMove içinde "Turn == Black" kontrolü var, AI oynadıktan sonra sıra Beyaz'a geçeceği için sorun yok.
                ExecuteMove(aiFrom, aiTo);
            }

            _isAIThinking = false;
            Debug.Log("AI Move Complete.");
        }

        private void CheckGameState()
        {
            GameState state = Arbiter.CheckGameState(_board);
            _currentGameState = state;

            if (state == GameState.Checkmate)
            {
                AudioManager.Instance.PlayGameOver(); // YENİ: Bitiş Sesi
                
                string winner = (_board.Turn == PieceColor.White) ? "BLACK" : "WHITE";
                Debug.Log($"Kazanan: {winner}");
                _uiManager.ShowGameOver(winner);
            }
            else if (state == GameState.Stalemate)
            {
                AudioManager.Instance.PlayGameOver(); // YENİ
                _uiManager.ShowGameOver("DRAW (Stalemate)");
            }
            // Şah çekme sesi (Opsiyonel):
            // Eğer mat değilse ama Şah tehdit altındaysa 'PlayNotify' çalınabilir.
            // Bunu kontrol etmek için Arbiter'a "IsCheck" sorusu sormamız gerekir.
        }
    }
}