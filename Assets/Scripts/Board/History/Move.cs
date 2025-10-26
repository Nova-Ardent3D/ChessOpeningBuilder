using UnityEngine;
using Board.Pieces;
using static Board.Pieces.Piece;

namespace Board.History
{
    public class Move
    {
        public bool IsCapture;
        public bool IsCheck;
        public bool IsCastle;
        public bool IsWhite;

        public PieceTypes PieceType;
        
        public File FromFile;
        public Rank FromRank;

        public File ToFile;
        public Rank ToRank;

        public File? FileDisambiguation;
        public Rank? RankDisambiguation;

        public override string ToString()
        {
            if (IsCastle)
            {
                if (ToFile == File.G)
                {
                    return "O-O";
                }
                else
                {
                    return "O-O-O";
                }
            }

            string notation = "";
            switch (PieceType)
            {
                case PieceTypes.Pawn:
                    if (IsCapture)
                    {
                        notation += FromFile.ToString().ToLower();
                    }
                    else
                    {
                        notation += IsWhite ? "" : "";
                    }
                    break;
                case PieceTypes.Knight:
                    notation += IsWhite ? "N" : "n";
                    break;
                case PieceTypes.Bishop:
                    notation += IsWhite ? "B" : "b";
                    break;
                case PieceTypes.Rook:
                    notation += IsWhite ? "R" : "r";
                    break;
                case PieceTypes.Queen:
                    notation += IsWhite ? "Q" : "q";
                    break;
                case PieceTypes.King:
                    notation += IsWhite ? "K" : "k";
                    break;
            }

            if (IsCapture)
            {
                notation += "x";
            }

            notation += $"{ToFile.ToString().ToLower()}{(int)ToRank + 1}";

            if (IsCheck)
            {
                notation += "+";
            }

            return notation;
        }
    }
}
