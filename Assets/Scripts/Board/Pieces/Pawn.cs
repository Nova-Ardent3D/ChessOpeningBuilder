using Board.BoardMarkers;
using Board.Pieces.Moves;
using System.Collections.Generic;
using UnityEngine;

namespace Board.Pieces
{
    public class Pawn : Piece
    {
        public override IEnumerable<MoveData> GetMoves(BoardState boardState)
        {
            if (IsWhite)
            {
                if (CurrentRank == Rank.Two)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (boardState.Pieces[(int)CurrentFile, (int)CurrentRank + i + 1] != null)
                        {
                            break;
                        }
                        yield return new MoveData() { File = CurrentFile, Rank = CurrentRank + i + 1, Type = MoveType.Move };
                    }
                }
                else
                {
                    if (boardState.Pieces[(int)CurrentFile, (int)CurrentRank + 1] == null)
                    {
                        yield return new MoveData() { File = CurrentFile, Rank = CurrentRank + 1, Type = MoveType.Move };
                    }
                }

                Rank diagnolRank = CurrentRank + 1;
                if (CurrentFile != File.A)
                {
                    File diagnolFileLeft = CurrentFile - 1;
                    Piece targetPieceLeft = boardState.Pieces[(int)diagnolFileLeft, (int)diagnolRank];

                    if (targetPieceLeft != null && targetPieceLeft.IsWhite != IsWhite)
                    {
                        yield return new MoveData() { File = diagnolFileLeft, Rank = diagnolRank, Type = MoveType.Take };
                    }
                }

                if (CurrentFile != File.H)
                {
                    File diagnolFileRight = CurrentFile + 1;
                    Piece targetPieceRight = boardState.Pieces[(int)diagnolFileRight, (int)diagnolRank];

                    if (targetPieceRight != null && targetPieceRight.IsWhite != IsWhite)
                    {
                        yield return new MoveData() { File = diagnolFileRight, Rank = diagnolRank, Type = MoveType.Take };
                    }
                }

            }
            else
            {
                if (CurrentRank == Rank.Seven)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (boardState.Pieces[(int)CurrentFile, (int)CurrentRank - i - 1] != null)
                        {
                            break;
                        }
                        yield return new MoveData() { File = CurrentFile, Rank = CurrentRank - i - 1, Type = MoveType.Move };
                    }
                }
                else
                {
                    if (boardState.Pieces[(int)CurrentFile, (int)CurrentRank - 1] == null)
                    {
                        yield return new MoveData() { File = CurrentFile, Rank = CurrentRank - 1, Type = MoveType.Move };
                    }
                }

                Rank diagnolRank = CurrentRank - 1;
                if (CurrentFile != File.A)
                {
                    File diagnolFileLeft = CurrentFile - 1;
                    Piece targetPieceLeft = boardState.Pieces[(int)diagnolFileLeft, (int)diagnolRank];

                    if (targetPieceLeft != null && targetPieceLeft.IsWhite != IsWhite)
                    {
                        yield return new MoveData() { File = diagnolFileLeft, Rank = diagnolRank, Type = MoveType.Take };
                    }
                }

                if (CurrentFile != File.H)
                {
                    File diagnolFileRight = CurrentFile + 1;
                    Piece targetPieceRight = boardState.Pieces[(int)diagnolFileRight, (int)diagnolRank];

                    if (targetPieceRight != null && targetPieceRight.IsWhite != IsWhite)
                    {
                        yield return new MoveData() { File = diagnolFileRight, Rank = diagnolRank, Type = MoveType.Take };
                    }
                }
            }
        }
    }
}