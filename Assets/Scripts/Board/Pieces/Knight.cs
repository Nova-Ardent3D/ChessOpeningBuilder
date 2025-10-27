using Board.BoardMarkers;
using Board.Pieces.Moves;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Board.Pieces
{
    public class Knight : Piece
    {
        public override PieceTypes Type => PieceTypes.Knight;


        public static readonly Vector2[] MoveDirections = new Vector2[]
        {
            new Vector2(1, 2),
            new Vector2(2, 1),
            new Vector2(2, -1),
            new Vector2(1, -2),
            new Vector2(-1, -2),
            new Vector2(-2, -1),
            new Vector2(-2, 1),
            new Vector2(-1, 2),
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

                if (boardState.Pieces[targetFile, targetRank] != null)
                {
                    if (boardState.Pieces[targetFile, targetRank].IsWhite == IsWhite)
                    {
                        continue;
                    }

                    yield return new MoveData()
                    {
                        File = (File)targetFile,
                        Rank = (Rank)targetRank,
                        Type = MoveType.Take
                    };
                }
                else
                {
                    yield return new MoveData()
                    {
                        File = (File)targetFile,
                        Rank = (Rank)targetRank,
                        Type = MoveType.Move
                    };
                }
            }
        }

        public override IEnumerable<Piece> GetAttackingPiecesOfType(Piece[,] pieces, int fromX, int fromY)
        {
            foreach (var direction in MoveDirections)
            {
                int targetFile = (int)CurrentFile + (int)direction.x;
                int targetRank = (int)CurrentRank + (int)direction.y;

                if (targetFile < 0 || targetFile > 7 || targetRank < 0 || targetRank > 7)
                {
                    continue;
                }

                if (targetFile == fromX && targetRank == fromY)
                {
                    continue;
                }
                else if (pieces[targetFile, targetRank] == null)
                {
                    continue;
                }
                else if (pieces[targetFile, targetRank] is Knight knight && knight.IsWhite == IsWhite)
                {
                    yield return knight;
                }
            }
        }
    }
}