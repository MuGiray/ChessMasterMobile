using System.Collections.Generic;
using Chess.Core.Models;

namespace Chess.Core.Logic
{
    public static class MoveGenerator
    {
        private static readonly Vector2Int[] RookDirections = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        private static readonly Vector2Int[] BishopDirections = { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };
        private static readonly Vector2Int[] KnightMoves = { 
            new Vector2Int(1, 2), new Vector2Int(1, -2), new Vector2Int(-1, 2), new Vector2Int(-1, -2),
            new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(-2, 1), new Vector2Int(-2, -1) 
        };

        public static List<Vector2Int> GetPseudoLegalMoves(Board board, Vector2Int from)
        {
            List<Vector2Int> moves = new List<Vector2Int>();
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
                    GetSlidingMoves(board, from, piece, moves, RookDirections);
                    GetSlidingMoves(board, from, piece, moves, BishopDirections);
                    break;
                case PieceType.King:
                    GetSteppingMoves(board, from, piece, moves, RookDirections);
                    GetSteppingMoves(board, from, piece, moves, BishopDirections);
                    GetCastlingMoves(board, from, piece, moves); // YENİ: Rok Kontrolü
                    break;
            }

            return moves;
        }

        // --- YENİ: ROK MANTIĞI ---
        private static void GetCastlingMoves(Board board, Vector2Int from, Piece king, List<Vector2Int> moves)
        {
            if (king.Type != PieceType.King) return;

            // Şah tehdit altındaysa rok atılamaz
            if (IsSquareAttacked(board, from, GetOpponentColor(king.Color))) return;

            int rank = king.IsWhite ? 0 : 7;
            CastlingRights myRights = board.CurrentCastlingRights;

            // KISA ROK (King Side)
            CastlingRights kSide = king.IsWhite ? CastlingRights.WhiteKingSide : CastlingRights.BlackKingSide;
            if ((myRights & kSide) != 0)
            {
                // Aradaki kareler boş mu? (f ve g sütunları)
                if (board.GetPieceAt(new Vector2Int(5, rank)).Type == PieceType.None &&
                    board.GetPieceAt(new Vector2Int(6, rank)).Type == PieceType.None)
                {
                    // Şahın geçeceği kare (f) ve gideceği kare (g) tehdit altında mı?
                    if (!IsSquareAttacked(board, new Vector2Int(5, rank), GetOpponentColor(king.Color)) &&
                        !IsSquareAttacked(board, new Vector2Int(6, rank), GetOpponentColor(king.Color)))
                    {
                        moves.Add(new Vector2Int(6, rank));
                    }
                }
            }

            // UZUN ROK (Queen Side)
            CastlingRights qSide = king.IsWhite ? CastlingRights.WhiteQueenSide : CastlingRights.BlackQueenSide;
            if ((myRights & qSide) != 0)
            {
                // Aradaki kareler boş mu? (d, c, b sütunları)
                if (board.GetPieceAt(new Vector2Int(3, rank)).Type == PieceType.None &&
                    board.GetPieceAt(new Vector2Int(2, rank)).Type == PieceType.None &&
                    board.GetPieceAt(new Vector2Int(1, rank)).Type == PieceType.None)
                {
                    // Şahın geçeceği (d) ve gideceği (c) kare tehdit altında mı? (b karesi tehdit altında olabilir, kural yok)
                    if (!IsSquareAttacked(board, new Vector2Int(3, rank), GetOpponentColor(king.Color)) &&
                        !IsSquareAttacked(board, new Vector2Int(2, rank), GetOpponentColor(king.Color)))
                    {
                        moves.Add(new Vector2Int(2, rank));
                    }
                }
            }
        }

        // --- GÜNCELLENEN: PİYON MANTIĞI (En Passant Dahil) ---
        private static void GetPawnMoves(Board board, Vector2Int from, Piece piece, List<Vector2Int> moves)
        {
            int direction = piece.IsWhite ? 1 : -1;
            int startRow = piece.IsWhite ? 1 : 6;

            // A. İleri Hareket (Aynı)
            Vector2Int forwardOne = new Vector2Int(from.x, from.y + direction);
            if (IsInsideBoard(forwardOne) && board.GetPieceAt(forwardOne).Type == PieceType.None)
            {
                moves.Add(forwardOne);
                if (from.y == startRow)
                {
                    Vector2Int forwardTwo = new Vector2Int(from.x, from.y + direction * 2);
                    if (IsInsideBoard(forwardTwo) && board.GetPieceAt(forwardTwo).Type == PieceType.None)
                    {
                        moves.Add(forwardTwo);
                    }
                }
            }

            // B. Çapraz Yeme & EN PASSANT
            Vector2Int[] captureOffsets = { new Vector2Int(-1, direction), new Vector2Int(1, direction) };
            foreach (var offset in captureOffsets)
            {
                Vector2Int target = new Vector2Int(from.x + offset.x, from.y + offset.y);
                if (IsInsideBoard(target))
                {
                    Piece targetPiece = board.GetPieceAt(target);
                    
                    // Normal Yeme
                    if (targetPiece.Type != PieceType.None && targetPiece.Color != piece.Color)
                    {
                        moves.Add(target);
                    }
                    // YENİ: En Passant
                    else if (board.EnPassantSquare.HasValue && target.x == board.EnPassantSquare.Value.x && target.y == board.EnPassantSquare.Value.y)
                    {
                        moves.Add(target);
                    }
                }
            }
        }

