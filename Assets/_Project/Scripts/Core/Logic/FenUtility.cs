using System.Text; // StringBuilder için gerekli
using Chess.Core.Models;

namespace Chess.Core.Logic
{
    public static class FenUtility
    {
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        // MEVCUT METOD (OKUMA)
        public static void LoadPositionFromFen(Board board, string fen)
        {
            // ... (Burası aynen kalsın, dokunma) ...
            // Mevcut kodu koru. Aşağıya yeni metodu ekle.
            board.Clear();
            string[] sections = fen.Split(' ');
            LoadPiecePlacement(board, sections[0]);
            
            if (sections.Length > 1) board.Turn = (sections[1] == "b") ? PieceColor.Black : PieceColor.White;

            board.CurrentCastlingRights = CastlingRights.None;
            if (sections.Length > 2 && sections[2] != "-")
            {
                string c = sections[2];
                if (c.Contains("K")) board.CurrentCastlingRights |= CastlingRights.WhiteKingSide;
                if (c.Contains("Q")) board.CurrentCastlingRights |= CastlingRights.WhiteQueenSide;
                if (c.Contains("k")) board.CurrentCastlingRights |= CastlingRights.BlackKingSide;
                if (c.Contains("q")) board.CurrentCastlingRights |= CastlingRights.BlackQueenSide;
            }

            board.EnPassantSquare = null;
            if (sections.Length > 3 && sections[3] != "-")
            {
                string ep = sections[3];
                if (ep.Length >= 2)
                {
                    int file = ep[0] - 'a';
                    int rank = ep[1] - '1';
                    board.EnPassantSquare = new Vector2Int(file, rank);
                }
            }
            
            // Half/Full moves parsing eklenebilir ama şu an kritik değil.
        }

        // --- YENİ METOD: OLUŞTURMA (SAVE İÇİN) ---
        public static string GenerateFenFromBoard(Board board)
        {
            StringBuilder sb = new StringBuilder();

            // 1. Taş Dizilimi
            for (int rank = 7; rank >= 0; rank--)
            {
                int emptyCount = 0;
                for (int file = 0; file < 8; file++)
                {
                    Piece piece = board.GetPieceAt(new Vector2Int(file, rank));
                    if (piece.Type == PieceType.None)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            sb.Append(emptyCount);
                            emptyCount = 0;
                        }
                        char code = GetPieceSymbol(piece.Type);
                        if (piece.Color == PieceColor.White) code = char.ToUpper(code);
                        sb.Append(code);
                    }
                }
                if (emptyCount > 0) sb.Append(emptyCount);
                if (rank > 0) sb.Append('/');
            }

            // 2. Sıra
            sb.Append(board.Turn == PieceColor.White ? " w " : " b ");

            // 3. Rok Hakları
            string castling = "";
            if ((board.CurrentCastlingRights & CastlingRights.WhiteKingSide) != 0) castling += "K";
            if ((board.CurrentCastlingRights & CastlingRights.WhiteQueenSide) != 0) castling += "Q";
            if ((board.CurrentCastlingRights & CastlingRights.BlackKingSide) != 0) castling += "k";
            if ((board.CurrentCastlingRights & CastlingRights.BlackQueenSide) != 0) castling += "q";
            if (string.IsNullOrEmpty(castling)) castling = "-";
            sb.Append(castling);

            // 4. En Passant
            sb.Append(" ");
            if (board.EnPassantSquare.HasValue)
            {
                char file = (char)('a' + board.EnPassantSquare.Value.x);
                int rank = board.EnPassantSquare.Value.y + 1;
                sb.Append($"{file}{rank}");
            }
            else
            {
                sb.Append("-");
            }

            // 5. Saatler (Şimdilik varsayılan 0 1)
            sb.Append($" {board.HalfMoveClock} {board.FullMoveNumber}");

            return sb.ToString();
        }

        // --- YARDIMCI METODLAR ---
        private static char GetPieceSymbol(PieceType type)
        {
            return type switch
            {
                PieceType.Pawn => 'p',
                PieceType.Rook => 'r',
                PieceType.Knight => 'n',
                PieceType.Bishop => 'b',
                PieceType.Queen => 'q',
                PieceType.King => 'k',
                _ => ' '
            };
        }

        // (Mevcut LoadPiecePlacement ve GetPieceTypeFromSymbol metodları burada kalmalı)
        private static void LoadPiecePlacement(Board board, string data)
        {
             // ... (Eski kodun aynısı) ...
             int rank = 7;
            int file = 0;
            foreach (char symbol in data)
            {
                if (symbol == '/') { file = 0; rank--; }
                else
                {
                    if (char.IsDigit(symbol)) file += (int)char.GetNumericValue(symbol);
                    else
                    {
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
            // ... (Eski kodun aynısı) ...
            return symbol switch { 'p' => PieceType.Pawn, 'r' => PieceType.Rook, 'n' => PieceType.Knight, 'b' => PieceType.Bishop, 'q' => PieceType.Queen, 'k' => PieceType.King, _ => PieceType.None };
        }
    }
}