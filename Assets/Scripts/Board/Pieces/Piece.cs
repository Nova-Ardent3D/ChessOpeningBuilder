using Board.BoardMarkers;
using Board.MouseClickData;
using Board.Pieces.Moves;
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

    }
}