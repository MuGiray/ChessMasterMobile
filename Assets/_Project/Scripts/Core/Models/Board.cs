using System;
using System.Collections.Generic; // List için
using System.Runtime.CompilerServices;
using Chess.Core.Logic; // Zobrist için

namespace Chess.Core.Models
{
    public class Board
    {
        private readonly Piece[,] _grid; 
        public const int Size = 8;
        
        // --- ZOBRIST HASHING ALANI ---
        public ulong ZobristKey { get; private set; }
        public List<ulong> History { get; private set; } = new List<ulong>();
        // -----------------------------

        // Backing Fields (Property değiştiğinde Hash güncellemek için)
        private PieceColor _turn;
        private CastlingRights _castlingRights;
        private Vector2Int? _enPassantSquare;

        public PieceColor Turn 
        { 
            get => _turn; 
            set 
            {
                // Sadece değişirse Hash güncelle
                if (_turn != value)
                {
                    _turn = value;
                    ZobristKey ^= Zobrist.SideToMove; // XOR (Varsa çıkar, yoksa ekle)
                }
            } 
        }

        public CastlingRights CurrentCastlingRights 
        { 
            get => _castlingRights; 
            set 
            {
                if (_castlingRights != value)
                {
                    ZobristKey ^= Zobrist.CastlingRights[(int)_castlingRights]; // Eskiyi çıkar
                    _castlingRights = value;
                    ZobristKey ^= Zobrist.CastlingRights[(int)_castlingRights]; // Yeniyi ekle
                }
            } 
        }

        public Vector2Int? EnPassantSquare 
        { 
            get => _enPassantSquare; 
            set 
            {
                if (_enPassantSquare != value)
                {
                    // Eskiyi çıkar
                    int oldFile = _enPassantSquare.HasValue ? _enPassantSquare.Value.x : 8; // 8 = Yok
                    ZobristKey ^= Zobrist.EnPassantFile[oldFile];

                    _enPassantSquare = value;

                    // Yeniyi ekle
                    int newFile = _enPassantSquare.HasValue ? _enPassantSquare.Value.x : 8;
                    ZobristKey ^= Zobrist.EnPassantFile[newFile];
                }
            } 
        }

        public int HalfMoveClock { get; set; }
        public int FullMoveNumber { get; set; }

        // Cache (Önceki adımdan kalanlar)
        public Vector2Int WhiteKingPos { get; private set; } = new Vector2Int(-1, -1);
        public Vector2Int BlackKingPos { get; private set; } = new Vector2Int(-1, -1);

        public Board()
        {
            _grid = new Piece[Size, Size];
            _turn = PieceColor.White; // Property set edilirse Hash bozulabilir, field kullan
            _castlingRights = CastlingRights.All;
            _enPassantSquare = null;
            
            // Başlangıç Hash Değeri Hesapla (Boş Tahta + Default Ayarlar)
            ZobristKey = 0;
            ZobristKey ^= Zobrist.CastlingRights[(int)_castlingRights];
            ZobristKey ^= Zobrist.EnPassantFile[8]; // EnPassant Yok
            // White to move (Zobrist.SideToMove eklenmez, çünkü sıra beyazda)
            
            History.Clear();
            History.Add(ZobristKey);
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
                 Piece oldPiece = _grid[coords.x, coords.y];
                 
                 // --- HASH UPDATE (Eski taşı çıkar) ---
                 if (oldPiece.Type != PieceType.None)
                 {
                     int squareIndex = coords.y * 8 + coords.x;
                     int pieceIndex = Zobrist.GetPieceIndex(oldPiece);
                     ZobristKey ^= Zobrist.PiecesArray[squareIndex, pieceIndex];
                 }

                 _grid[coords.x, coords.y] = piece;

                 // --- HASH UPDATE (Yeni taşı ekle) ---
                 if (piece.Type != PieceType.None)
                 {
                     int squareIndex = coords.y * 8 + coords.x;
                     int pieceIndex = Zobrist.GetPieceIndex(piece);
                     ZobristKey ^= Zobrist.PiecesArray[squareIndex, pieceIndex];

                     // King Cache Update
                     if (piece.Type == PieceType.King)
                     {
                         if (piece.Color == PieceColor.White) WhiteKingPos = coords;
                         else BlackKingPos = coords;
                     }
                 }
             }
        }

