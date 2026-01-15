using System;

namespace Chess.Core.Models
{
    public class Board
    {
       private readonly Piece[,] _grid; 
        public const int Size = 8;
        
        public PieceColor Turn { get; set; }
        
        // YENİ ÖZELLİKLER
        public CastlingRights CurrentCastlingRights { get; set; }
        public Vector2Int? EnPassantSquare { get; set; } // Null olabilir (Eğer yoksa)
        public int HalfMoveClock { get; set; } // 50 hamle kuralı için
        public int FullMoveNumber { get; set; } // Hamle sayısı

        public Board()
        {
            _grid = new Piece[Size, Size];
            Turn = PieceColor.White;
            CurrentCastlingRights = CastlingRights.All;
            EnPassantSquare = null;
        }

        public Piece GetPieceAt(Vector2Int coords)
        {
            if (!IsInsideBoard(coords)) return new Piece(PieceType.None, PieceColor.None);
            return _grid[coords.x, coords.y];
        }

        public void MovePiece(Vector2Int from, Vector2Int to)
        {
            // Validasyon MoveValidator sınıfında yapılacak (Single Responsibility)
            _grid[to.x, to.y] = _grid[from.x, from.y];
            _grid[from.x, from.y] = new Piece(PieceType.None, PieceColor.None);
            
            // Sırayı değiştir
            Turn = (Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        }

        public void SetPieceAt(Vector2Int coords, Piece piece)
        {
             if (IsInsideBoard(coords)) _grid[coords.x, coords.y] = piece;
        }

        // Tahtayı sıfırlar
        public void Clear()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    _grid[x, y] = new Piece(PieceType.None, PieceColor.None);
                }
            }
        }

        private bool IsInsideBoard(Vector2Int coords)
        {
            return coords.x >= 0 && coords.x < Size && coords.y >= 0 && coords.y < Size;
        }
    }
}