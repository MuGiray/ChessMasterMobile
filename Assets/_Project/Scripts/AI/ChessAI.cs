using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chess.Core.Models;
using Chess.Core.Logic;
using Vector2Int = Chess.Core.Models.Vector2Int;
using System;

namespace Chess.Core.AI
{
    public class ChessAI
    {
        private const int MAX_DEPTH = 3; 

        // OPTİMİZASYON: Dictionary yerine Array kullanımı (O(1) ve çok hızlı)
        // PieceType enum değerlerine karşılık gelen puanlar.
        // None=0, Pawn=1, Knight=2, Bishop=3, Rook=4, Queen=5, King=6
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

        public async Task<Vector2Int[]> GetBestMoveAsync(Board board)
        {
            // KRİTİK DÜZELTME: Ana tahtayı kopyala (Clone).
            // Böylece AI arka planda düşünürken ana oyun etkilenmez.
            Board boardClone = board.Clone();

            return await Task.Run(() => 
            {
                Move bestMove = CalculateBestMove(boardClone, MAX_DEPTH, int.MinValue, int.MaxValue, false);
                return new Vector2Int[] { bestMove.From, bestMove.To };
            });
        }

        private Move CalculateBestMove(Board board, int depth, int alpha, int beta, bool isMaximizingPlayer)
        {
            if (depth == 0) return new Move(EvaluateBoard(board));

            // Not: GetAllPieces performansı PieceList mimarisi olmadığı için O(64)'tür.
            // Ancak şu an için kabul edilebilir.
            List<Vector2Int> allPieces = GetAllPieces(board, isMaximizingPlayer ? PieceColor.White : PieceColor.Black);
            
            Move bestMove = new Move(isMaximizingPlayer ? int.MinValue : int.MaxValue);
            
            // Eğer hiç taş yoksa veya hamle yoksa (Mat/Pat durumu), mevcut skoru döndür
            if (allPieces.Count == 0) return new Move(EvaluateBoard(board));

            foreach (Vector2Int from in allPieces)
            {
                var moves = MoveGenerator.GetPseudoLegalMoves(board, from);

                foreach (Vector2Int to in moves)
                {
                    Piece movedPiece = board.GetPieceAt(from);
                    Piece capturedPiece = board.GetPieceAt(to);

                    // Kral yeme kontrolü (Illegal durum yakalama - PseudoLegal olduğu için Kral yenebilir gibi görünür)
                    if (capturedPiece.Type == PieceType.King) 
                         return new Move(isMaximizingPlayer ? 100000 : -100000, from, to);

                    // Simülasyon
                    board.SetPieceAt(to, movedPiece);
                    board.SetPieceAt(from, new Piece(PieceType.None, PieceColor.None));
                    
                    Move childMove = CalculateBestMove(board, depth - 1, alpha, beta, !isMaximizingPlayer);
                    
                    // Geri Al (Backtracking)
                    board.SetPieceAt(from, movedPiece);
                    board.SetPieceAt(to, capturedPiece);

                    if (isMaximizingPlayer)
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

                    if (beta <= alpha) break; // Alpha-Beta Pruning
                }
                if (beta <= alpha) break;
            }

            // Eğer hiç geçerli hamle bulunamadıysa (ama taş varsa), en iyi skor olarak mevcut durumu dön
            if (bestMove.From.x == -1) return new Move(EvaluateBoard(board));

            return bestMove;
        }

        private int EvaluateBoard(Board board)
        {
            int score = 0;
            // Dizi erişimi Dictionary'den çok daha hızlıdır
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece p = board.GetPieceAt(new Vector2Int(x, y));
                    if (p.Type != PieceType.None)
                    {
                        // Array lookup optimizasyonu
                        int value = _pieceValues[(int)p.Type];
                        score += (p.Color == PieceColor.White) ? value : -value;
                    }
                }
            }
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