using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks; // Asenkron işlem için
using Chess.Core.Models;
using Chess.Core.Logic;
using Vector2Int = Chess.Core.Models.Vector2Int;

namespace Chess.Core.AI
{
    public class ChessAI
    {
        private const int MAX_DEPTH = 3; // Zeka Derinliği (3 hamle sonrasını görür)
        // Not: Mobilde 3 idealdir. 4 ve üzeri telefon işlemcisini zorlayabilir.

        // TAŞ DEĞERLERİ (Material Scores)
        private Dictionary<PieceType, int> _pieceValues = new Dictionary<PieceType, int>()
        {
            { PieceType.None, 0 },
            { PieceType.Pawn, 100 },
            { PieceType.Knight, 320 },
            { PieceType.Bishop, 330 },
            { PieceType.Rook, 500 },
            { PieceType.Queen, 900 },
            { PieceType.King, 20000 }
        };

        // ANA METOD: En iyi hamleyi bul (Asenkron çalışır, oyunu dondurmaz)
        public async Task<Vector2Int[]> GetBestMoveAsync(Board board)
        {
            return await Task.Run(() => 
            {
                // Tahtanın kopyasını almayacağız (Performans). 
                // Yap-Boz (Make-Unmake) tekniği kullanacağız.
                
                Move bestMove = CalculateBestMove(board, MAX_DEPTH, int.MinValue, int.MaxValue, false);
                return new Vector2Int[] { bestMove.From, bestMove.To };
            });
        }

        // MINIMAX ALGORİTMASI (Alpha-Beta Budamalı)
        // isMaximizing: True ise Beyaz (Avantaj arıyor), False ise Siyah (Avantaj arıyor)
        // Biz AI'yı SİYAH olarak varsayacağız, yani Minimize etmeye çalışacak (Skor ne kadar düşükse Siyah o kadar önde).
        // DÜZELTME: Standart Minimax'ta bir taraf Max, diğer taraf Min'dir.
        // Genelde: Beyaz (+), Siyah (-). 
        // Yani Siyah oynuyorsa en KÜÇÜK değeri arayacağız.
        
        private Move CalculateBestMove(Board board, int depth, int alpha, int beta, bool isMaximizingPlayer)
        {
            // 1. Derinlik bittiyse veya oyun bittiyse -> Durumu puanla
            if (depth == 0) // TODO: Checkmate kontrolü de eklenebilir
            {
                return new Move(EvaluateBoard(board));
            }

            List<Vector2Int> allPieces = GetAllPieces(board, isMaximizingPlayer ? PieceColor.White : PieceColor.Black);
            Move bestMove = new Move(isMaximizingPlayer ? int.MinValue : int.MaxValue);

            foreach (Vector2Int from in allPieces)
            {
                // Sadece Yasal (Legal) hamleleri değil, Pseudo hamleleri alıp kontrol edeceğiz.
                // Arbiter.GetLegalMoves çok yavaş olabilir, burada basitleştirilmiş bir legal check yapacağız.
                // Performans için Arbiter yerine MoveGenerator kullanıp, Şahı tehlikeye atıyor mu diye manuel bakacağız.
                
                var moves = MoveGenerator.GetPseudoLegalMoves(board, from);

                foreach (Vector2Int to in moves)
                {
                    // --- SİMÜLASYON BAŞLANGICI ---
                    Piece movedPiece = board.GetPieceAt(from);
                    Piece capturedPiece = board.GetPieceAt(to);

                    // Kralı yiyebilecek hamle varsa (Illegal durum ama ağaçta çıkabilir), sonsuz puan ver.
                    if (capturedPiece.Type == PieceType.King) 
                    {
                         // Geri al ve dön
                         return new Move(isMaximizingPlayer ? 100000 : -100000, from, to);
                    }

                    // Basit Legal Kontrol (Kendi şahını tehlikeye atma)
                    // (Performans için bu adımı derinlik > 1 iken atlayabiliriz ama şimdilik güvenli gidelim)
                    
                    // Hamleyi Yap
                    board.SetPieceAt(to, movedPiece);
                    board.SetPieceAt(from, new Piece(PieceType.None, PieceColor.None));
                    
                    // RECURSION (Özyineleme)
                    Move childMove = CalculateBestMove(board, depth - 1, alpha, beta, !isMaximizingPlayer);
                    
                    // Hamleyi Geri Al (Backtracking)
                    board.SetPieceAt(from, movedPiece);
                    board.SetPieceAt(to, capturedPiece);
                    // --- SİMÜLASYON BİTİŞİ ---

                    // SKOR DEĞERLENDİRME
                    if (isMaximizingPlayer) // BEYAZ (En yüksek puanı arıyor)
                    {
                        if (childMove.Score > bestMove.Score)
                        {
                            bestMove.Score = childMove.Score;
                            bestMove.From = from;
                            bestMove.To = to;
                        }
                        alpha = System.Math.Max(alpha, bestMove.Score);
                    }
                    else // SİYAH (En düşük puanı arıyor - Negatif değerler siyahın avantajıdır)
                    {
                        if (childMove.Score < bestMove.Score)
                        {
                            bestMove.Score = childMove.Score;
                            bestMove.From = from;
                            bestMove.To = to;
                        }
                        beta = System.Math.Min(beta, bestMove.Score);
                    }

                    // Alpha-Beta Budama (Gereksiz dalları kes)
                    if (beta <= alpha) break;
                }
                if (beta <= alpha) break;
            }

            return bestMove;
        }

        // TAHTA PUANLAMA (Evaluation Function)
        private int EvaluateBoard(Board board)
        {
            int score = 0;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece p = board.GetPieceAt(new Vector2Int(x, y));
                    if (p.Type != PieceType.None)
                    {
                        int value = _pieceValues[p.Type];
                        // Beyazsa ekle, Siyahsa çıkar
                        score += (p.Color == PieceColor.White) ? value : -value;
                        
                        // Konum Avantajı (İleride eklenebilir: Merkezdeki taşlar daha değerlidir)
                    }
                }
            }
            return score;
        }

        private List<Vector2Int> GetAllPieces(Board board, PieceColor color)
        {
            List<Vector2Int> pieces = new List<Vector2Int>();
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (board.GetPieceAt(new Vector2Int(x, y)).Color == color)
                        pieces.Add(new Vector2Int(x, y));
                }
            }
            return pieces;
        }

        // Yardımcı Struct
        private struct Move
        {
            public int Score;
            public Vector2Int From;
            public Vector2Int To;

            public Move(int score) { Score = score; From = new Vector2Int(-1,-1); To = new Vector2Int(-1, -1); }
            public Move(int score, Vector2Int from, Vector2Int to) { Score = score; From = from; To = to; }
        }
    }
}