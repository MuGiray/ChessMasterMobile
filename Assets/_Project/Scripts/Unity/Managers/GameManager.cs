using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Chess.Core.Models;
using Chess.Architecture.Commands;
using Chess.Core.Logic;
using Chess.Core.AI;
using Vector2Int = Chess.Core.Models.Vector2Int;

namespace Chess.Unity.Managers
{
    [DefaultExecutionOrder(-10)] // Diğer scriptlerden önce başlasın
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Views.BoardView _boardView;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private CapturedPiecesUI _capturedPiecesUI;
        [SerializeField] private PromotionUI _promotionUI;

        // Core Components
        private Board _board;
        private Stack<ICommand> _commandHistory;
        private ChessAI _aiOpponent;

        // State
        private GameState _currentGameState = GameState.InProgress;
        private Vector2Int _selectedSquare = new Vector2Int(-1, -1);
        private List<Vector2Int> _validMoves = new List<Vector2Int>();

        private bool _isPromotionActive = false;
        
        // AI Control
        private bool _isAIThinking = false;
        private readonly WaitForSeconds _aiDelay = new WaitForSeconds(1.0f); // Cachelendi (Memory Optimization)

        private void Awake()
        {
            if (Instance != null && Instance != this) 
            { 
                Destroy(gameObject); 
                return; 
            }
            Instance = this;
            InitializeGame();
        }

        private void InitializeGame()
        {
            _board = new Board();
            _commandHistory = new Stack<ICommand>();
            _aiOpponent = new ChessAI();

            if (_boardView != null) _boardView.GenerateBoard();
            if (_capturedPiecesUI != null) _capturedPiecesUI.ResetUI();

            // Oyunu Başlat
            LoadGame(FenUtility.StartFen);
            Debug.Log("Game Core Initialized.");
        }

