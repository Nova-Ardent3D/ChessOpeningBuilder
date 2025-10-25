using Board.BoardMarkers;
using Board.Pieces.Moves;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Board.Pieces
{
    public class Queen : Piece
    {
        static readonly Vector2[] MoveDirections = new Vector2[]
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
                for (int i = 1; i < 8; i++)
                {
                    int targetFile = (int)CurrentFile + (int)direction.x * i;
                    int targetRank = (int)CurrentRank + (int)direction.y * i;
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
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
