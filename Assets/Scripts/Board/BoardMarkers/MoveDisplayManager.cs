using UnityEngine;
using System.Collections.Generic;
using Board.Pieces.Moves;

namespace Board.BoardMarkers
{
    public class MoveDisplayManager : MonoBehaviour
    {
        public Move MoveMarkerPrefab;
        public Move TakeMarkerPrefab;

        Stack<Move> _movePool = new Stack<Move>();
        Stack<Move> _takePool = new Stack<Move>();

        Stack<Move> _activeMoves = new Stack<Move>();
        Stack<Move> _activeTakes = new Stack<Move>();

        public void SpawnMoves(IEnumerable<MoveData> moveData)
        {
            ClearMarkers();

            foreach (var move in moveData)
            {
                Move marker = GetMoveMarker(move);
                if (move.Type == MoveType.Move)
                {
                    _activeMoves.Push(marker);
                }
                else
                {
                    _activeTakes.Push(marker);
                }
            }
        }

        Move GetMoveMarker(MoveData move)
        {
            Move marker = null;
            if (move.Type == MoveType.Move)
            {
                if (_movePool.Count > 0)
                {
                    marker = _movePool.Pop();
                    marker.gameObject.SetActive(true);
                }
                else
                {
                    marker = Instantiate<Move>(MoveMarkerPrefab, this.transform);
                }
            }
            else
            {
                if (_takePool.Count > 0)
                {
                    marker = _takePool.Pop();
                    marker.gameObject.SetActive(true);
                }
                else
                {
                    marker = Instantiate<Move>(TakeMarkerPrefab, this.transform);
                }
            }

            marker.CurrentFile = move.File;
            marker.CurrentRank = move.Rank;
            return marker;
        }

        public void ClearMarkers()
        {
            while (_activeMoves.Count > 0)
            {
                var marker = _activeMoves.Pop();
                _movePool.Push(marker);
                marker.gameObject.SetActive(false);
            }

            while (_activeTakes.Count > 0)
            {
                var marker = _activeTakes.Pop();
                _takePool.Push(marker);
                marker.gameObject.SetActive(false);
            }
        }
    }
}
