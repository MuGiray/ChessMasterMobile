using System;
using System.Runtime.CompilerServices;

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

        // --- CACHE (ÖNBELLEK) ---
        // Şahların konumunu her an elimizin altında tutuyoruz.
        // (-1, -1) başlangıçta "yok" demek.
        public Vector2Int WhiteKingPos { get; private set; } = new Vector2Int(-1, -1);
        public Vector2Int BlackKingPos { get; private set; } = new Vector2Int(-1, -1);

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
             if (IsInsideBoard(coords))
             {
                 _grid[coords.x, coords.y] = piece;

                 // OPTİMİZASYON: Eğer konulan taş Şah ise, konumunu kaydet.
                 if (piece.Type == PieceType.King)
                 {
                     if (piece.Color == PieceColor.White) WhiteKingPos = coords;
                     else BlackKingPos = coords;
                 }
             }
        }

        public void MovePiece(Vector2Int from, Vector2Int to)
        {
            // Taşı al
            Piece movedPiece = _grid[from.x, from.y];
            
            // Taşı hedef kareye koy
            _grid[to.x, to.y] = movedPiece;
            _grid[from.x, from.y] = new Piece(PieceType.None, PieceColor.None);
            
            // OPTİMİZASYON: Hareket eden taş Şah ise, yeni konumunu güncelle.
            if (movedPiece.Type == PieceType.King)
            {
                if (movedPiece.Color == PieceColor.White) WhiteKingPos = to;
                else BlackKingPos = to;
            }

            // Sıra değişimi
            Turn = (Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        }

        public void Clear()
        {
            Array.Clear(_grid, 0, _grid.Length);
            // Tahta temizlenince cache'i de sıfırla
            WhiteKingPos = new Vector2Int(-1, -1);
            BlackKingPos = new Vector2Int(-1, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInsideBoard(Vector2Int coords)
        {
            return coords.x >= 0 && coords.x < Size && coords.y >= 0 && coords.y < Size;
        }

        // --- YENİ: AI İÇİN GÜVENLİ KOPYALAMA ---
        public Board Clone()
        {
            Board clone = new Board();
            
            // Değer tiplerini kopyala
            clone.Turn = this.Turn;
            clone.CurrentCastlingRights = this.CurrentCastlingRights;
            clone.EnPassantSquare = this.EnPassantSquare;
            clone.HalfMoveClock = this.HalfMoveClock;
            clone.FullMoveNumber = this.FullMoveNumber;
            
            // Cache'i kopyala
            clone.WhiteKingPos = this.WhiteKingPos;
            clone.BlackKingPos = this.BlackKingPos;

            // Grid dizisini hızlıca kopyala (Block Copy)
            Array.Copy(this._grid, clone._grid, this._grid.Length);
            
            return clone;
        }
    }
}