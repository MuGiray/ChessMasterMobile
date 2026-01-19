using UnityEngine;
using Chess.Core.Models;

namespace Chess.Core.Logic
{
    public static class Zobrist
    {
        // 64 Kare x 12 Taş Tipi (6 Beyaz + 6 Siyah)
        public static readonly ulong[,] PiecesArray = new ulong[64, 12];
        
        // Sıra (Siyah için)
        public static readonly ulong SideToMove;
        
        // Rok Hakları (16 olasılık)
        public static readonly ulong[] CastlingRights = new ulong[16];
        
        // En Passant (8 dosya)
        public static readonly ulong[] EnPassantFile = new ulong[9]; // 8 dosya + 1 (yok)

        static Zobrist()
        {
            // Rastgele sayıları üret (Seed sabitliyoruz ki her seferinde aynı olsun)
            System.Random rng = new System.Random(123456789);

            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    PiecesArray[i, j] = Random64(rng);
                }
            }

            SideToMove = Random64(rng);

            for (int i = 0; i < 16; i++)
            {
                CastlingRights[i] = Random64(rng);
            }

            for (int i = 0; i < 9; i++)
            {
                EnPassantFile[i] = Random64(rng);
            }
        }

        private static ulong Random64(System.Random rng)
        {
            byte[] buffer = new byte[8];
            rng.NextBytes(buffer);
            return System.BitConverter.ToUInt64(buffer, 0);
        }

        // --- YARDIMCI METODLAR ---
        
        // Taş tipini 0-11 arası indexe çevirir
        public static int GetPieceIndex(Piece piece)
        {
            // (PieceType - 1) + (IsWhite ? 0 : 6)
            // Pawn(1) -> 0 (White), 6 (Black)
            return ((int)piece.Type - 1) + (piece.Color == PieceColor.White ? 0 : 6);
        }
    }
}