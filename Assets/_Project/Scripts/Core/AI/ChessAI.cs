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
        private int _searchDepth = 3;
        private int _contemptFactor = 0; // Beraberlikten kaçınma isteği (Puan)

        // Materyal Değerleri
        private readonly int[] _pieceValues = new int[] 
        { 
            0, 100, 320, 330, 500, 900, 20000
        };

        // --- ADAPTİF ZORLUK AYARI ---
        public void AdaptToELO(int playerELO)
        {
            // ELO'ya göre AI Profili belirle
            if (playerELO < 1000)
            {
                _searchDepth = 2; // Acemi: Sadece 2 hamle sonrasını görür (Hata yapar)
                _contemptFactor = 50; // Risk alır
            }
            else if (playerELO < 1500)
            {
                _searchDepth = 3; // Orta: Standart oyun
                _contemptFactor = 20;
            }
            else
            {
                _searchDepth = 4; // Usta: Çok derin hesaplar
                _contemptFactor = 0; // Soğukkanlı oynar
            }

            Debug.Log($"AI Adapted to ELO {playerELO}. Depth: {_searchDepth}, Contempt: {_contemptFactor}");
        }

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

        // Mevcut pozisyonu analiz etmek için
        public int GetPositionScore(Board board, int depth)
        {
            Board boardClone = board.Clone();
            bool isMaximizing = (boardClone.Turn == PieceColor.White);
            Move result = CalculateBestMove(boardClone, depth, int.MinValue, int.MaxValue, isMaximizing);
            return result.Score;
        }

        private Move CalculateBestMove(Board board, int depth, int alpha, int beta, bool isMaximizing)
        {
            if (depth == 0) 
            {
                int eval = Evaluation.Evaluate(board);
                return new Move(eval);
            }

            List<Vector2Int> pieces = GetAllPieces(board, board.Turn);
            Move bestMove = new Move(isMaximizing ? int.MinValue : int.MaxValue);
            bool hasMove = false;

            // TEKRAR KONTROLÜ (Repetition Check)
            if (Arbiter.IsThreefoldRepetition(board))
            {
                int drawScore = isMaximizing ? -_contemptFactor : _contemptFactor;
                return new Move(drawScore);
            }

            foreach (Vector2Int from in pieces)
            {
                var moves = MoveGenerator.GetPseudoLegalMoves(board, from);

                foreach (Vector2Int to in moves)
                {
                    Chess.Architecture.Commands.MoveCommand cmd = 
                        new Chess.Architecture.Commands.MoveCommand(board, from, to, PieceType.Queen);
                    
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
                    return new Move(isMaximizing ? -100000 + depth : 100000 - depth); // Mat
                else
                    return new Move(0); // Pat
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

    // --- EKSİK OLAN YAPI BURAYA EKLENDİ ---
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