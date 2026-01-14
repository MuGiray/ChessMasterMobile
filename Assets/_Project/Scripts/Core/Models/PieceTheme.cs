using UnityEngine;
using Chess.Core.Models; // PieceType ve PieceColor için

namespace Chess.Unity.ScriptableObjects
{
    // Bu attribute, Unity içinde sağ tık menüsüne bu ayarı ekler.
    [CreateAssetMenu(fileName = "NewPieceTheme", menuName = "Chess/Piece Theme")]
    public class PieceTheme : ScriptableObject
    {
        [Header("White Pieces")]
        public Sprite WhitePawn;
        public Sprite WhiteRook;
        public Sprite WhiteKnight;
        public Sprite WhiteBishop;
        public Sprite WhiteQueen;
        public Sprite WhiteKing;

        [Header("Black Pieces")]
        public Sprite BlackPawn;
        public Sprite BlackRook;
        public Sprite BlackKnight;
        public Sprite BlackBishop;
        public Sprite BlackQueen;
        public Sprite BlackKing;

        public Sprite GetSprite(PieceType type, PieceColor color)
        {
            if (color == PieceColor.White)
            {
                switch (type)
                {
                    case PieceType.Pawn: return WhitePawn;
                    case PieceType.Rook: return WhiteRook;
                    case PieceType.Knight: return WhiteKnight;
                    case PieceType.Bishop: return WhiteBishop;
                    case PieceType.Queen: return WhiteQueen;
                    case PieceType.King: return WhiteKing;
                }
            }
            else
            {
                switch (type)
                {
                    case PieceType.Pawn: return BlackPawn;
                    case PieceType.Rook: return BlackRook;
                    case PieceType.Knight: return BlackKnight;
                    case PieceType.Bishop: return BlackBishop;
                    case PieceType.Queen: return BlackQueen;
                    case PieceType.King: return BlackKing;
                }
            }
            return null;
        }
    }
}