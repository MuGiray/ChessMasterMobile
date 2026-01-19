using UnityEngine;
using Chess.Core.Models;

// HATA ÇÖZÜMÜ: Çakışmayı önlemek için Alias ekledik
using Vector2Int = Chess.Core.Models.Vector2Int;

namespace Chess.Core.AI
{
    public static class Evaluation
    {
        // Temel Materyal Puanları
        public const int PawnValue = 100;
        public const int KnightValue = 320;
        public const int BishopValue = 330;
        public const int RookValue = 500;
        public const int QueenValue = 900;
        public const int KingValue = 20000;

        // --- PIECE-SQUARE TABLES (PST) ---
        // Tahtanın "Beyaz" tarafına göre dizilmiştir (A1 sol alt, H8 sağ üst).
        // Siyah için okurken indeksi ters çevireceğiz (Mirroring).

        private static readonly int[] PawnTable = 
        {
             0,  0,  0,  0,  0,  0,  0,  0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
             5,  5, 10, 25, 25, 10,  5,  5,
             0,  0,  0, 20, 20,  0,  0,  0,
             5, -5,-10,  0,  0,-10, -5,  5,
             5, 10, 10,-20,-20, 10, 10,  5,
             0,  0,  0,  0,  0,  0,  0,  0
        };

        private static readonly int[] KnightTable = 
        {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50
        };

        private static readonly int[] BishopTable = 
        {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20
        };

        private static readonly int[] RookTable = 
        {
             0,  0,  0,  0,  0,  0,  0,  0,
             5, 10, 10, 10, 10, 10, 10,  5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
             0,  0,  0,  5,  5,  0,  0,  0
        };

        private static readonly int[] QueenTable = 
        {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20
        };

        private static readonly int[] KingTableMiddle = 
        {
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -20,-30,-30,-40,-40,-30,-30,-20,
            -10,-20,-20,-20,-20,-20,-20,-10,
             20, 20,  0,  0,  0,  0, 20, 20,
             20, 30, 10,  0,  0, 10, 30, 20
        };

        public static int Evaluate(Board board)
        {
            int whiteScore = 0;
            int blackScore = 0;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece p = board.GetPieceAt(new Vector2Int(x, y));
                    if (p.Type == PieceType.None) continue;

                    int score = GetPieceValue(p.Type);

                    int tableIndex = (p.Color == PieceColor.White) 
                        ? (y * 8 + x) 
                        : ((7 - y) * 8 + x);

                    score += GetPositionBonus(p.Type, tableIndex);

                    if (p.Color == PieceColor.White)
                        whiteScore += score;
                    else
                        blackScore += score;
                }
            }

            return whiteScore - blackScore;
        }

        private static int GetPieceValue(PieceType type)
        {
            return type switch
            {
                PieceType.Pawn => PawnValue,
                PieceType.Knight => KnightValue,
                PieceType.Bishop => BishopValue,
                PieceType.Rook => RookValue,
                PieceType.Queen => QueenValue,
                PieceType.King => KingValue,
                _ => 0
            };
        }

        private static int GetPositionBonus(PieceType type, int index)
        {
            return type switch
            {
                PieceType.Pawn => PawnTable[index],
                PieceType.Knight => KnightTable[index],
                PieceType.Bishop => BishopTable[index],
                PieceType.Rook => RookTable[index],
                PieceType.Queen => QueenTable[index],
                PieceType.King => KingTableMiddle[index],
                _ => 0
            };
        }
    }
}