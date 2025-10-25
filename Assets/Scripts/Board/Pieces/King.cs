using Board.BoardMarkers;
using Board.Pieces.Moves;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Board.Pieces
{
    public class King : Piece
    {
        public override PieceTypes Type => PieceTypes.King;


        public static readonly Vector2[] MoveDirections = new Vector2[]
        {
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1),
        };

        public override IEnumerable<MoveData> GetMoves(BoardState boardState)
        {
            foreach (var direction in MoveDirections)
            {
                int targetFile = (int)CurrentFile + (int)direction.x;
                int targetRank = (int)CurrentRank + (int)direction.y;
                if (targetFile < 0 || targetFile > 7 || targetRank < 0 || targetRank > 7)
                {
                    continue;
                }

                if (boardState.Pieces[targetFile, targetRank] == null)
                {
                    yield return new MoveData()
                    {
                        File = (File)targetFile,
                        Rank = (Rank)targetRank,
                        Type = MoveType.Move
                    };
                }
                else if (boardState.Pieces[targetFile, targetRank].IsWhite != IsWhite)
                {
                    yield return new MoveData()
                    {
                        File = (File)targetFile,
                        Rank = (Rank)targetRank,
                        Type = MoveType.Take
                    };
                }
            }

            if (CanCastleKingSide(boardState))
            {
                if (IsWhite)
                {
                    yield return new MoveData()
                    {
                        File = File.G,
                        Rank = Rank.One,
                        Type = MoveType.Castle
                    };
                }
                else
                {
                    yield return new MoveData()
                    {
                        File = File.G,
                        Rank = Rank.Eight,
                        Type = MoveType.Castle
                    };
                }
            }
            
            if (CanCastleQueenSide(boardState))
            {
                if (IsWhite)
                {
                    yield return new MoveData()
                    {
                        File = File.C,
                        Rank = Rank.One,
                        Type = MoveType.Castle
                    };
                }
                else
                {
                    yield return new MoveData()
                    {
                        File = File.C,
                        Rank = Rank.Eight,
                        Type = MoveType.Castle
                    };
                }
            }

        }

        bool CanCastleKingSide(BoardState boardState)
        {
            if (IsWhite)
            {
                if (!boardState.WhiteCastlingRights.HasFlag(BoardState.CastlingRights.KingSide))
                {
                    return false;
                }

                if (boardState.Pieces[(int)Piece.File.F, (int)Piece.Rank.One] != null ||
                    boardState.Pieces[(int)Piece.File.G, (int)Piece.Rank.One] != null)
                {
                    return false;
                }

                return !IsAttacked(boardState.Pieces, (int)Piece.File.E, (int)Piece.Rank.One) &&
                       !IsAttacked(boardState.Pieces, (int)Piece.File.F, (int)Piece.Rank.One) &&
                       !IsAttacked(boardState.Pieces, (int)Piece.File.G, (int)Piece.Rank.One);
            }
            else
            {
                if (!boardState.BlackCastlingRights.HasFlag(BoardState.CastlingRights.KingSide))
                {
                    return false;
                }

                if (boardState.Pieces[(int)Piece.File.F, (int)Piece.Rank.Eight] != null ||
                    boardState.Pieces[(int)Piece.File.G, (int)Piece.Rank.Eight] != null)
                {
                    return false;
                }

                return !IsAttacked(boardState.Pieces, (int)Piece.File.E, (int)Piece.Rank.Eight) &&
                       !IsAttacked(boardState.Pieces, (int)Piece.File.F, (int)Piece.Rank.Eight) &&
                       !IsAttacked(boardState.Pieces, (int)Piece.File.G, (int)Piece.Rank.Eight);
            }
        }

        bool CanCastleQueenSide(BoardState boardState)
        {
            if (IsWhite)
            {
                if (!boardState.WhiteCastlingRights.HasFlag(BoardState.CastlingRights.QueenSide))
                {
                    return false;
                }

                if (boardState.Pieces[(int)Piece.File.B, (int)Piece.Rank.One] != null ||
                    boardState.Pieces[(int)Piece.File.C, (int)Piece.Rank.One] != null ||
                    boardState.Pieces[(int)Piece.File.D, (int)Piece.Rank.One] != null)
                {
                    return false;
                }

                return !IsAttacked(boardState.Pieces, (int)Piece.File.E, (int)Piece.Rank.One) &&
                       !IsAttacked(boardState.Pieces, (int)Piece.File.D, (int)Piece.Rank.One) &&
                       !IsAttacked(boardState.Pieces, (int)Piece.File.C, (int)Piece.Rank.One);
            }
            else
            {
                if (!boardState.BlackCastlingRights.HasFlag(BoardState.CastlingRights.QueenSide))
                {
                    return false;
                }

                if (boardState.Pieces[(int)Piece.File.B, (int)Piece.Rank.Eight] != null ||
                    boardState.Pieces[(int)Piece.File.C, (int)Piece.Rank.Eight] != null ||
                    boardState.Pieces[(int)Piece.File.D, (int)Piece.Rank.Eight] != null)
                {
                    return false;
                }

                return !IsAttacked(boardState.Pieces, (int)Piece.File.E, (int)Piece.Rank.Eight) &&
                       !IsAttacked(boardState.Pieces, (int)Piece.File.D, (int)Piece.Rank.Eight) &&
                       !IsAttacked(boardState.Pieces, (int)Piece.File.C, (int)Piece.Rank.Eight);
            }
        }

        public bool IsAttacked(Piece[,] pieces, int fileOverride = -1, int rankOverride = -1)
        {
            int currentFile = fileOverride == -1 ? (int)CurrentFile : fileOverride;
            int currentRank = rankOverride == -1 ? (int)CurrentRank : rankOverride;

            return IsPositionAttacked(pieces, IsWhite, currentFile, currentRank);
        }
    }
}
