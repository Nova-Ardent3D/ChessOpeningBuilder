using UnityEngine;
using static Board.Pieces.Piece;

namespace Board.BoardMarkers
{
    public class Move : MonoBehaviour
    {
        [SerializeField] RectTransform Transform;

        [SerializeField] File _currentFile;
        [SerializeField] Rank _currentRank;
        [SerializeField] bool _isTake;

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

        void UpdatePosition()
        {
            int x = (int)_currentFile;
            int y = (int)_currentRank;
            Vector2 p1 = 100 * new Vector2(x, y) - new Vector2(350, 350);

            Transform.localPosition = p1;
        }
    }
}