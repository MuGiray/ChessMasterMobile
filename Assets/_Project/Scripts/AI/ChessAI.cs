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

        private readonly Dictionary<PieceType, int> _pieceValues = new Dictionary<PieceType, int>()
        {
            { PieceType.None, 0 },
            { PieceType.Pawn, 100 },
            { PieceType.Knight, 320 },
            { PieceType.Bishop, 330 },
            { PieceType.Rook, 500 },
            { PieceType.Queen, 900 },
            { PieceType.King, 20000 }
        };

        public async Task<Vector2Int[]> GetBestMoveAsync(Board board)
        {
            return await Task.Run(() => 
            {
                Move bestMove = CalculateBestMove(board, MAX_DEPTH, int.MinValue, int.MaxValue, false);
                return new Vector2Int[] { bestMove.From, bestMove.To };
            });
        }

        private Move CalculateBestMove(Board board, int depth, int alpha, int beta, bool isMaximizingPlayer)
        {
            if (depth == 0) return new Move(EvaluateBoard(board));

            List<Vector2Int> allPieces = GetAllPieces(board, isMaximizingPlayer ? PieceColor.White : PieceColor.Black);
            Move bestMove = new Move(isMaximizingPlayer ? int.MinValue : int.MaxValue);

            foreach (Vector2Int from in allPieces)
            {
                var moves = MoveGenerator.GetPseudoLegalMoves(board, from);

                foreach (Vector2Int to in moves)
                {
                    Piece movedPiece = board.GetPieceAt(from);
                    Piece capturedPiece = board.GetPieceAt(to);

                    // Kral yeme kontrolü (Illegal durum yakalama)
                    if (capturedPiece.Type == PieceType.King) 
                         return new Move(isMaximizingPlayer ? 100000 : -100000, from, to);

                    // Simülasyon
                    board.SetPieceAt(to, movedPiece);
                    board.SetPieceAt(from, new Piece(PieceType.None, PieceColor.None));
                    
                    Move childMove = CalculateBestMove(board, depth - 1, alpha, beta, !isMaximizingPlayer);
                    
                    // Geri Al
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

                    if (beta <= alpha) break;
                }
                if (beta <= alpha) break;
            }

            return bestMove;
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
                        int value = _pieceValues[p.Type];
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