using UnityEngine;
using Chess.Core.Models;
using Chess.Architecture.Commands; // EKLENDİ: MoveCommand burada
using System.Collections.Generic;

// EKLENDİ: Çakışmayı önlemek için bizim Vector2Int'i varsayılan yapıyoruz
using Vector2Int = Chess.Core.Models.Vector2Int; 

namespace Chess.Core.Logic
{
    public static class NotationConverter
    {
        private static readonly string[] Files = { "a", "b", "c", "d", "e", "f", "g", "h" };

        public static string EncodeMove(Board board, MoveCommand move)
        {
            // 1. ROK (Özel Durum)
            Piece movedPiece = board.GetPieceAt(move.From);
            if (movedPiece.Type == PieceType.King && Mathf.Abs(move.From.x - move.To.x) == 2)
            {
                return (move.To.x > move.From.x) ? "O-O" : "O-O-O";
            }

            string notation = "";

            // 2. TAŞ İSMİ (Piyon hariç)
            if (movedPiece.Type != PieceType.Pawn)
            {
                notation += GetPieceLetter(movedPiece.Type);
                
                // DISAMBIGUATION (Çakışma Çözümü)
                notation += GetDisambiguation(board, move);
            }

            // 3. YEME İŞARETİ (x)
            Piece targetPiece = board.GetPieceAt(move.To);
            
            // En Passant kontrolü
            bool isEnPassant = (movedPiece.Type == PieceType.Pawn && move.From.x != move.To.x && targetPiece.Type == PieceType.None);
            
            if (targetPiece.Type != PieceType.None || isEnPassant)
            {
                if (movedPiece.Type == PieceType.Pawn)
                {
                    notation += Files[move.From.x]; // Piyon yerken sütun söyler: "exd5"
                }
                notation += "x";
            }

            // 4. HEDEF KARE
            notation += Files[move.To.x] + (move.To.y + 1);

            // 5. PROMOTION
            if (move.PromotionType != PieceType.None && move.PromotionType != PieceType.Queen) 
            {
                int lastRank = movedPiece.IsWhite ? 7 : 0;
                if (movedPiece.Type == PieceType.Pawn && move.To.y == lastRank)
                {
                    notation += "=" + GetPieceLetter(move.PromotionType);
                }
            }
            else if (movedPiece.Type == PieceType.Pawn) // Parametre Queen olsa bile piyon son karedeyse
            {
                int lastRank = movedPiece.IsWhite ? 7 : 0;
                if (move.To.y == lastRank) notation += "=Q";
            }

            return notation;
        }

        private static string GetPieceLetter(PieceType type)
        {
            switch (type)
            {
                case PieceType.King: return "K";
                case PieceType.Queen: return "Q";
                case PieceType.Rook: return "R";
                case PieceType.Bishop: return "B";
                case PieceType.Knight: return "N";
                default: return "";
            }
        }

        private static string GetDisambiguation(Board board, MoveCommand currentMove)
        {
            Piece movedPiece = board.GetPieceAt(currentMove.From);
            List<Vector2Int> candidates = new List<Vector2Int>();

            // Tüm tahtayı tara
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (pos == currentMove.From) continue; // Kendisi hariç

                    Piece p = board.GetPieceAt(pos);
                    if (p.Type == movedPiece.Type && p.Color == movedPiece.Color)
                    {
                        // Bu taş da hedef kareye gidebiliyor mu?
                        var moves = MoveGenerator.GetPseudoLegalMoves(board, pos);
                        if (moves.Contains(currentMove.To))
                        {
                            candidates.Add(pos);
                        }
                    }
                }
            }

            if (candidates.Count == 0) return "";

            // Çakışma var! Ayırt et.
            bool fileMatch = false;
            bool rankMatch = false;

            foreach (var candidate in candidates)
            {
                if (candidate.x == currentMove.From.x) fileMatch = true;
                if (candidate.y == currentMove.From.y) rankMatch = true;
            }

            if (!fileMatch) return Files[currentMove.From.x]; // Sütun farklı: "Nbd7"
            if (!rankMatch) return (currentMove.From.y + 1).ToString(); // Satır farklı: "N1f3"
            
            return Files[currentMove.From.x] + (currentMove.From.y + 1); // İkisi de aynı: "Nb1d2"
        }
    }
}