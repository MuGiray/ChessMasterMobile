using System;
using System.Collections.Generic;
using Chess.Core.Models; // MoveRecord için gerekli olabilir, yoksa struct aşağıda

namespace Chess.Core.Models
{
    [Serializable]
    public class SaveData
    {
        public string InitialFen;
        public GameMode CurrentMode;
        public List<MoveRecord> MoveHistory;
        
        // Oyuncuların kalan süreleri
        public float WhiteTimeRemaining;
        public float BlackTimeRemaining;

        // --- YENİ EKLENENLER (Draw Rules için) ---
        // Oyun tekrar yüklendiğinde 50 hamle kuralının kaldığı yerden devam etmesi için
        public int HalfMoveClock;      
        public int FullMoveNumber;
    }

    [Serializable]
    public struct MoveRecord
    {
        public Vector2Int From;
        public Vector2Int To;
        public PieceType Promotion;
        public string Notation; // YENİ: "e4", "Nf3", "Qxd5+" gibi metinleri tutacak

        public MoveRecord(Vector2Int from, Vector2Int to, PieceType promotion, string notation)
        {
            From = from;
            To = to;
            Promotion = promotion;
            Notation = notation;
        }
    }
}