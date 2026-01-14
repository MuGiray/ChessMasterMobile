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
        private readonly Piece _capturedPiece; // Geri alma işlemi için yenen taş saklanmalı
        private readonly Piece _movedPiece;

        public MoveCommand(Board board, Vector2Int from, Vector2Int to)
        {
            _board = board;
            _from = from;
            _to = to;
            _movedPiece = board.GetPieceAt(from);
            _capturedPiece = board.GetPieceAt(to);
        }

        public void Execute()
        {
            _board.MovePiece(_from, _to);
            // Burada Observer pattern ile UI'a "Taş oynadı" event'i fırlatılacak.
        }

        public void Undo()
        {
            // Taşı eski yerine koy
            _board.SetPieceAt(_from, _movedPiece);
            // Eğer bir taş yendiyse onu geri getir
            _board.SetPieceAt(_to, _capturedPiece);
            
            // TODO: Turn bilgisini de geri al (Board class'ında handle edilmeli)
        }
    }
}