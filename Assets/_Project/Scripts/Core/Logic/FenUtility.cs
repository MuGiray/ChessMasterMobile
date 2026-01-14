using System.Collections.Generic;
using Chess.Core.Models;

namespace Chess.Core.Logic
{
    public static class FenUtility
    {
        // Standart Başlangıç Pozisyonu
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static void LoadPositionFromFen(Board board, string fen)
        {
            // 1. Tahtayı temizle
            board.Clear();

            string[] sections = fen.Split(' ');
            
            // BÖLÜM 1: Taş Yerleşimi
            LoadPiecePlacement(board, sections[0]);

            // BÖLÜM 2: Sıra Kimde (w/b)
            if (sections.Length > 1)
            {
                board.Turn = (sections[1] == "w") ? PieceColor.White : PieceColor.Black;
            }
            
            // (İleride Castling ve EnPassant verileri de buradan okunacak)
        }

        private static void LoadPiecePlacement(Board board, string data)
        {
            int rank = 7; // FEN 8. sıradan (Rank 7) başlar, 1. sıraya (Rank 0) iner.
            int file = 0; // Dosya A'dan (0) H'ye (7) gider.

            foreach (char symbol in data)
            {
                if (symbol == '/')
                {
                    file = 0;
                    rank--;
                }
                else
                {
                    if (char.IsDigit(symbol))
                    {
                        // Rakamlar boş kare sayısını belirtir. Örn: '8' -> 8 boş kare.
                        file += (int)char.GetNumericValue(symbol);
                    }
                    else
                    {
                        // Harfler taşları belirtir. Büyük harf: Beyaz, Küçük harf: Siyah.
                        PieceColor color = char.IsUpper(symbol) ? PieceColor.White : PieceColor.Black;
                        PieceType type = GetPieceTypeFromSymbol(char.ToLower(symbol));
                        
                        board.SetPieceAt(new Vector2Int(file, rank), new Piece(type, color));
                        file++;
                    }
                }
            }
        }

        private static PieceType GetPieceTypeFromSymbol(char symbol)
        {
            switch (symbol)
            {
                case 'p': return PieceType.Pawn;
                case 'r': return PieceType.Rook;
                case 'n': return PieceType.Knight;
                case 'b': return PieceType.Bishop;
                case 'q': return PieceType.Queen;
                case 'k': return PieceType.King;
                default: return PieceType.None;
            }
        }
    }
}