        // --- YENİ: TEHDİT ANALİZİ (IsSquareAttacked) ---
        // Bu metod, "square" karesine "attackerColor" tarafından saldırı olup olmadığını kontrol eder.
        public static bool IsSquareAttacked(Board board, Vector2Int square, PieceColor attackerColor)
        {
            // Mantık: Sanki "square" karesinde bir Süper Taş varmış gibi düşünüp, 
            // At, Kale, Fil, Vezir, Piyon ve Şah hamlelerini tersine tarıyoruz.
            
            // 1. Piyon Tehdidi (Tersten bakıyoruz: Saldıran piyon nerede olur?)
            int pawnDir = (attackerColor == PieceColor.White) ? -1 : 1; // Saldıran beyazsa aşağıdan gelir
            if (CheckPiece(board, square, new Vector2Int(1, pawnDir), PieceType.Pawn, attackerColor)) return true;
            if (CheckPiece(board, square, new Vector2Int(-1, pawnDir), PieceType.Pawn, attackerColor)) return true;

            // 2. At Tehdidi
            foreach(var move in KnightMoves) 
                if (CheckPiece(board, square, move, PieceType.Knight, attackerColor)) return true;

            // 3. Şah Tehdidi (Bitişik kareler)
            foreach (var dir in RookDirections) if (CheckPiece(board, square, dir, PieceType.King, attackerColor)) return true;
            foreach (var dir in BishopDirections) if (CheckPiece(board, square, dir, PieceType.King, attackerColor)) return true;

            // 4. Sliding (Kale/Vezir)
            if (CheckSlidingAttack(board, square, RookDirections, PieceType.Rook, attackerColor)) return true;

            // 5. Sliding (Fil/Vezir)
            if (CheckSlidingAttack(board, square, BishopDirections, PieceType.Bishop, attackerColor)) return true;

            return false;
        }

        // Yardımcı Metod: Belirli bir noktada belirli bir taş var mı?
        private static bool CheckPiece(Board board, Vector2Int origin, Vector2Int offset, PieceType type, PieceColor color)
        {
            Vector2Int pos = new Vector2Int(origin.x + offset.x, origin.y + offset.y);
            if (!IsInsideBoard(pos)) return false;
            Piece p = board.GetPieceAt(pos);
            return p.Type == type && p.Color == color;
        }

        // Yardımcı Metod: Kayarak gelen tehdit (Vezir dahil)
        private static bool CheckSlidingAttack(Board board, Vector2Int from, Vector2Int[] dirs, PieceType type, PieceColor color)
        {
            foreach (var dir in dirs)
            {
                for (int i = 1; i < 8; i++)
                {
                    Vector2Int target = new Vector2Int(from.x + dir.x * i, from.y + dir.y * i);
                    if (!IsInsideBoard(target)) break;

                    Piece p = board.GetPieceAt(target);
                    if (p.Type != PieceType.None)
                    {
                        // Eğer o taş "Saldırgan" renkteyse ve (Türü bizim aradığımızsa VEYA Vezirse) -> TEHDİT VAR
                        if (p.Color == color && (p.Type == type || p.Type == PieceType.Queen)) return true;
                        
                        break; // Başka bir taş yolu kesti
                    }
                }
            }
            return false;
        }

        private static PieceColor GetOpponentColor(PieceColor color) => color == PieceColor.White ? PieceColor.Black : PieceColor.White;

        // --- MEVCUT YARDIMCI METODLAR (Aynen Kalıyor) ---
        private static void GetSlidingMoves(Board board, Vector2Int from, Piece piece, List<Vector2Int> moves, Vector2Int[] dirs)
        {
            foreach (var dir in dirs)
            {
                for (int i = 1; i < 8; i++)
                {
                    Vector2Int target = new Vector2Int(from.x + dir.x * i, from.y + dir.y * i);
                    if (!IsInsideBoard(target)) break; 
                    Piece targetPiece = board.GetPieceAt(target);
                    if (targetPiece.Type == PieceType.None) { moves.Add(target); }
                    else { if (targetPiece.Color != piece.Color) moves.Add(target); break; }
                }
            }
        }

        private static void GetSteppingMoves(Board board, Vector2Int from, Piece piece, List<Vector2Int> moves, Vector2Int[] offsets)
        {
            foreach (var offset in offsets)
            {
                Vector2Int target = new Vector2Int(from.x + offset.x, from.y + offset.y);
                if (!IsInsideBoard(target)) continue;
                Piece targetPiece = board.GetPieceAt(target);
                if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color) moves.Add(target);
            }
        }

        private static bool IsInsideBoard(Vector2Int coord) => coord.x >= 0 && coord.x < 8 && coord.y >= 0 && coord.y < 8;
    }
}