using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chess.Core.Models;
using Chess.Core.Logic;
using Chess.Architecture.Commands; // MoveCommand için
using Vector2Int = Chess.Core.Models.Vector2Int;
using System;

namespace Chess.Core.AI
{
    public class ChessAI
    {
        // Zorluk seviyesine göre derinlik (Easy:2, Medium:3, Hard:4)
        private int _searchDepth = 3; 

        // EVALUATION: Bu değerleri daha sonra Evaluation.cs'ye taşıyacağız ama şimdilik burada kalsın.
        private readonly int[] _pieceValues = new int[] 
        { 
            0,      // None
            100,    // Pawn
            320,    // Knight
            330,    // Bishop
            500,    // Rook
            900,    // Queen
            20000   // King
        };

        public void SetDifficulty(int depth)
        {
            _searchDepth = depth;
        }

        public async Task<MoveResult> GetBestMoveAsync(Board board)
        {
            // Ana tahtayı kopyala ki UI'daki oyun etkilenmesin
            Board boardClone = board.Clone();

            return await Task.Run(() => 
            {
                Move bestMove = CalculateBestMove(boardClone, _searchDepth, int.MinValue, int.MaxValue, true); // true = AI (White/Black farketmez, maximize eden taraf)
                return new MoveResult(bestMove.From, bestMove.To, bestMove.Score);
            });
        }

        private Move CalculateBestMove(Board board, int depth, int alpha, int beta, bool isMaximizing)
        {
            if (depth == 0) return new Move(EvaluateBoard(board));

            // Optimist yaklaşım: Önce taş yiyen hamlelere bakılmalı (Move Ordering)
            // Şimdilik standart PseudoLegal alıyoruz.
            List<Vector2Int> pieces = GetAllPieces(board, board.Turn);
            
            Move bestMove = new Move(isMaximizing ? int.MinValue : int.MaxValue);
            bool hasMove = false;

            foreach (Vector2Int from in pieces)
            {
                var moves = MoveGenerator.GetPseudoLegalMoves(board, from);

                foreach (Vector2Int to in moves)
                {
                    // KRİTİK DÜZELTME: SetPieceAt yerine MoveCommand kullan.
                    // Bu, Rok haklarını ve oyun durumunu doğru yönetir.
                    // AI analizinde Promotion hep Vezir varsayılır (Basitleştirme).
                    MoveCommand cmd = new MoveCommand(board, from, to, PieceType.Queen);
                    
                    // Kendi şahımızı tehlikeye atıyor muyuz? (PseudoLegal kontrolü)
                    // MoveCommand içinde Execute yapıp sonra geri alacağız ama
                    // Execute yapmadan önce basit bir kontrol maliyetli olur.
                    // En güvenlisi: Oyna -> Şah kontrolü yap -> Geri al.
                    
                    cmd.Execute();

                    // Eğer hamle illegal ise (Şah yiyorsa veya kendi şahını açıyorsa) geri al ve geç
                    if (IsKingInCheck(board, !isMaximizing)) // Hamleyi yapan taraf (sıra değiştiği için !isMaximizing) şah altında mı?
                    {
                        cmd.Undo();
                        continue;
                    }
                    
                    hasMove = true;

                    Move childMove = CalculateBestMove(board, depth - 1, alpha, beta, !isMaximizing);
                    
                    cmd.Undo(); // Tahtayı eski haline getir

                    if (isMaximizing)
                    {
                        if (childMove.Score > bestMove.Score)
                        {
                            bestMove.Score = childMove.Score;
                            bestMove.From = from;
                            bestMove.To = to;
                        }
                        alpha = Math.Max(alpha, bestMove.Score);
                    }
                    else
                    {
                        if (childMove.Score < bestMove.Score)
                        {
                            bestMove.Score = childMove.Score;
                            bestMove.From = from;
                            bestMove.To = to;
                        }
                        beta = Math.Min(beta, bestMove.Score);
                    }

                    if (beta <= alpha) break;
                }
                if (beta <= alpha) break;
            }

            if (!hasMove)
            {
                // Mat veya Pat durumu
                if (Arbiter.IsInCheck(board, board.Turn))
                    return new Move(isMaximizing ? -100000 + depth : 100000 - depth); // Mat (Derinlik avantajı ile)
                else
                    return new Move(0); // Pat (Draw)
            }

            return bestMove;
        }

        private bool IsKingInCheck(Board board, bool wasMaximizingPlayer)
        {
            // Hamleyi yapan tarafın şahı tehdit altında mı?
            // Not: MoveCommand sonrası Turn değişti. O yüzden "önceki" oyuncunun rengine bakacağız.
            // Ama Board.Turn zaten değişmiş durumda. 
            // Eğer White oynadıysa, şimdi sıra Black. White King kontrol edilmeli.
            PieceColor justMovedColor = (board.Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            return Arbiter.IsInCheck(board, justMovedColor);
        }

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
                        int value = _pieceValues[(int)p.Type];
                        score += (p.Color == PieceColor.White) ? value : -value;
                    }
                }
            }
            // Siyah için skoru tersine çevirmiyoruz, Minimax (Negamax değil) kullanıyoruz.
            // White pozitif, Black negatif sever.
            return score;
        }

        private List<Vector2Int> GetAllPieces(Board board, PieceColor color)
        {
            List<Vector2Int> pieces = new List<Vector2Int>(16); 
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

        // Helper structs
        private struct Move
        {
            public int Score;
            public Vector2Int From;
            public Vector2Int To;
            public Move(int score) { Score = score; From = new Vector2Int(-1,-1); To = new Vector2Int(-1, -1); }
        }
    }

    // UI'a dönecek sonuç paketi
    public struct MoveResult
    {
        public Vector2Int From;
        public Vector2Int To;
        public int EvalScore; // Analiz için puanı burada taşıyacağız

        public MoveResult(Vector2Int from, Vector2Int to, int score)
        {
            From = from;
            To = to;
            EvalScore = score;
        }
    }
}