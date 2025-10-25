using Board.BoardMarkers;
using Board.MouseClickData;
using Board.Pieces.Moves;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Board.Pieces
{
    public class Piece : MonoBehaviour
    {
        public enum File : int
        {
            A,
            B,
            C,
            D,
            E,
            F,
            G,
            H
        }

        public enum Rank : int
        {
            One,
            Two,
            Three,
            Four,
            Five,
            Six,
            Seven,
            Eight
        }

        [SerializeField] RectTransform Transform;

        [SerializeField] File _currentFile;
        [SerializeField] Rank _currentRank;
        [SerializeField] bool _isWhite;

        public bool IsWhite
        {
            get { return _isWhite; }
        }

        public File CurrentFile
        {
            get { return _currentFile; }
            set 
            {
                _currentFile = value;
                UpdatePosition();
            }
        }

        public Rank CurrentRank
        {
            get { return _currentRank; }
            set 
            {
                _currentRank = value;
                UpdatePosition();
            }
        }

        private void Awake()
        {
            UpdatePosition();
        }

        public virtual IEnumerable<MoveData> GetMoves(BoardState boardState)
        {
            yield break;
        }

        public void UpdatePosition()
        {
            int x = (int)_currentFile;
            int y = (int)_currentRank;
            Vector2 p1 = 100 * new Vector2(x, y) - new Vector2(350, 350);

            Transform.localPosition = p1;
        }

        protected static bool IsPositionAttacked(Piece[,] pieces, bool isWhite, int file, int rank)
        {
            foreach (var knightDirections in Knight.MoveDirections)
            {
                int targetFile = (int)file + (int)knightDirections.x;
                int targetRank = (int)rank + (int)knightDirections.y;
                if (targetFile < 0 || targetFile > 7 || targetRank < 0 || targetRank > 7)
                {
                    continue;
                }

                Piece piece = pieces[targetFile, targetRank];
                if (piece is Knight knight)
                {
                    if (knight.IsWhite != isWhite)
                    {
                        return true;
                    }
                }
            }

            foreach (var bishopDirection in Bishop.MoveDirections)
            {
                for (int i = 1; i < 8; i++)
                {
                    int targetFile = (int)file + (int)bishopDirection.x * i;
                    int targetRank = (int)rank + (int)bishopDirection.y * i;
                    if (targetFile < 0 || targetFile > 7 || targetRank < 0 || targetRank > 7)
                    {
                        break;
                    }

                    Piece piece = pieces[targetFile, targetRank];

                    if (piece == null)
                    {
                        continue;
                    }
                    else if (piece.IsWhite == isWhite)
                    {
                        break;
                    }
                    else if (piece is Bishop || piece is Queen)
                    {
                        return true;
                    }
                }
            }

            foreach (var rookDirection in Rook.MoveDirections)
            {
                for (int i = 1; i < 8; i++)
                {
                    int targetFile = (int)file + (int)rookDirection.x * i;
                    int targetRank = (int)rank + (int)rookDirection.y * i;
                    if (targetFile < 0 || targetFile > 7 || targetRank < 0 || targetRank > 7)
                    {
                        break;
                    }

                    Piece piece = pieces[targetFile, targetRank];

                    if (piece == null)
                    {
                        continue;
                    }
                    else if (piece.IsWhite == isWhite)
                    {
                        break;
                    }
                    else if (piece is Rook || piece is Queen)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }
}