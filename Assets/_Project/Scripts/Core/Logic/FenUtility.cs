using System.Collections.Generic;
using Chess.Core.Models;

namespace Chess.Core.Logic
{
    public static class FenUtility
    {
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static void LoadPositionFromFen(Board board, string fen)
        {
            board.Clear();
            string[] sections = fen.Split(' ');
            
            // 1. Taşlar
            LoadPiecePlacement(board, sections[0]);

            // 2. Sıra
            if (sections.Length > 1)
                board.Turn = (sections[1] == "b") ? PieceColor.Black : PieceColor.White;

            // 3. Rok
            board.CurrentCastlingRights = CastlingRights.None;
            if (sections.Length > 2 && sections[2] != "-")
            {
                string c = sections[2];
                if (c.Contains("K")) board.CurrentCastlingRights |= CastlingRights.WhiteKingSide;
                if (c.Contains("Q")) board.CurrentCastlingRights |= CastlingRights.WhiteQueenSide;
                if (c.Contains("k")) board.CurrentCastlingRights |= CastlingRights.BlackKingSide;
                if (c.Contains("q")) board.CurrentCastlingRights |= CastlingRights.BlackQueenSide;
            }

            // 4. En Passant
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
        }

        private static void LoadPiecePlacement(Board board, string data)
        {
            int rank = 7;
            int file = 0;

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
                        file += (int)char.GetNumericValue(symbol);
                    }
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
            return symbol switch
            {
                'p' => PieceType.Pawn,
                'r' => PieceType.Rook,
                'n' => PieceType.Knight,
                'b' => PieceType.Bishop,
                'q' => PieceType.Queen,
                'k' => PieceType.King,
                _ => PieceType.None
            };
        }
    }
}