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
        
        // OPTİMİZASYON İÇİN EKLENEN DEĞİŞKENLER
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
                ResetTimers(); // Yeni oyunsa da timerları sıfırla
                LoadGame(FenUtility.StartFen);
                _isTimerActive = true; // Yeni oyunda da timer başlasın
            }
            
            // İlk açılışta UI'ı bir kez zorla güncelle
            _uiManager.UpdateTimerUI(_whiteTime, _blackTime, _board.Turn);
        }

        private void Update()
        {
            if (_currentGameState != GameState.InProgress || _isPaused || !_isTimerActive) return;

            // Zamanı Azalt
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

            // --- OPTİMİZASYON ---
            // Sadece saniye değiştiğinde UI güncelle (Performans + Battery Saver)
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
            _currentGameState = GameState.Checkmate; // Teknik olarak Timeout ama oyun biter.
            
            string winner = (loserColor == PieceColor.White) ? "Black" : "White";
            Debug.Log($"TIME OUT! {winner} Wins!");
            
            _uiManager.ShowGameOver($"{winner} Wins by Timeout!");
            
            // Kaydı sil
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
            _initialFen = fen; // Başlangıç noktamızı hatırla
            
            FenUtility.LoadPositionFromFen(_board, fen);
            RefreshBoardVisuals(); // Görseli güncelle
            
            Debug.Log("Game Loaded from FEN.");
        }

        public void RestartGame()
        {
            // 1. ZAMANI VE DURUMU DÜZELT
            if (_isPaused)
            {
                Time.timeScale = 1f;
                _isPaused = false;
                _uiManager.HidePause();
            }

            // 2. UI Temizliği
            _uiManager.HideGameOver();
            
            // 3. Kaydı Sil
            SaveManager.DeleteSave(GameSettings.CurrentMode);
            
            // 4. State Reset
            _currentGameState = GameState.InProgress;
            _selectedSquare = new Vector2Int(-1, -1);
            _validMoves.Clear();
            _commandHistory.Clear();
            
            _boardView.HideHighlights();
            _boardView.HideLastMoveHighlights();
            _capturedPiecesUI.ResetUI();

            // --- BUG FIX ---
             ResetTimers(); 
             _isTimerActive = true; 
             _uiManager.UpdateTimerUI(_whiteTime, _blackTime, PieceColor.White);

            // 5. Oyunu Baştan Yükle
            LoadGame(FenUtility.StartFen);
            
            // 6. Temiz başlangıcı kaydet
            SaveCurrentGame(); 
        }

        private void ReplayGame(SaveData data)
        {
            // 1. Tahtayı BAŞLANGIÇ konumuna getir (SaveData'daki InitialFen)
            // Eğer save eski versiyonsa ve InitialFen yoksa StartFen kullan.
            string startFen = string.IsNullOrEmpty(data.InitialFen) ? FenUtility.StartFen : data.InitialFen;
            
            _initialFen = startFen;
            FenUtility.LoadPositionFromFen(_board, startFen);
            
            // 2. Hamleleri sırayla tekrar oyna (MANTIK ONLY)
            if (data.MoveHistory != null)
            {
                foreach (var moveRec in data.MoveHistory)
                {
                    // Komutu oluştur
                    ICommand cmd = new MoveCommand(_board, moveRec.From, moveRec.To, moveRec.Promotion);
                    
                    // Çalıştır (Bu board state'i günceller)
                    cmd.Execute();
                    
                    // Geçmişe ekle (Böylece Undo çalışır!)
                    _commandHistory.Push(cmd);
                    
                    // Yenen taş varsa UI'a ekle (Görsel tutarlılık için)
                    MoveCommand mCmd = cmd as MoveCommand;
                    if (mCmd != null && mCmd.CapturedPiece.Type != PieceType.None)
                    {
                        _capturedPiecesUI.AddCapturedPiece(mCmd.CapturedPiece);
                    }
                    // En Passant UI da eklenebilir ama Load hızı için kritik değil, 
                    // Undo yapınca zaten düzelecek.
                }
            }

            // 3. Görseli SON haliye güncelle
            RefreshBoardVisuals();
            
            // 4. Son hamle sarı çerçevesini koy
            if (data.MoveHistory != null && data.MoveHistory.Count > 0)
            {
                var lastMove = data.MoveHistory[data.MoveHistory.Count - 1];
                _boardView.HighlightLastMove(lastMove.From, lastMove.To);
            }

            Debug.Log($"Replay Complete. {data.MoveHistory?.Count} moves restored.");
        }

        #region Input & Movement

        public void OnSquareSelected(Vector2Int coords)
        {
            // 1. Oyun bitmişse veya AI düşünüyorsa dokunma
            if (_currentGameState != GameState.InProgress || _isAIThinking || _isPromotionActive || _isPaused) return;

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

            // --- AUTO SAVE ---
            SaveCurrentGame();
            // -----------------

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

        #region Undo System

        public void UndoMove()
        {
            // 1. Güvenlik Kontrolleri
            if (_currentGameState != GameState.InProgress || _isAIThinking || _isPromotionActive || _isPaused) return;

            // 2. Mod Kontrolü
            if (GameSettings.CurrentMode == GameMode.HumanVsAI)
            {
                // AI Modu: Eğer sıra BEYAZ'daysa (yani AI oynamış ve sıra bize geçmişse),
                // "Son Hamleyi Geri Al" demek, hem AI'yı hem Bizi geri al demektir.
                // Böylece tekrar oynama sırası bize geçer.
                if (_board.Turn == PieceColor.White && _commandHistory.Count >= 2)
                {
                    PerformUndo(); // AI'nın hamlesini geri al
                    PerformUndo(); // Bizim hamlemizi geri al
                }
            }
            else
            {
                // Arkadaş Modu: Sadece tek bir hamle geri al.
                if (_commandHistory.Count > 0)
                {
                    PerformUndo();
                }
            }

            // --- AUTO SAVE (Undo sonrası güncelle) ---
            SaveCurrentGame();
        }

        private void PerformUndo()
        {
            if (_commandHistory.Count == 0) return;

            // 1. Komutu Çıkar
            ICommand lastCmd = _commandHistory.Pop();
            MoveCommand moveCmd = lastCmd as MoveCommand; // Verilere erişmek için cast ediyoruz

            // 2. Mantıksal Geri Alma (Board Logic)
            lastCmd.Undo();

            // 3. Görsel Geri Alma (Board Visual)
            // En temiz yöntem: Tahtayı o anki duruma göre yeniden çizmek.
            // (Optimize edilmiş BoardView sayesinde bu işlem çok hızlıdır)
            RefreshBoardVisuals();

            // 4. Yenen Taşlar UI Güncellemesi
            if (moveCmd != null && moveCmd.CapturedPiece.Type != PieceType.None)
            {
                // Eğer bu hamlede taş yendiyse, UI'dan sil.
                _capturedPiecesUI.RemoveLastCapturedPiece(moveCmd.CapturedPiece.Color);
            }

            // 5. Ses
            // Geri alma sesi çalabiliriz veya sessiz olabilir. Şimdilik sessiz.

            // 6. Sarı Çerçeveleri (Last Move) Güncelle
            // Bir önceki hamlenin sarı çerçevesini göstermek için geçmişe bak
            if (_commandHistory.Count > 0)
            {
                // Bir önceki hamleyi bul ama stack'ten çıkarma (Peek)
                // Not: Stack ICommand tutuyor, MoveCommand olduğunu varsayıyoruz.
                // Detaylı görselleştirme için MoveCommand içine "public To/From" eklememiz gerekebilir.
                // Şimdilik sarı çerçeveyi gizleyelim, kafa karıştırmasın.
                _boardView.HideLastMoveHighlights();
            }
            else
            {
                _boardView.HideLastMoveHighlights();
            }
            
            // Oyun durumu devam ediyor (Mat olmuşsa bile geri alınca devam eder)
            _currentGameState = GameState.InProgress;
            _uiManager.HideGameOver(); // Oyun bittiyse paneli kapat
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
            if (_currentGameState == GameState.Checkmate || _currentGameState == GameState.Stalemate)
            {
                SaveManager.DeleteSave(GameSettings.CurrentMode); // GÜNCELLENDİ: Parametre eklendi
                return;
            }

            // Command Stack (LIFO) -> List (Chronological)
            // Stack'i Array yapınca ters sıra gelir (En son yapılan hamle [0] olur).
            // Replay için ESKİDEN YENİYE sıralamalıyız.
            ICommand[] stackArray = _commandHistory.ToArray();
            List<MoveRecord> historyList = new List<MoveRecord>();

            // Tersten döngü (En eskiden en yeniye)
            for (int i = stackArray.Length - 1; i >= 0; i--)
            {
                MoveCommand cmd = stackArray[i] as MoveCommand;
                if (cmd != null)
                {
                    historyList.Add(new MoveRecord(cmd.From, cmd.To, cmd.PromotionType));
                }
            }

            SaveData data = new SaveData
            {
                InitialFen = _initialFen,
                CurrentMode = GameSettings.CurrentMode,
                MoveHistory = historyList,
                
                // SÜRELERİ EKLİYORUZ
                WhiteTimeRemaining = _whiteTime,
                BlackTimeRemaining = _blackTime
            };
            SaveManager.Save(data);
        }

        #region Pause & Menu System

        public void TogglePause()
        {
            // Oyun bitmişse durdurmaya gerek yok
            if (_currentGameState != GameState.InProgress) return;

            _isPaused = !_isPaused;

            if (_isPaused)
            {
                Time.timeScale = 0f; // ZAMANI DURDUR (Animasyonlar, Coroutine'ler durur)
                _uiManager.ShowPause();
            }
            else
            {
                Time.timeScale = 1f; // ZAMANI AKIT
                _uiManager.HidePause();
            }
        }

        public void ReturnToMainMenu()
        {
            // 1. Zamanı normale döndür (Çok önemli!)
            Time.timeScale = 1f;
            
            // 2. Oyunu Kaydet (Çıkarken son durum kalsın)
            SaveCurrentGame();
            
            // 3. Menü Sahnesine Geç
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        #endregion
    }
}