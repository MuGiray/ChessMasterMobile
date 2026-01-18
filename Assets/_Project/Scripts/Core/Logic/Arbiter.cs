using System.Collections.Generic;
using Chess.Core.Models;

namespace Chess.Core.Logic
{
    public static class Arbiter
    {
        public static List<Vector2Int> GetLegalMoves(Board board, Vector2Int from)
        {
            List<Vector2Int> pseudoMoves = MoveGenerator.GetPseudoLegalMoves(board, from);
            // Capacity belirterek re-allocation'ı önlüyoruz
            List<Vector2Int> legalMoves = new List<Vector2Int>(pseudoMoves.Count);

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
            // Mat/Pat kontrolü için herhangi bir yasal hamle var mı diye bak
            // Bu kısım mecburen döngü, ama MoveGenerator optimize olduğu için hızlı.
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
                            goto EndLoop; // Hızlı çıkış
                        }
                    }
                }
            }
            
            EndLoop:
            if (hasLegalMove) return GameState.InProgress;

            // OPTİMİZASYON: FindKing yerine Cached Position kullanıyoruz.
            // O(64) -> O(1)
            Vector2Int kingPos = (board.Turn == PieceColor.White) ? board.WhiteKingPos : board.BlackKingPos;
            
            PieceColor opponentColor = (board.Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            
            // Eğer şahın yeri geçerliyse ve saldırı altındaysa -> MAT
            if (kingPos.x != -1 && MoveGenerator.IsSquareAttacked(board, kingPos, opponentColor))
            {
                return GameState.Checkmate;
            }
            
            return GameState.Stalemate;
        }
        
        // Bu metoda da dışarıdan erişilebilmesi için public yaptım (GameManager kullanıyordu)
        public static bool IsInCheck(Board board, PieceColor kingColor)
        {
            Vector2Int kingPos = (kingColor == PieceColor.White) ? board.WhiteKingPos : board.BlackKingPos;
            if (kingPos.x == -1) return false;

            PieceColor opponentColor = (kingColor == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            return MoveGenerator.IsSquareAttacked(board, kingPos, opponentColor);
        }

        private static bool IsMoveSafe(Board board, Vector2Int from, Vector2Int to)
        {
            Piece movedPiece = board.GetPieceAt(from);
            Piece capturedPiece = board.GetPieceAt(to);
            
            // Simülasyon (SetPieceAt, Cache'i otomatik günceller)
            board.SetPieceAt(to, movedPiece);
            board.SetPieceAt(from, new Piece(PieceType.None, PieceColor.None));
            
            // Eğer hareket eden Şah ise, zaten 'to' konumuna gitmiştir.
            // Değilse cache'deki yerini al.
            Vector2Int kingPos = (movedPiece.Type == PieceType.King) ? to : 
                                 ((movedPiece.Color == PieceColor.White) ? board.WhiteKingPos : board.BlackKingPos);
            
            PieceColor opponentColor = (movedPiece.Color == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            
            bool isCheck = MoveGenerator.IsSquareAttacked(board, kingPos, opponentColor);

            // Geri Al (SetPieceAt, Cache'i otomatik eski haline döndürür)
            board.SetPieceAt(from, movedPiece);
            board.SetPieceAt(to, capturedPiece);

            return !isCheck;
        }
        
        // FindKing metoduna artık ihtiyacımız kalmadı, silebiliriz.
    }
}