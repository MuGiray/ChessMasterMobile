using UnityEngine;
using Chess.Core.Models;

namespace Chess.Unity.ScriptableObjects
{
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
                return type switch
                {
                    PieceType.Pawn => WhitePawn,
                    PieceType.Rook => WhiteRook,
                    PieceType.Knight => WhiteKnight,
                    PieceType.Bishop => WhiteBishop,
                    PieceType.Queen => WhiteQueen,
                    PieceType.King => WhiteKing,
                    _ => null
                };
            }
            else
            {
                return type switch
                {
                    PieceType.Pawn => BlackPawn,
                    PieceType.Rook => BlackRook,
                    PieceType.Knight => BlackKnight,
                    PieceType.Bishop => BlackBishop,
                    PieceType.Queen => BlackQueen,
                    PieceType.King => BlackKing,
                    _ => null
                };
            }
        }
    }
}