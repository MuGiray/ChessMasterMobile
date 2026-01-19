using System;
using System.Collections.Generic;
using Chess.Core.Models;

namespace Chess.Core.Models
{
    [Serializable]
    public class SaveData
    {
        public string InitialFen;
        public GameMode CurrentMode;
        public List<MoveRecord> MoveHistory;
        
        public float WhiteTimeRemaining;
        public float BlackTimeRemaining;

        public int HalfMoveClock;      
        public int FullMoveNumber;
    }

    [Serializable]
    public struct MoveRecord
    {
        public Vector2Int From;
        public Vector2Int To;
        public PieceType Promotion;
        public string Notation;
        public int EvalScore; // YENİ: Hamle yapıldığı andaki puan (Analiz için)

        public MoveRecord(Vector2Int from, Vector2Int to, PieceType promotion, string notation, int evalScore)
        {
            From = from;
            To = to;
            Promotion = promotion;
            Notation = notation;
            EvalScore = evalScore;
        }
    }
}