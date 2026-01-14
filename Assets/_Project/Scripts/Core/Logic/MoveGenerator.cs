using System.Collections.Generic;
using Chess.Core.Models;

namespace Chess.Core.Logic
{
    public static class MoveGenerator
    {
        // Yön Vektörleri (Direction Vectors) - Sabitler
        private static readonly Vector2Int[] RookDirections = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        private static readonly Vector2Int[] BishopDirections = { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };
        private static readonly Vector2Int[] KnightMoves = { 
            new Vector2Int(1, 2), new Vector2Int(1, -2), new Vector2Int(-1, 2), new Vector2Int(-1, -2),
            new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(-2, 1), new Vector2Int(-2, -1) 
        };

        // Ana Metod: Bir taşın gidebileceği TÜM kareleri döndürür.
        public static List<Vector2Int> GetPseudoLegalMoves(Board board, Vector2Int from)
        {
            List<Vector2Int> moves = new List<Vector2Int>(); // Object Pooling ile optimize edilebilir, şimdilik List.
            Piece piece = board.GetPieceAt(from);

            if (piece.Type == PieceType.None) return moves;

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    GetPawnMoves(board, from, piece, moves);
                    break;
                case PieceType.Knight:
                    GetSteppingMoves(board, from, piece, moves, KnightMoves);
                    break;
                case PieceType.Bishop:
                    GetSlidingMoves(board, from, piece, moves, BishopDirections);
                    break;
                case PieceType.Rook:
                    GetSlidingMoves(board, from, piece, moves, RookDirections);
                    break;
                case PieceType.Queen:
                    GetSlidingMoves(board, from, piece, moves, RookDirections);   // Kale gibi
                    GetSlidingMoves(board, from, piece, moves, BishopDirections); // Fil gibi
                    break;
                case PieceType.King:
                    GetSteppingMoves(board, from, piece, moves, RookDirections);   // Düz 1 adım
                    GetSteppingMoves(board, from, piece, moves, BishopDirections); // Çapraz 1 adım
                    break;
            }

            return moves;
        }

        // --- HAREKET MANTIKLARI ---

        // 1. Sliding Pieces (Kale, Fil, Vezir): Engel çıkana kadar gider.
        private static void GetSlidingMoves(Board board, Vector2Int from, Piece piece, List<Vector2Int> moves, Vector2Int[] dirs)
        {
            foreach (var dir in dirs)
            {
                for (int i = 1; i < 8; i++) // Maksimum 7 kare gidebilir
                {
                    Vector2Int target = new Vector2Int(from.x + dir.x * i, from.y + dir.y * i);

                    if (!IsInsideBoard(target)) break; // Tahta dışına çıktı

                    Piece targetPiece = board.GetPieceAt(target);

                    if (targetPiece.Type == PieceType.None)
                    {
                        moves.Add(target); // Boş kare, devam et
                    }
                    else
                    {
                        if (targetPiece.Color != piece.Color)
                        {
                            moves.Add(target); // Rakip taş, ye ve dur
                        }
                        break; // Kendi taşın veya rakip taş, yol bitti.
                    }
                }
            }
        }

        // 2. Stepping Pieces (At, Şah): Tek hamlelik sıçrama.
        private static void GetSteppingMoves(Board board, Vector2Int from, Piece piece, List<Vector2Int> moves, Vector2Int[] offsets)
        {
            foreach (var offset in offsets)
            {
                Vector2Int target = new Vector2Int(from.x + offset.x, from.y + offset.y);

                if (!IsInsideBoard(target)) continue;

                Piece targetPiece = board.GetPieceAt(target);

                // Boşsa veya rakipse gidebilir
                if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color)
                {
                    moves.Add(target);
                }
            }
        }

        // 3. Pawn (Piyon): Biraz karışık (İleri 1, İleri 2, Çapraz Yeme)
        private static void GetPawnMoves(Board board, Vector2Int from, Piece piece, List<Vector2Int> moves)
        {
            int direction = piece.IsWhite ? 1 : -1; // Beyaz yukarı (+1), Siyah aşağı (-1)
            int startRow = piece.IsWhite ? 1 : 6;

            // A. Tek Adım İleri
            Vector2Int forwardOne = new Vector2Int(from.x, from.y + direction);
            if (IsInsideBoard(forwardOne) && board.GetPieceAt(forwardOne).Type == PieceType.None)
            {
                moves.Add(forwardOne);

                // B. İki Adım İleri (Sadece başlangıç karesindeyse ve önü boşsa)
                if (from.y == startRow)
                {
                    Vector2Int forwardTwo = new Vector2Int(from.x, from.y + direction * 2);
                    if (IsInsideBoard(forwardTwo) && board.GetPieceAt(forwardTwo).Type == PieceType.None)
                    {
                        moves.Add(forwardTwo);
                    }
                }
            }

            // C. Çapraz Yeme (Capture)
            Vector2Int[] captureOffsets = { new Vector2Int(-1, direction), new Vector2Int(1, direction) };
            foreach (var offset in captureOffsets)
            {
                Vector2Int target = new Vector2Int(from.x + offset.x, from.y + offset.y);
                if (IsInsideBoard(target))
                {
                    Piece targetPiece = board.GetPieceAt(target);
                    if (targetPiece.Type != PieceType.None && targetPiece.Color != piece.Color)
                    {
                        moves.Add(target);
                    }
                }
            }
        }

        private static bool IsInsideBoard(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < 8 && coord.y >= 0 && coord.y < 8;
        }
    }
}