using System.Collections.Generic;
using UnityEngine;

namespace Board.Pieces
{
    [System.Serializable]
    public struct PiecePrefabs
    {
        public static readonly HashSet<char> PieceIDs = new HashSet<char>()
        {
            'P', 'N', 'B', 'R', 'Q', 'K',
            'p', 'n', 'b', 'r', 'q', 'k'
        };

        public Piece WhitePawn;
        public Piece WhiteRook;
        public Piece WhiteKnight;
        public Piece WhiteBishop;
        public Piece WhiteQueen;
        public Piece WhiteKing;

        public Piece BlackPawn;
        public Piece BlackRook;
        public Piece BlackKnight;
        public Piece BlackBishop;
        public Piece BlackQueen;
        public Piece BlackKing;

        public Piece GetPiece(char c)
        {
            switch (c)
            {
                case 'p': return BlackPawn;
                case 'r': return BlackRook;
                case 'n': return BlackKnight;
                case 'b': return BlackBishop;
                case 'q': return BlackQueen;
                case 'k': return BlackKing;

                case 'P': return WhitePawn;
                case 'R': return WhiteRook;
                case 'N': return WhiteKnight;
                case 'B': return WhiteBishop;
                case 'Q': return WhiteQueen;
                case 'K': return WhiteKing;
                default:
                    return null;
            }
        }
    }
}
