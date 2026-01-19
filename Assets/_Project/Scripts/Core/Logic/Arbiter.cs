using System.Collections.Generic;
using Chess.Core.Models;

namespace Chess.Core.Logic
{
    public static class Arbiter
    {
        public static List<Vector2Int> GetLegalMoves(Board board, Vector2Int from)
        {
            List<Vector2Int> pseudoMoves = MoveGenerator.GetPseudoLegalMoves(board, from);
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
            // 1. 50 Hamle Kuralı
            if (IsDrawByFiftyMoveRule(board)) return GameState.Draw;

            // 2. 3 Konum Tekrarı
            if (IsThreefoldRepetition(board)) return GameState.Draw;

            // 3. Mat / Pat Kontrolü
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
                            goto EndLoop;
                        }
                    }
                }
            }
            
            EndLoop:
            if (hasLegalMove) return GameState.InProgress;

            if (IsInCheck(board, board.Turn))
            {
                return GameState.Checkmate;
            }
            return GameState.Stalemate;
        }

        // --- YENİ BERABERLİK KURALLARI ---

        public static bool IsThreefoldRepetition(Board board)
        {
            if (board.History.Count < 3) return false;

            ulong currentHash = board.ZobristKey;
            int repetitionCount = 0;

            // Tarihçede geriye doğru bak (Genelde yakın zamanda olur)
            for (int i = board.History.Count - 1; i >= 0; i--)
            {
                if (board.History[i] == currentHash)
                {
                    repetitionCount++;
                    if (repetitionCount >= 3) return true; // 3. kez aynı konum
                }
            }
            return false;
        }

        public static bool IsDrawByFiftyMoveRule(Board board)
        {
            // 100 Yarım Hamle = 50 Tam Hamle
            return board.HalfMoveClock >= 100;
        }

        // --- MEVCUT METODLAR ---
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
            
            // Simülasyon (Zobrist güncellemelerine dikkat, burada sadece check bakıyoruz)
            // Performans için Zobrist update'i umursamadan grid değişimi yapabiliriz
            // Ama Board.SetPieceAt zaten update yapıyor, sorun yok.
            
            board.SetPieceAt(to, movedPiece);
            board.SetPieceAt(from, new Piece(PieceType.None, PieceColor.None));
            
            Vector2Int kingPos = (movedPiece.Type == PieceType.King) ? to : 
                                 ((movedPiece.Color == PieceColor.White) ? board.WhiteKingPos : board.BlackKingPos);
            
            PieceColor opponentColor = (movedPiece.Color == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            bool isCheck = MoveGenerator.IsSquareAttacked(board, kingPos, opponentColor);

            // Geri Al
            board.SetPieceAt(from, movedPiece);
            board.SetPieceAt(to, capturedPiece);

            return !isCheck;
        }
    }
}