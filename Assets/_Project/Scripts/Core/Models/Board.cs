using System;
using System.Runtime.CompilerServices; // Inlining için

namespace Chess.Core.Models
{
    public class Board
    {
        private readonly Piece[,] _grid; 
        public const int Size = 8;
        
        public PieceColor Turn { get; set; }
        public CastlingRights CurrentCastlingRights { get; set; }
        public Vector2Int? EnPassantSquare { get; set; }
        public int HalfMoveClock { get; set; }
        public int FullMoveNumber { get; set; }

        public Board()
        {
            _grid = new Piece[Size, Size];
            Turn = PieceColor.White;
            CurrentCastlingRights = CastlingRights.All;
            EnPassantSquare = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Piece GetPieceAt(Vector2Int coords)
        {
            if (!IsInsideBoard(coords)) return new Piece(PieceType.None, PieceColor.None);
            return _grid[coords.x, coords.y];
        }

        public void SetPieceAt(Vector2Int coords, Piece piece)
        {
             if (IsInsideBoard(coords)) _grid[coords.x, coords.y] = piece;
        }

        public void MovePiece(Vector2Int from, Vector2Int to)
        {
            // Basit taşıma (Validation logic katmanında yapılır)
            _grid[to.x, to.y] = _grid[from.x, from.y];
            _grid[from.x, from.y] = new Piece(PieceType.None, PieceColor.None);
            
            // Sıra değişimi
            Turn = (Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        }

        public void Clear()
        {
            Array.Clear(_grid, 0, _grid.Length); // Loop yerine Array.Clear daha hızlıdır
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Bu metod milyonlarca kez çağrılır, inline şart.
        public static bool IsInsideBoard(Vector2Int coords)
        {
            // Bitwise kontrol (uint cast) negatif kontrolünü de kapsar ve daha hızlıdır.
            // return (uint)coords.x < Size && (uint)coords.y < Size;
            // Ama okunabilirlik için standart bırakıyorum:
            return coords.x >= 0 && coords.x < Size && coords.y >= 0 && coords.y < Size;
        }
    }
}