        private void LoadGame(string fen)
        {
            FenUtility.LoadPositionFromFen(_board, fen);
            
            _boardView.ClearPieces();

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

        public void RestartGame()
        {
            _uiManager.HideGameOver();

            // YENİ: Restart atılırsa paneli kapat ve kilidi aç
            _promotionUI.Hide();
            _isPromotionActive = false;

            _currentGameState = GameState.InProgress;
            
            DeselectPiece();
            _boardView.HideHighlights();
            _boardView.HideLastMoveHighlights();
            
            _validMoves.Clear();
            _capturedPiecesUI.ResetUI();
            
            LoadGame(FenUtility.StartFen);
        }

        #region Input & Movement

        public void OnSquareSelected(Vector2Int coords)
        {
            // 1. Oyun bitmişse veya AI düşünüyorsa dokunma
            if (_currentGameState != GameState.InProgress || _isAIThinking || _isPromotionActive) return;

            // 2. MOD KONTROLÜ (DÜZELTME BURADA)
            // Eğer oyun modu "Human vs AI" ise VE sıra Siyah'taysa (AI), oyuncunun dokunmasını engelle.
            // Ama "Human vs Human" ise bu satır çalışmayacak ve Siyah oynayabilecek.
            if (GameSettings.CurrentMode == GameMode.HumanVsAI && _board.Turn == PieceColor.Black) return;

            if (_selectedSquare.x == -1)
            {
                SelectPiece(coords);
            }
            else
            {
                Piece clickedPiece = _board.GetPieceAt(coords);
                if (clickedPiece.Color == _board.Turn)
                {
                    SelectPiece(coords);
                }
                else if (IsMoveValid(coords))
                {
                    ExecuteMove(_selectedSquare, coords);
                    DeselectPiece();
                }
                else
                {
                    DeselectPiece();
                }
            }
        }

        private void SelectPiece(Vector2Int coords)
        {
            Piece piece = _board.GetPieceAt(coords);
            if (piece.Type == PieceType.None || piece.Color != _board.Turn) return;

            _selectedSquare = coords;
            _validMoves = Arbiter.GetLegalMoves(_board, coords);

            List<Vector2Int> moves = new List<Vector2Int>();
            List<Vector2Int> captures = new List<Vector2Int>();

            foreach (var move in _validMoves)
            {
                Piece target = _board.GetPieceAt(move);
                bool isEnPassant = (piece.Type == PieceType.Pawn && move.x != coords.x && target.Type == PieceType.None);

                if (target.Type != PieceType.None || isEnPassant)
                    captures.Add(move);
                else
                    moves.Add(move);
            }
            
            _boardView.HighlightMoves(moves, captures);
        }

        public void DeselectPiece()
        {
            _selectedSquare = new Vector2Int(-1, -1);
            _validMoves.Clear();
            _boardView.HideHighlights();
        }

        private bool IsMoveValid(Vector2Int target)
        {
            // List.Contains yerine döngü daha performanslı olabilir ama bu liste çok küçük (max 27).
            // Kod okunabilirliği için Exists kullanabiliriz.
            return _validMoves.Exists(m => m.x == target.x && m.y == target.y);
        }

        public void ExecuteMove(Vector2Int from, Vector2Int to)
        {
            Piece movedPiece = _board.GetPieceAt(from);
            
            int lastRank = movedPiece.IsWhite ? 7 : 0;
            bool isPromotion = (movedPiece.Type == PieceType.Pawn && to.y == lastRank);
            
            bool isHumanPlayer = (GameSettings.CurrentMode == GameMode.HumanVsHuman) || 
                                 (GameSettings.CurrentMode == GameMode.HumanVsAI && _board.Turn == PieceColor.White);

            if (isPromotion && isHumanPlayer)
            {
                _isPromotionActive = true; // KİLİTLE: Artık tahtaya tıklanamaz
                
                _promotionUI.Show(movedPiece.Color, (selectedType) => 
                {
                    _isPromotionActive = false; // KİLİDİ AÇ: Seçim yapıldı
                    ExecuteConfirmedMove(from, to, selectedType);
                });
                return; 
            }

            ExecuteConfirmedMove(from, to, PieceType.Queen);
        }

        // Asıl işi yapan metod (Eski ExecuteMove içeriği buraya taşındı)
        private void ExecuteConfirmedMove(Vector2Int from, Vector2Int to, PieceType promotionType)
        {
            Piece movedPiece = _board.GetPieceAt(from);
            Piece targetPiece = _board.GetPieceAt(to);
            bool isCapture = targetPiece.Type != PieceType.None;

            if (isCapture)
            {
                _capturedPiecesUI.AddCapturedPiece(targetPiece);
                _boardView.RemovePieceVisual(to);
            }

            // En Passant Görsel Silme
            if (movedPiece.Type == PieceType.Pawn && targetPiece.Type == PieceType.None && from.x != to.x)
            {
                Vector2Int capturedPos = new Vector2Int(to.x, from.y);
                Piece epPiece = _board.GetPieceAt(capturedPos);
                _capturedPiecesUI.AddCapturedPiece(epPiece);
                _boardView.RemovePieceVisual(capturedPos);
            }

            // GÖRSEL HAREKET
            _boardView.MovePieceVisual(from, to);
            _boardView.HighlightLastMove(from, to);

            // CASTLING GÖRSELİ
            if (movedPiece.Type == PieceType.King && Mathf.Abs(from.x - to.x) == 2)
            {
                int rank = from.y;
                bool isKingSide = to.x > from.x;
                Vector2Int rookFrom = isKingSide ? new Vector2Int(7, rank) : new Vector2Int(0, rank);
                Vector2Int rookTo = isKingSide ? new Vector2Int(5, rank) : new Vector2Int(3, rank);
                _boardView.MovePieceVisual(rookFrom, rookTo);
            }

            // SESLER
            if (isCapture) AudioManager.Instance.PlayCapture();
            else AudioManager.Instance.PlayMove();

            // --- COMMAND EXECUTION (Güncellendi) ---
            // Seçilen promotionType'ı gönderiyoruz
            ICommand moveCmd = new MoveCommand(_board, from, to, promotionType);
            moveCmd.Execute();
            _commandHistory.Push(moveCmd);

            // GÖRSEL DÜZELTME (Piyon -> Seçilen Taş)
            Piece pieceAfterMove = _board.GetPieceAt(to);
            if (movedPiece.Type == PieceType.Pawn && pieceAfterMove.Type != PieceType.Pawn)
            {
                _boardView.RemovePieceVisual(to);
                _boardView.PlacePiece(to, pieceAfterMove);
            }

            CheckGameState();

            // AI TETİKLEME
            if (_currentGameState == GameState.InProgress && _board.Turn == PieceColor.Black && GameSettings.CurrentMode == GameMode.HumanVsAI)
            {
                StartCoroutine(TriggerAI());
            }
        }

        #endregion

        #region AI & Game Loop

        private IEnumerator TriggerAI()
        {
            _isAIThinking = true;
            
            Task<Vector2Int[]> aiTask = _aiOpponent.GetBestMoveAsync(_board);
            
            // Task bitene kadar bekle (Thread blocking yapmaz)
            while (!aiTask.IsCompleted)
            {
                yield return null;
            }

            // İnsansı gecikme
            yield return _aiDelay;

            if (aiTask.Result != null)
            {
                ExecuteMove(aiTask.Result[0], aiTask.Result[1]);
            }

            _isAIThinking = false;
        }

        private void CheckGameState()
        {
            _currentGameState = Arbiter.CheckGameState(_board);

            if (_currentGameState == GameState.Checkmate)
            {
                AudioManager.Instance.PlayGameOver();
                
                // Kazananı belirle
                string winnerName = (_board.Turn == PieceColor.White) ? "BLACK" : "WHITE";
                
                // Mesajı burada oluştur: "WHITE WINS!"
                _uiManager.ShowGameOver($"{winnerName} WINS!");
            }
            else if (_currentGameState == GameState.Stalemate)
            {
                AudioManager.Instance.PlayGameOver();
                
                // Mesajı burada oluştur: Alt satıra sebebini yaz
                _uiManager.ShowGameOver("GAME DRAWN\n(Stalemate)");
            }
            // İleride buraya "Insufficient Material" veya "50-Move Rule" ekleyebiliriz.
        }

        public void SetPaused(bool paused)
        {
            // Pause mantığı genişletilebilir
            _isAIThinking = paused; 
        }

        #endregion
    }
}