        public void MovePiece(Vector2Int from, Vector2Int to)
        {
            Piece movedPiece = _grid[from.x, from.y];
            
            // SetPieceAt HASH'i otomatik günceller, bu yüzden manuel Grid erişimi yerine
            // SetPieceAt kullanmak daha güvenlidir ama yavaştır. 
            // Performans için manuel yapıp Hash'i burada yönetiyoruz.
            
            int fromIndex = from.y * 8 + from.x;
            int toIndex = to.y * 8 + to.x;
            int pieceIndex = Zobrist.GetPieceIndex(movedPiece);

            // 1. Kaynaktan taşı kaldır (Hash Çıkar)
            ZobristKey ^= Zobrist.PiecesArray[fromIndex, pieceIndex];
            _grid[from.x, from.y] = new Piece(PieceType.None, PieceColor.None);

            // 2. Hedefteki taşı kaldır (Varsa Hash Çıkar - Yeme durumu)
            // Not: Normal MovePiece sadece taşır, yeme logic'i MoveCommand'dedir.
            // Ama Board verisinde hedef doluysa üzerine yazar.
            Piece targetPiece = _grid[to.x, to.y];
            if (targetPiece.Type != PieceType.None)
            {
                int targetIndex = Zobrist.GetPieceIndex(targetPiece);
                ZobristKey ^= Zobrist.PiecesArray[toIndex, targetIndex];
            }

            // 3. Hedefe taşı koy (Hash Ekle)
            ZobristKey ^= Zobrist.PiecesArray[toIndex, pieceIndex];
            _grid[to.x, to.y] = movedPiece;

            // King Cache
            if (movedPiece.Type == PieceType.King)
            {
                if (movedPiece.Color == PieceColor.White) WhiteKingPos = to;
                else BlackKingPos = to;
            }
            
            // Sıra değişimi (Otomatik Hash Update yapar)
            Turn = (Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            
            // TARİHÇEYE EKLE
            History.Add(ZobristKey);
        }

        // UNDO İÇİN GEREKLİ
        public void RemoveLastHistory()
        {
            if (History.Count > 0)
            {
                History.RemoveAt(History.Count - 1);
            }
        }

        public void Clear()
        {
            Array.Clear(_grid, 0, _grid.Length);
            WhiteKingPos = new Vector2Int(-1, -1);
            BlackKingPos = new Vector2Int(-1, -1);
            
            // Hash Reset
            ZobristKey = 0;
            // Default hakları tekrar hesaplatmak için propertyleri tetiklemiyoruz, manuel set.
            _turn = PieceColor.White;
            _castlingRights = CastlingRights.None; 
            _enPassantSquare = null;
            
            // History Reset
            History.Clear();
        }

        public Board Clone()
        {
            Board clone = new Board();
            // Field'ları kopyala (Property kullanma, Hash tetiklenmesin)
            clone._turn = this._turn;
            clone._castlingRights = this._castlingRights;
            clone._enPassantSquare = this._enPassantSquare;
            
            clone.HalfMoveClock = this.HalfMoveClock;
            clone.FullMoveNumber = this.FullMoveNumber;
            clone.ZobristKey = this.ZobristKey;
            
            clone.WhiteKingPos = this.WhiteKingPos;
            clone.BlackKingPos = this.BlackKingPos;

            Array.Copy(this._grid, clone._grid, this._grid.Length);
            
            // History kopyala (Deep Copy)
            clone.History = new List<ulong>(this.History);
            
            return clone;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInsideBoard(Vector2Int coords)
        {
            return coords.x >= 0 && coords.x < Size && coords.y >= 0 && coords.y < Size;
        }
    }
}