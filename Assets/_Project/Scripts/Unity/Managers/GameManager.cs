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
    [DefaultExecutionOrder(-10)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Views.BoardView _boardView;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private CapturedPiecesUI _capturedPiecesUI;
        [SerializeField] private PromotionUI _promotionUI;
        [SerializeField] private AnalysisUI _analysisUI;

        // Core Components
        private Board _board;
        private Stack<ICommand> _commandHistory;
        private ChessAI _aiOpponent;
        private string _initialFen;

        // --- CHESS CLOCK ---
        private float _whiteTime;
        private float _blackTime;
        private const float START_TIME = 600f; 
        private bool _isTimerActive = false;
        
        private int _lastWhiteSecond;
        private int _lastBlackSecond;

        // State
        private GameState _currentGameState = GameState.InProgress;
        private Vector2Int _selectedSquare = new Vector2Int(-1, -1);
        private List<Vector2Int> _validMoves = new List<Vector2Int>();

        private bool _isPromotionActive = false;
        private bool _isPaused = false;
        
        private bool _isAIThinking = false;
        private readonly WaitForSeconds _aiDelay = new WaitForSeconds(1.0f);

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

            if (_boardView != null) _boardView.GenerateBoard();
            if (_capturedPiecesUI != null) _capturedPiecesUI.ResetUI();

            _aiOpponent = new ChessAI();
            // Adaptif Zorluk
            if (GameSettings.CurrentMode == GameMode.HumanVsAI)
            {
                UserProfile profile = ProfileManager.GetProfile();
                _aiOpponent.AdaptToELO(profile.ELO);
            }
            else
            {
                _aiOpponent.SetDifficulty(3); // 2 Kişilik modda standart kalabilir veya kullanılmaz
            }

            GameMode currentMode = GameSettings.CurrentMode;

            if (SaveManager.HasSave(currentMode))
            {
                Debug.Log($"Save file found for {currentMode}. Replaying History...");
                SaveData data = SaveManager.Load(GameSettings.CurrentMode);
                
                if (data != null)
                {
                    _whiteTime = data.WhiteTimeRemaining > 0 ? data.WhiteTimeRemaining : START_TIME;
                    _blackTime = data.BlackTimeRemaining > 0 ? data.BlackTimeRemaining : START_TIME;

                    GameSettings.CurrentMode = data.CurrentMode;
                    ReplayGame(data);
                }
                else
                {
                    ResetTimers();
                    LoadGame(FenUtility.StartFen);
                }
                _isTimerActive = true;
            }
            else
            {
                ResetTimers();
                LoadGame(FenUtility.StartFen);
                _isTimerActive = true;
            }
            
            _uiManager.UpdateTimerUI(_whiteTime, _blackTime, _board.Turn);
        }

        private void Update()
        {
            if (_currentGameState != GameState.InProgress || _isPaused || !_isTimerActive) return;

            if (_board.Turn == PieceColor.White)
            {
                _whiteTime -= Time.deltaTime;
                if (_whiteTime <= 0) HandleTimeout(PieceColor.White);
            }
            else
            {
                _blackTime -= Time.deltaTime;
                if (_blackTime <= 0) HandleTimeout(PieceColor.Black);
            }

            int currentWhiteCeil = Mathf.CeilToInt(_whiteTime);
            int currentBlackCeil = Mathf.CeilToInt(_blackTime);

            if (currentWhiteCeil != _lastWhiteSecond || currentBlackCeil != _lastBlackSecond)
            {
                _uiManager.UpdateTimerUI(_whiteTime, _blackTime, _board.Turn);
                _lastWhiteSecond = currentWhiteCeil;
                _lastBlackSecond = currentBlackCeil;
            }
        }

        private void HandleTimeout(PieceColor loserColor)
        {
            _isTimerActive = false;
            _currentGameState = GameState.Checkmate; 
            
            AudioManager.Instance.PlayGameOver(); 
            
            string winner = (loserColor == PieceColor.White) ? "Black" : "White";
            _uiManager.ShowGameOver($"{winner} Wins by Timeout!");
            
            SaveManager.DeleteSave(GameSettings.CurrentMode);
        }

        private void ResetTimers()
        {
            _whiteTime = START_TIME;
            _blackTime = START_TIME;
            _lastWhiteSecond = (int)START_TIME;
            _lastBlackSecond = (int)START_TIME;
        }

        private void LoadGame(string fen)
        {
            _initialFen = fen;
            FenUtility.LoadPositionFromFen(_board, fen);
            RefreshBoardVisuals();
            Debug.Log("Game Loaded from FEN.");
        }

        public void RestartGame()
        {
            if (_isPaused)
            {
                Time.timeScale = 1f;
                _isPaused = false;
                _uiManager.HidePause();
            }

            _uiManager.HideGameOver();
            SaveManager.DeleteSave(GameSettings.CurrentMode);
            
            _currentGameState = GameState.InProgress;
            _selectedSquare = new Vector2Int(-1, -1);
            _validMoves.Clear();
            _commandHistory.Clear();
            
            _boardView.HideHighlights();
            _boardView.HideLastMoveHighlights();
            _capturedPiecesUI.ResetUI();

             ResetTimers(); 
             _isTimerActive = true; 
             _uiManager.UpdateTimerUI(_whiteTime, _blackTime, PieceColor.White);

            LoadGame(FenUtility.StartFen);
            SaveCurrentGame(); 
        }

        private void ReplayGame(SaveData data)
        {
            string startFen = string.IsNullOrEmpty(data.InitialFen) ? FenUtility.StartFen : data.InitialFen;
            _initialFen = startFen;
            FenUtility.LoadPositionFromFen(_board, startFen);
            
            if (data.MoveHistory != null)
            {
                foreach (var moveRec in data.MoveHistory)
                {
                    ICommand cmd = new MoveCommand(_board, moveRec.From, moveRec.To, moveRec.Promotion);
                    
                    MoveCommand mCmd = cmd as MoveCommand;
                    if (mCmd != null) 
                    {
                        mCmd.Notation = moveRec.Notation;
                        mCmd.EvaluationScore = moveRec.EvalScore; // YENİ: Skoru geri yükle
                    }

                    cmd.Execute(); 
                    
                    _commandHistory.Push(cmd);
                    
                    if (mCmd != null && mCmd.CapturedPiece.Type != PieceType.None)
                    {
                        _capturedPiecesUI.AddCapturedPiece(mCmd.CapturedPiece);
                    }
                }
            }

            RefreshBoardVisuals();
            
            if (data.MoveHistory != null && data.MoveHistory.Count > 0)
            {
                var lastMove = data.MoveHistory[data.MoveHistory.Count - 1];
                _boardView.HighlightLastMove(lastMove.From, lastMove.To);
            }
            
            CheckGameState();

            Debug.Log($"Replay Complete. {data.MoveHistory?.Count} moves restored.");
        }

        #region Input & Movement

        public void OnSquareSelected(Vector2Int coords)
        {
            if (_currentGameState != GameState.InProgress || _isAIThinking || _isPromotionActive || _isPaused) return;
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
                    HapticsManager.Instance.VibrateError();
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
            return _validMoves.Exists(m => m.x == target.x && m.y == target.y);
        }

        // YENİ: evalScore parametresi eklendi (Opsiyonel)
        public void ExecuteMove(Vector2Int from, Vector2Int to, int evalScore = 0)
        {
            Piece movedPiece = _board.GetPieceAt(from);
            
            int lastRank = movedPiece.IsWhite ? 7 : 0;
            bool isPromotion = (movedPiece.Type == PieceType.Pawn && to.y == lastRank);
            
            bool isHumanPlayer = (GameSettings.CurrentMode == GameMode.HumanVsHuman) || 
                                 (GameSettings.CurrentMode == GameMode.HumanVsAI && _board.Turn == PieceColor.White);

            if (isPromotion && isHumanPlayer)
            {
                _isPromotionActive = true; 
                _promotionUI.Show(movedPiece.Color, (selectedType) => 
                {
                    _isPromotionActive = false; 
                    // İnsan hamlesi olduğu için puan şimdilik 0, AI hesaplayacaksa sonra güncellenir
                    ExecuteConfirmedMove(from, to, selectedType, 0); 
                });
                return; 
            }

            ExecuteConfirmedMove(from, to, PieceType.Queen, evalScore);
        }

        private void ExecuteConfirmedMove(Vector2Int from, Vector2Int to, PieceType promotionType, int evalScore)
        {
            MoveCommand moveCmd = new MoveCommand(_board, from, to, promotionType);
            
            // ANALİZ PUANI ENTEGRASYONU
            moveCmd.EvaluationScore = evalScore;

            string pgnMove = NotationConverter.EncodeMove(_board, moveCmd);

            // Görsel İşlemler
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

            _boardView.MovePieceVisual(from, to);
            _boardView.HighlightLastMove(from, to);

            if (movedPiece.Type == PieceType.King && Mathf.Abs(from.x - to.x) == 2)
            {
                int rank = from.y;
                bool isKingSide = to.x > from.x;
                Vector2Int rookFrom = isKingSide ? new Vector2Int(7, rank) : new Vector2Int(0, rank);
                Vector2Int rookTo = isKingSide ? new Vector2Int(5, rank) : new Vector2Int(3, rank);
                _boardView.MovePieceVisual(rookFrom, rookTo);
            }

            if (isCapture) AudioManager.Instance.PlayCapture();
            else AudioManager.Instance.PlayMove();

            moveCmd.Execute(); 
            _commandHistory.Push(moveCmd);

            // EĞER EVAL SCORE 0 İSE (İnsan Oynadıysa) HESAPLA
            if (evalScore == 0)
            {
                // Evaluation sınıfı statik analiz yapar (Derinlik yok ama insan hatasını yakalar)
                moveCmd.EvaluationScore = _aiOpponent.GetPositionScore(_board, 2);
            }
            else
            {
                moveCmd.EvaluationScore = evalScore; // AI zaten hesapladı
            }
            // ----------------------------------------------------

            MoveCommand castedCmd = moveCmd as MoveCommand;
            if (isCapture || (castedCmd != null && castedCmd.CapturedPiece.Type != PieceType.None))
            {
                HapticsManager.Instance.VibrateMedium();
            }
            else
            {
                HapticsManager.Instance.VibrateLight();
            }

            Piece pieceAfterMove = _board.GetPieceAt(to);
            if (movedPiece.Type == PieceType.Pawn && pieceAfterMove.Type != PieceType.Pawn)
            {
                _boardView.RemovePieceVisual(to);
                _boardView.PlacePiece(to, pieceAfterMove);
            }

            CheckGameState();

            if (_currentGameState == GameState.Checkmate)
            {
                pgnMove += "#";
            }
            else if (Arbiter.IsInCheck(_board, _board.Turn)) 
            {
                pgnMove += "+";
            }
            
            moveCmd.Notation = pgnMove;
            Debug.Log($"PGN: {pgnMove} | Eval: {evalScore}"); 

            SaveCurrentGame();

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
            
            // --- HATA DÜZELTME: Return type artık MoveResult ---
            Task<MoveResult> aiTask = _aiOpponent.GetBestMoveAsync(_board);
            
            while (!aiTask.IsCompleted)
            {
                yield return null;
            }

            yield return _aiDelay;

            if (aiTask.Result.From.x != -1) // Geçerli bir hamle döndüyse
            {
                MoveResult result = aiTask.Result;
                // Skoru da parametre olarak gönderiyoruz
                ExecuteMove(result.From, result.To, result.EvalScore);
            }

            _isAIThinking = false;
        }

        private void CheckGameState()
        {
            _currentGameState = Arbiter.CheckGameState(_board);

            // Eğer oyun bittiyse İstatistik Güncelle
            if (_currentGameState != GameState.InProgress)
            {
                HandleGameOver(_currentGameState); // YENİ: Tüm bitiş işlemlerini buraya taşıyalım
            }
            else
            {
                if (Arbiter.IsInCheck(_board, _board.Turn))
                {
                    HapticsManager.Instance.VibrateMedium();
                    AudioManager.Instance.PlayNotify();
                }
            }
        }

        // YENİ METOD: Oyun bitişini temiz bir şekilde yönetir
        private void HandleGameOver(GameState state)
        {
            AudioManager.Instance.PlayGameOver();
            SaveManager.DeleteSave(GameSettings.CurrentMode);

            // 1. Mesajı Hazırla
            string msg = "";
            if (state == GameState.Checkmate)
            {
                HapticsManager.Instance.VibrateHeavy();
                string winnerName = (_board.Turn == PieceColor.White) ? "BLACK" : "WHITE";
                msg = $"{winnerName} WINS!";
                
                // 2. İstatistik Güncelle (Sadece Human vs AI ise)
                if (GameSettings.CurrentMode == GameMode.HumanVsAI)
                {
                    // Eğer Beyaz (İnsan) sıradaysa ve Mat olduysa -> KAYBETTİ (-1)
                    // Eğer Siyah (AI) sıradaysa ve Mat olduysa -> KAZANDI (1)
                    int result = (_board.Turn == PieceColor.White) ? -1 : 1;
                    
                    // Zorluk seviyesini (Basitçe ELO'ya göre 1-3 arası veriyoruz)
                    int difficulty = 2; // Orta
                    UserProfile p = ProfileManager.GetProfile();
                    if (p.ELO < 1000) difficulty = 1;
                    else if (p.ELO > 1500) difficulty = 3;

                    ProfileManager.UpdateStats(result, difficulty);
                }
            }
            else // Beraberlik Durumları
            {
                HapticsManager.Instance.VibrateHeavy();
                if (state == GameState.Stalemate) msg = "DRAW\n(Stalemate)";
                else msg = "DRAW"; // Diğer sebepler

                if (GameSettings.CurrentMode == GameMode.HumanVsAI)
                {
                    ProfileManager.UpdateStats(0, 2); // 0 = Berabere
                }
            }

            _uiManager.ShowGameOver(msg);
        }

        public void SetPaused(bool paused)
        {
            _isAIThinking = paused; 
        }

        #endregion

        #region Undo System

        public void UndoMove()
        {
            if (_currentGameState != GameState.InProgress || _isAIThinking || _isPromotionActive || _isPaused) return;

            if (GameSettings.CurrentMode == GameMode.HumanVsAI)
            {
                if (_board.Turn == PieceColor.White && _commandHistory.Count >= 2)
                {
                    PerformUndo(); 
                    PerformUndo(); 
                }
            }
            else
            {
                if (_commandHistory.Count > 0)
                {
                    PerformUndo();
                }
            }

            SaveCurrentGame();
        }

        private void PerformUndo()
        {
            if (_commandHistory.Count == 0) return;

            ICommand lastCmd = _commandHistory.Pop();
            MoveCommand moveCmd = lastCmd as MoveCommand; 

            lastCmd.Undo();
            RefreshBoardVisuals();

            if (moveCmd != null && moveCmd.CapturedPiece.Type != PieceType.None)
            {
                _capturedPiecesUI.RemoveLastCapturedPiece(moveCmd.CapturedPiece.Color);
            }

            if (_commandHistory.Count > 0)
            {
                _boardView.HideLastMoveHighlights();
            }
            else
            {
                _boardView.HideLastMoveHighlights();
            }
            
            _currentGameState = GameState.InProgress;
            _uiManager.HideGameOver();
        }

        private void RefreshBoardVisuals()
        {
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

        #endregion

        private void SaveCurrentGame()
        {
            if (_currentGameState != GameState.InProgress) return;

            ICommand[] stackArray = _commandHistory.ToArray();
            List<MoveRecord> historyList = new List<MoveRecord>();

            for (int i = stackArray.Length - 1; i >= 0; i--)
            {
                MoveCommand cmd = stackArray[i] as MoveCommand;
                if (cmd != null)
                {
                    // YENİ: EvalScore eklendi
                    historyList.Add(new MoveRecord(cmd.From, cmd.To, cmd.PromotionType, cmd.Notation, cmd.EvaluationScore));
                }
            }

            SaveData data = new SaveData
            {
                InitialFen = _initialFen,
                CurrentMode = GameSettings.CurrentMode,
                MoveHistory = historyList,
                WhiteTimeRemaining = _whiteTime,
                BlackTimeRemaining = _blackTime,
                HalfMoveClock = _board.HalfMoveClock,
                FullMoveNumber = _board.FullMoveNumber
            };
            SaveManager.Save(data);
        }

        #region Pause & Menu System

        public void TogglePause()
        {
            if (_currentGameState != GameState.InProgress) return;

            _isPaused = !_isPaused;

            if (_isPaused)
            {
                Time.timeScale = 0f; 
                _uiManager.ShowPause();
            }
            else
            {
                Time.timeScale = 1f; 
                _uiManager.HidePause();
            }
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SaveCurrentGame();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        #endregion

        public void AnalyzeGame()
        {            
            // YENİ YÖNTEM: Doğrudan hafızadaki canlı veriyi kullanıyoruz.
            
            if (_commandHistory == null || _commandHistory.Count == 0) 
            {
                Debug.LogWarning("Analiz edilecek hamle geçmişi yok.");
                return;
            }

            // 1. Stack'i Listeye çevir (SaveCurrentGame'deki mantığın aynısı)
            // Stack LIFO (Last In First Out) olduğu için tersten döngü kuruyoruz.
            ICommand[] stackArray = _commandHistory.ToArray();
            List<MoveRecord> historyList = new List<MoveRecord>();

            for (int i = stackArray.Length - 1; i >= 0; i--)
            {
                MoveCommand cmd = stackArray[i] as MoveCommand;
                if (cmd != null)
                {
                    // Hamle verisini paketle
                    historyList.Add(new MoveRecord(
                        cmd.From, 
                        cmd.To, 
                        cmd.PromotionType, 
                        cmd.Notation, 
                        cmd.EvaluationScore
                    ));
                }
            }

            // 2. Dedektifi çağır (Analiz)
            Chess.Core.AI.GameReport report = Chess.Core.AI.GameAnalyst.Analyze(historyList);
            
            // 3. Sonucu Ekrana Bas
            if (_analysisUI != null)
            {
                _analysisUI.ShowReport(report);
            }
            else
            {
                Debug.LogError("AnalysisUI referansı GameManager'da atanmamış!");
            }
        }
    }
}