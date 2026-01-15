using System.Collections.Generic;
using Chess.Core.Models;

namespace Chess.Core.Logic
{
    public static class FenUtility
    {
        // Standart Başlangıç Pozisyonu
        // Standart FEN (Tam hali)
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static void LoadPositionFromFen(Board board, string fen)
        {
            board.Clear();
            string[] sections = fen.Split(' ');
            
            // 1. Taşlar
            LoadPiecePlacement(board, sections[0]);

            // 2. Sıra
            board.Turn = (sections.Length > 1 && sections[1] == "b") ? PieceColor.Black : PieceColor.White;

            // 3. Rok Hakları (Castling)
            board.CurrentCastlingRights = CastlingRights.None;
            if (sections.Length > 2 && sections[2] != "-")
            {
                if (sections[2].Contains("K")) board.CurrentCastlingRights |= CastlingRights.WhiteKingSide;
                if (sections[2].Contains("Q")) board.CurrentCastlingRights |= CastlingRights.WhiteQueenSide;
                if (sections[2].Contains("k")) board.CurrentCastlingRights |= CastlingRights.BlackKingSide;
                if (sections[2].Contains("q")) board.CurrentCastlingRights |= CastlingRights.BlackQueenSide;
            }

            // 4. En Passant Karesi
            board.EnPassantSquare = null;
            if (sections.Length > 3 && sections[3] != "-")
            {
                // Örn: "e3" -> Vector2Int
                string epString = sections[3];
                int file = epString[0] - 'a';
                int rank = epString[1] - '1';
                board.EnPassantSquare = new Vector2Int(file, rank);
            }

            // 5. Half Move (50 Hamle Kuralı)
            int halfMove = 0; // Varsayılan değer
            if (sections.Length > 4) 
            {
                int.TryParse(sections[4], out halfMove);
            }
            board.HalfMoveClock = halfMove;

            // 6. Full Move (Hamle Sayısı)
            int fullMove = 1; // Varsayılan değer 1'dir
            if (sections.Length > 5) 
            {
                int.TryParse(sections[5], out fullMove);
            }
            board.FullMoveNumber = fullMove;
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