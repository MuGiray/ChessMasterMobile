using System;

namespace Chess.Core.Models
{
    public enum PieceType : byte { None, Pawn, Knight, Bishop, Rook, Queen, King } // Byte optimizasyonu
    public enum PieceColor : byte { None, White, Black }

    [Serializable]
    public struct Piece
    {
        public readonly PieceType Type;
        public readonly PieceColor Color;

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
        }

        public bool IsWhite => Color == PieceColor.White;
        public bool IsValid => Type != PieceType.None;

        // Eşitlik kontrolü performansı için
        public static bool operator ==(Piece a, Piece b) => a.Type == b.Type && a.Color == b.Color;
        public static bool operator !=(Piece a, Piece b) => !(a == b);
        public override bool Equals(object obj) => obj is Piece p && this == p;
        public override int GetHashCode() => HashCode.Combine(Type, Color);
    }

    [Flags]
    public enum CastlingRights : byte
    {
        None = 0,
        WhiteKingSide = 1,
        WhiteQueenSide = 2,
        BlackKingSide = 4,
        BlackQueenSide = 8,
        All = 15
    }

    // KRİTİK OPTİMİZASYON: IEquatable ve GetHashCode olmadan Dictionary performansı çöker.
    [Serializable]
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }

        public bool Equals(Vector2Int other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is Vector2Int other && Equals(other);
        public override int GetHashCode() => x * 397 ^ y; // Basit ve hızlı hash

        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        
        public override string ToString() => $"({x}, {y})";
    }

    public enum GameState
    {
        InProgress,
        Checkmate,
        Stalemate,
        Draw
    }
}