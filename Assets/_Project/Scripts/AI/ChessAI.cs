using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chess.Core.Models;
using Chess.Core.Logic;
using Chess.Architecture.Commands;
using Vector2Int = Chess.Core.Models.Vector2Int;
using System;

namespace Chess.Core.AI
{
    public class ChessAI
    {
        // Zorluk seviyesine göre derinlik (Easy:2, Medium:3, Hard:4)
        private int _searchDepth = 3; 

        public void SetDifficulty(int depth)
        {
            _searchDepth = depth;
        }

        public async Task<MoveResult> GetBestMoveAsync(Board board)
        {
            Board boardClone = board.Clone();

            return await Task.Run(() => 
            {
                bool isMaximizing = (boardClone.Turn == PieceColor.White);
                Move bestMove = CalculateBestMove(boardClone, _searchDepth, int.MinValue, int.MaxValue, isMaximizing);
                
                return new MoveResult(bestMove.From, bestMove.To, bestMove.Score);
            });
        }

        private Move CalculateBestMove(Board board, int depth, int alpha, int beta, bool isMaximizing)
        {
            if (depth == 0) 
            {
                // Evaluation sınıfı artık hatasız çalışacak
                return new Move(Evaluation.Evaluate(board));
            }

            List<Vector2Int> pieces = GetAllPieces(board, board.Turn);
            
            Move bestMove = new Move(isMaximizing ? int.MinValue : int.MaxValue);
            bool hasMove = false;

            foreach (Vector2Int from in pieces)
            {
                var moves = MoveGenerator.GetPseudoLegalMoves(board, from);

                foreach (Vector2Int to in moves)
                {
                    MoveCommand cmd = new MoveCommand(board, from, to, PieceType.Queen);
                    cmd.Execute();

                    if (IsKingInCheck(board, !isMaximizing)) 
                    {
                        cmd.Undo();
                        continue;
                    }
                    
                    hasMove = true;

                    Move childMove = CalculateBestMove(board, depth - 1, alpha, beta, !isMaximizing);
                    
                    cmd.Undo(); 

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
                if (Arbiter.IsInCheck(board, board.Turn))
                    return new Move(isMaximizing ? -100000 + depth : 100000 - depth); 
                else
                    return new Move(0); 
            }

            return bestMove;
        }

        private bool IsKingInCheck(Board board, bool wasMaximizingPlayer)
        {
            PieceColor justMovedColor = (board.Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            return Arbiter.IsInCheck(board, justMovedColor);
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
        }
    }

    // HATA ÇÖZÜMÜ: Struct tanımı buraya eklendi
    public struct MoveResult
    {
        public Vector2Int From;
        public Vector2Int To;
        public int EvalScore;

        public MoveResult(Vector2Int from, Vector2Int to, int score)
        {
            From = from;
            To = to;
            EvalScore = score;
        }
    }
}