using System;
using Chess.Core.Models;

namespace Chess.Architecture.Commands
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    public class MoveCommand : ICommand
    {
        private readonly Board _board;
        private readonly Vector2Int _from;
        private readonly Vector2Int _to;
        
        // State Snapshot (Undo için)
        private readonly Piece _movedPiece;
        private readonly Piece _capturedPiece;
        private readonly CastlingRights _oldCastlingRights;
        private readonly Vector2Int? _oldEnPassantSquare;
        private readonly int _oldHalfMoveClock;

        private bool _isEnPassantMove;

        public MoveCommand(Board board, Vector2Int from, Vector2Int to)
        {
            _board = board;
            _from = from;
            _to = to;
            _movedPiece = board.GetPieceAt(from);
            _capturedPiece = board.GetPieceAt(to);
            
            _oldCastlingRights = board.CurrentCastlingRights;
            _oldEnPassantSquare = board.EnPassantSquare;
            _oldHalfMoveClock = board.HalfMoveClock;
        }

        public void Execute()
        {
            // 1. Move
            _board.MovePiece(_from, _to);

            // 2. En Passant Capture
            if (_movedPiece.Type == PieceType.Pawn && _from.x != _to.x && _capturedPiece.Type == PieceType.None)
            {
                _isEnPassantMove = true;
                int direction = _movedPiece.IsWhite ? 1 : -1;
                Vector2Int capturedPawnPos = new Vector2Int(_to.x, _to.y - direction);
                _board.SetPieceAt(capturedPawnPos, new Piece(PieceType.None, PieceColor.None));
            }

            // 3. Promotion (Auto Queen)
            int lastRank = _movedPiece.IsWhite ? 7 : 0;
            if (_movedPiece.Type == PieceType.Pawn && _to.y == lastRank)
            {
                _board.SetPieceAt(_to, new Piece(PieceType.Queen, _movedPiece.Color));
            }

            // 4. Castling
            if (_movedPiece.Type == PieceType.King && Math.Abs(_from.x - _to.x) == 2)
            {
                int rank = _movedPiece.IsWhite ? 0 : 7;
                bool isKingSide = _to.x > _from.x; 
                Vector2Int rookFrom = isKingSide ? new Vector2Int(7, rank) : new Vector2Int(0, rank);
                Vector2Int rookTo = isKingSide ? new Vector2Int(5, rank) : new Vector2Int(3, rank);
                
                _board.MovePiece(rookFrom, rookTo);
                
                // Kale hareketi sırayı değiştirdiği için geri düzeltiyoruz
                _board.Turn = (_board.Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            }

            UpdateBoardState();
        }

        public void Undo()
        {
            // 1. Revert Move
            _board.SetPieceAt(_from, _movedPiece);
            _board.SetPieceAt(_to, _capturedPiece);

            // 2. Revert En Passant
            if (_isEnPassantMove)
            {
                int direction = _movedPiece.IsWhite ? 1 : -1;
                Vector2Int capturedPawnPos = new Vector2Int(_to.x, _to.y - direction);
                PieceColor enemyColor = _movedPiece.IsWhite ? PieceColor.Black : PieceColor.White;
                _board.SetPieceAt(capturedPawnPos, new Piece(PieceType.Pawn, enemyColor));
            }

            // 3. Revert Castling
            if (_movedPiece.Type == PieceType.King && Math.Abs(_from.x - _to.x) == 2)
            {
                int rank = _movedPiece.IsWhite ? 0 : 7;
                bool isKingSide = _to.x > _from.x;
                Vector2Int rookFrom = isKingSide ? new Vector2Int(7, rank) : new Vector2Int(0, rank);
                Vector2Int rookTo = isKingSide ? new Vector2Int(5, rank) : new Vector2Int(3, rank);
                
                Piece rook = _board.GetPieceAt(rookTo);
                _board.SetPieceAt(rookFrom, rook);
                _board.SetPieceAt(rookTo, new Piece(PieceType.None, PieceColor.None));
            }

            // 4. Restore State
            _board.CurrentCastlingRights = _oldCastlingRights;
            _board.EnPassantSquare = _oldEnPassantSquare;
            _board.HalfMoveClock = _oldHalfMoveClock;
            _board.Turn = (_board.Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        }

        private void UpdateBoardState()
        {
            _board.EnPassantSquare = null;

            // Set En Passant
            if (_movedPiece.Type == PieceType.Pawn && Math.Abs(_from.y - _to.y) == 2)
            {
                int direction = _movedPiece.IsWhite ? 1 : -1;
                _board.EnPassantSquare = new Vector2Int(_from.x, _from.y + direction);
            }

            // Update Castling Rights
            if (_movedPiece.Type == PieceType.King)
            {
                if (_movedPiece.IsWhite) _board.CurrentCastlingRights &= ~(CastlingRights.WhiteKingSide | CastlingRights.WhiteQueenSide);
                else _board.CurrentCastlingRights &= ~(CastlingRights.BlackKingSide | CastlingRights.BlackQueenSide);
            }
            
            if (_movedPiece.Type == PieceType.Rook)
            {
                if (_from.x == 0 && _from.y == 0) _board.CurrentCastlingRights &= ~CastlingRights.WhiteQueenSide;
                if (_from.x == 7 && _from.y == 0) _board.CurrentCastlingRights &= ~CastlingRights.WhiteKingSide;
                if (_from.x == 0 && _from.y == 7) _board.CurrentCastlingRights &= ~CastlingRights.BlackQueenSide;
                if (_from.x == 7 && _from.y == 7) _board.CurrentCastlingRights &= ~CastlingRights.BlackKingSide;
            }
        }
    }
}