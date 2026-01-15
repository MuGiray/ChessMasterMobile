using System.Collections.Generic;
using Chess.Core.Models;

namespace Chess.Core.Logic
{
    public static class Arbiter
    {
        public static List<Vector2Int> GetLegalMoves(Board board, Vector2Int from)
        {
            List<Vector2Int> pseudoMoves = MoveGenerator.GetPseudoLegalMoves(board, from);
            List<Vector2Int> legalMoves = new List<Vector2Int>();

            foreach (var to in pseudoMoves)
            {
                if (IsMoveSafe(board, from, to))
                {
                    legalMoves.Add(to);
                }
            }
            return legalMoves;
        }

        public static GameState CheckGameState(Board board)
        {
            bool hasLegalMove = false;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece piece = board.GetPieceAt(new Vector2Int(x, y));
                    if (piece.Type != PieceType.None && piece.Color == board.Turn)
                    {
                        if (GetLegalMoves(board, new Vector2Int(x, y)).Count > 0)
                        {
                            hasLegalMove = true;
                            break;
                        }
                    }
                }
                if (hasLegalMove) break;
            }

            if (hasLegalMove) return GameState.InProgress;

            Vector2Int kingPos = FindKing(board, board.Turn);
            PieceColor opponentColor = (board.Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            
            if (MoveGenerator.IsSquareAttacked(board, kingPos, opponentColor))
            {
                return GameState.Checkmate;
            }
            else
            {
                return GameState.Stalemate;
            }
        }
        
        // --- KRİTİK DÜZELTME BURADA ---
        private static bool IsMoveSafe(Board board, Vector2Int from, Vector2Int to)
        {
            Piece movedPiece = board.GetPieceAt(from);
            Piece capturedPiece = board.GetPieceAt(to);
            
            // 1. Hamleyi Yap (MANUEL SİMÜLASYON)
            // board.MovePiece() KULLANMIYORUZ! Çünkü o sırayı (Turn) değiştiriyor.
            // Biz sadece taşların yerini değiştirip bakacağız, sırayı bozmayacağız.
            board.SetPieceAt(to, movedPiece);
            board.SetPieceAt(from, new Piece(PieceType.None, PieceColor.None));
            
            // 2. Kontrol Et: Şahım tehdit altında mı?
            Vector2Int kingPos = (movedPiece.Type == PieceType.King) ? to : FindKing(board, movedPiece.Color);
            PieceColor opponentColor = (movedPiece.Color == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            
            bool isCheck = MoveGenerator.IsSquareAttacked(board, kingPos, opponentColor);

            // 3. Hamleyi Geri Al (Undo)
            board.SetPieceAt(from, movedPiece);
            board.SetPieceAt(to, capturedPiece);

            return !isCheck; // Şah çekilmiyorsa güvenlidir.
        }

        private static Vector2Int FindKing(Board board, PieceColor color)
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece p = board.GetPieceAt(new Vector2Int(x, y));
                    if (p.Type == PieceType.King && p.Color == color) return new Vector2Int(x, y);
                }
            }
            return new Vector2Int(-1, -1);
        }
    }
}