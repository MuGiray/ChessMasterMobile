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
        private readonly PieceType _promotionType; 

        // Properties
        public string Notation { get; set; }
        public Vector2Int From => _from;
        public Vector2Int To => _to;
        public PieceType PromotionType => _promotionType;
        public Piece CapturedPiece => _capturedPiece;

        // State Snapshot
        private readonly Piece _movedPiece;
        private readonly Piece _capturedPiece;
        private readonly CastlingRights _oldCastlingRights;
        private readonly Vector2Int? _oldEnPassantSquare;
        private readonly int _oldHalfMoveClock;
        private readonly ulong _oldZobristKey; // YENİ: Güvenlik için Hash yedeği

        private bool _isEnPassantMove;

        public MoveCommand(Board board, Vector2Int from, Vector2Int to, PieceType promotionType = PieceType.Queen)
        {
            _board = board;
            _from = from;
            _to = to;
            _promotionType = promotionType;

            _movedPiece = board.GetPieceAt(from);
            _capturedPiece = board.GetPieceAt(to);
            
            _oldCastlingRights = board.CurrentCastlingRights;
            _oldEnPassantSquare = board.EnPassantSquare;
            _oldHalfMoveClock = board.HalfMoveClock;
            _oldZobristKey = board.ZobristKey; // YENİ: Hash'i sakla
        }

        public void Execute()
        {
            // 1. Standart Taşıma
            // Not: MovePiece metodu Board.cs içinde History.Add() yapar.
            _board.MovePiece(_from, _to);

            // HalfMove Clock Update (50 Hamle Kuralı İçin)
            // Piyon sürüşü veya taş yeme varsa saat sıfırlanır, yoksa artar.
            if (_movedPiece.Type == PieceType.Pawn || _capturedPiece.Type != PieceType.None)
            {
                _board.HalfMoveClock = 0;
            }
            else
            {
                _board.HalfMoveClock++;
            }

            // Full Move Update (Siyah oynadığında artar)
            if (_board.Turn == PieceColor.White) // Şu an sıra beyazdaysa, siyah oynamış demektir (Board.MovePiece sırayı değiştirdi çünkü)
            {
                _board.FullMoveNumber++;
            }

            // 2. En Passant Capture Logic
            if (_movedPiece.Type == PieceType.Pawn && _from.x != _to.x && _capturedPiece.Type == PieceType.None)
            {
                _isEnPassantMove = true;
                int direction = _movedPiece.IsWhite ? 1 : -1;
                Vector2Int capturedPawnPos = new Vector2Int(_to.x, _to.y - direction);
                
                // Piyonu tahtadan sil (SetPieceAt Hash'i günceller)
                _board.SetPieceAt(capturedPawnPos, new Piece(PieceType.None, PieceColor.None));
                _board.HalfMoveClock = 0; // En passant bir piyon hamlesidir
            }

            // 3. Promotion Logic
            int lastRank = _movedPiece.IsWhite ? 7 : 0;
            if (_movedPiece.Type == PieceType.Pawn && _to.y == lastRank)
            {
                _board.SetPieceAt(_to, new Piece(_promotionType, _movedPiece.Color));
            }

            // 4. Castling Logic
            if (_movedPiece.Type == PieceType.King && System.Math.Abs(_from.x - _to.x) == 2)
            {
                int rank = _movedPiece.IsWhite ? 0 : 7;
                bool isKingSide = _to.x > _from.x; 
                Vector2Int rookFrom = isKingSide ? new Vector2Int(7, rank) : new Vector2Int(0, rank);
                Vector2Int rookTo = isKingSide ? new Vector2Int(5, rank) : new Vector2Int(3, rank);
                
                // Kaleyi taşı (Manuel SetPieceAt kullanıyoruz ki MovePiece tekrar History eklemesin)
                Piece rook = _board.GetPieceAt(rookFrom);
                _board.SetPieceAt(rookFrom, new Piece(PieceType.None, PieceColor.None));
                _board.SetPieceAt(rookTo, rook);
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
            if (_movedPiece.Type == PieceType.King && System.Math.Abs(_from.x - _to.x) == 2)
            {
                int rank = _movedPiece.IsWhite ? 0 : 7;
                bool isKingSide = _to.x > _from.x;
                Vector2Int rookFrom = isKingSide ? new Vector2Int(7, rank) : new Vector2Int(0, rank);
                Vector2Int rookTo = isKingSide ? new Vector2Int(5, rank) : new Vector2Int(3, rank);
                
                Piece rook = _board.GetPieceAt(rookTo);
                _board.SetPieceAt(rookTo, new Piece(PieceType.None, PieceColor.None));
                _board.SetPieceAt(rookFrom, rook);
            }

            // 4. Restore State
            _board.CurrentCastlingRights = _oldCastlingRights;
            _board.EnPassantSquare = _oldEnPassantSquare;
            _board.HalfMoveClock = _oldHalfMoveClock;
            
            // Sırayı geri al
            _board.Turn = (_board.Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;

            if (_board.Turn == PieceColor.Black) // Eğer sıra siyaha geçtiyse, hamle sayısı azalır
            {
                _board.FullMoveNumber--;
            }
            
            // YENİ: Tarihçeden son kaydı sil
            _board.RemoveLastHistory();
        }

        private void UpdateBoardState()
        {
            // En Passant Karesini Hesapla
            _board.EnPassantSquare = null;

            if (_movedPiece.Type == PieceType.Pawn && System.Math.Abs(_from.y - _to.y) == 2)
            {
                int direction = _movedPiece.IsWhite ? 1 : -1;
                _board.EnPassantSquare = new Vector2Int(_from.x, _from.y + direction);
            }

            // Rok Haklarını Güncelle
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
            
            // Hedef karede kale yenmişse de rok hakkı gider
            if (_capturedPiece.Type == PieceType.Rook)
            {
                if (_to.x == 0 && _to.y == 0) _board.CurrentCastlingRights &= ~CastlingRights.WhiteQueenSide;
                if (_to.x == 7 && _to.y == 0) _board.CurrentCastlingRights &= ~CastlingRights.WhiteKingSide;
                if (_to.x == 0 && _to.y == 7) _board.CurrentCastlingRights &= ~CastlingRights.BlackQueenSide;
                if (_to.x == 7 && _to.y == 7) _board.CurrentCastlingRights &= ~CastlingRights.BlackKingSide;
            }
        }
    }
}