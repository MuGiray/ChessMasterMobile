namespace Chess.Core.Models
{
    public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
    public enum PieceColor { None, White, Black }

    // Struct kullanarak Heap allocation'ı azaltıyoruz (Mobil Performans)
    [System.Serializable]
    public struct Piece
    {
        public PieceType Type;
        public PieceColor Color;

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
        }

        public bool IsWhite => Color == PieceColor.White;
        public bool IsValid => Type != PieceType.None;
    }

    [System.Flags]
    public enum CastlingRights
    {
        None = 0,
        WhiteKingSide = 1,  // K (1)
        WhiteQueenSide = 2, // Q (2)
        BlackKingSide = 4,  // k (4)
        BlackQueenSide = 8, // q (8)
        All = 15
    }

    [System.Serializable]
    public struct Vector2Int // Unity'nin Vector2Int'ine bağımlılığı kesmek için kendi struct'ımız (Opsiyonel ama önerilir)
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }

    public enum GameState
    {
        InProgress, // Oyun Devam Ediyor
        Checkmate,  // Şah Mat (Kazanan var)
        Stalemate,  // Pat (Berabere)
        Draw,       // Diğer beraberlikler (İleride eklenebilir)
    }
}