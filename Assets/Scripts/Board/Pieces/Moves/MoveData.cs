using UnityEngine;
using static Board.Pieces.Piece;

namespace Board.Pieces.Moves
{
    public enum MoveType
    {
        Move,
        Take,
        Castle,
        Enpassant,
    }

    public struct MoveData
    {
        public File File;
        public Rank Rank;
        public MoveType Type;
        public bool IsPromotion;
    }
}
