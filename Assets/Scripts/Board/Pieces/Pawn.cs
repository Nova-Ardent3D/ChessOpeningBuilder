using Board.BoardMarkers;
using Board.Pieces.Moves;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Board.Pieces
{
    public class Pawn : Piece
    {
        public bool CanEnPassant = false;

        public override PieceTypes Type => PieceTypes.Pawn;

        public override IEnumerable<MoveData> GetMoves(BoardState boardState)
        {
            foreach (var move in Moves(boardState))
            {
                if (move.Rank == Rank.Eight || move.Rank == Rank.One)
                {
                    // cant modify enumerable in for iteration
                    MoveData moveCopy = move;
                    moveCopy.IsPromotion = true;
                    yield return moveCopy;
                }
                else
                {
                    yield return move;
                }
            }
        }

        IEnumerable<MoveData> Moves(BoardState boardState)
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


                File enPassantLeft = CurrentFile - 1;
                if ((int)enPassantLeft >= 0 && boardState.Pieces[(int)enPassantLeft, (int)CurrentRank] is Pawn leftPawn && leftPawn.CanEnPassant)
                {
                    yield return new MoveData()
                    {
                        File = enPassantLeft,
                        Rank = CurrentRank + 1,
                        Type = MoveType.Enpassant
                    };
                }

                File enPassantRight = CurrentFile + 1;
                if ((int)enPassantRight <= 7 && boardState.Pieces[(int)enPassantRight, (int)CurrentRank] is Pawn rightPawn && rightPawn.CanEnPassant)
                {
                    yield return new MoveData()
                    {
                        File = enPassantRight,
                        Rank = CurrentRank + 1,
                        Type = MoveType.Enpassant
                    };
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

                File enPassantLeft = CurrentFile - 1;
                if ((int)enPassantLeft >= 0 && boardState.Pieces[(int)enPassantLeft, (int)CurrentRank] is Pawn leftPawn && leftPawn.CanEnPassant)
                {
                    yield return new MoveData()
                    {
                        File = enPassantLeft,
                        Rank = CurrentRank - 1,
                        Type = MoveType.Enpassant
                    };
                }

                File enPassantRight = CurrentFile + 1;
                if ((int)enPassantRight <= 7 && boardState.Pieces[(int)enPassantRight, (int)CurrentRank] is Pawn rightPawn && rightPawn.CanEnPassant)
                {
                    yield return new MoveData()
                    {
                        File = enPassantRight,
                        Rank = CurrentRank - 1,
                        Type = MoveType.Enpassant
                    };
                }
            }
        }
    }
}