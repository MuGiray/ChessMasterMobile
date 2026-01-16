using System;
using System.Collections.Generic;

namespace Chess.Core.Models
{
    [Serializable]
    public class SaveData
    {
        public string InitialFen; // Oyunun başlangıç konumu (Replay için şart)
        public GameMode CurrentMode;
        public List<MoveRecord> MoveHistory; // Hamle geçmişi
    }

    [Serializable]
    public struct MoveRecord
    {
        public Vector2Int From;
        public Vector2Int To;
        public PieceType Promotion; // Terfi varsa ne olduğu

        public MoveRecord(Vector2Int from, Vector2Int to, PieceType promotion)
        {
            From = from;
            To = to;
            Promotion = promotion;
        }
    }
}