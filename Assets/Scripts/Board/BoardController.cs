using Board.MouseClickData;
using Board.Pieces;
using Board.Pieces.Moves;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static Board.BoardMarkers.Highlighting;

namespace Board.BoardMarkers
{
    public class BoardController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] PiecePrefabs piecePrefabs;

        public MouseData rightClickData;
        public MouseData leftClickData;

        public Highlighting highlighting;
        public Arrows arrows;
        public MoveDisplayManager moveDisplayManager;
        public GameObject piecesContainer;

        public RectTransform pieceContainer;

        BoardState _boardState;
        RectTransform _transform;

        Piece _highlightedPiece;
        IEnumerable<MoveData> _highlightedPieceMoves;

        void Start()
        {
            if (this.gameObject.transform is RectTransform rectTransform)
            {
                _transform = rectTransform;
            }
            else
            {
                Debug.LogError("This GameObject needs to be a RectTransform for ParentClamp to work.");
            }

            _boardState = new BoardState(pieceContainer, piecePrefabs);
            _boardState.SetFEN(BoardState.DefaultFEN);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                highlighting.ClearAll(HighlightType.Left);

                rightClickData.FromPosition = ToLocalPosition(eventData.pressPosition);
                rightClickData.IsMouseDown = true;
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                highlighting.ClearAll(HighlightType.Right);
                highlighting.ClearAll(HighlightType.Left);
                arrows.ClearAll();

                leftClickData.FromPosition = ToLocalPosition(eventData.pressPosition);
                leftClickData.IsMouseDown = true;

                if (_highlightedPieceMoves != null && _highlightedPieceMoves.Any(x => (int)x.File == leftClickData.FromPosition.x && (int)x.Rank == leftClickData.FromPosition.y))
                {
                    MovePiece(leftClickData.FromPosition);
                }
                else
                {
                    moveDisplayManager.ClearMarkers();

                    _highlightedPiece = _boardState.GetPieceInfo(out _highlightedPieceMoves, leftClickData.FromPosition.x, leftClickData.FromPosition.y);
                    if (_highlightedPiece != null)
                    {
                        moveDisplayManager.SpawnMoves(_highlightedPieceMoves);
                        highlighting.Highlight(leftClickData.FromPosition.x, leftClickData.FromPosition.y, false);
                    }
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (!rightClickData.IsMouseDown)
                {
                    return;
                }

                rightClickData.ToPosition = ToLocalPosition(eventData.position);

                if (rightClickData.ToPosition == rightClickData.FromPosition)
                {
                    highlighting.Highlight(rightClickData.ToPosition.x, rightClickData.ToPosition.y, true);
                    return;
                }
                else
                {
                    arrows.UpdateArrow(rightClickData);
                }

                rightClickData.IsMouseDown = false;
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                leftClickData.ToPosition = ToLocalPosition(eventData.position);

                if (_highlightedPiece != null && leftClickData.IsMouseDown)
                {
                    if (_highlightedPieceMoves != null && _highlightedPieceMoves.Any(x => (int)x.File == leftClickData.ToPosition.x && (int)x.Rank == leftClickData.ToPosition.y))
                    {
                        MovePiece(leftClickData.ToPosition);
                    }
                    else
                    {
                        _highlightedPiece.UpdatePosition();
                    }
                }

                leftClickData.IsMouseDown = false;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            rightClickData.IsMouseDown = false;
            leftClickData.IsMouseDown = false;

            _highlightedPiece?.UpdatePosition();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_highlightedPiece != null && leftClickData.IsMouseDown)
            {
                _highlightedPiece.transform.localPosition = pieceContainer.transform.InverseTransformPoint(eventData.position);
            }
        }

        void MovePiece(Vector2Int toPosition)
        {
            highlighting.SetLastMove
                ( new Vector2Int((int)_highlightedPiece.CurrentFile, (int)_highlightedPiece.CurrentRank)
                , toPosition
                );
            highlighting.ClearAll();

            _boardState.MovePiece((int)_highlightedPiece.CurrentFile, (int)_highlightedPiece.CurrentRank, toPosition.x, toPosition.y);
            _highlightedPiece = null;
            _highlightedPieceMoves = null;

            moveDisplayManager.ClearMarkers();
        }

        Vector2Int ToLocalPosition(Vector2 position)
        {
            Vector3 outPosition = transform.InverseTransformPoint(position);
            outPosition.x = 8 * Mathf.Clamp(outPosition.x / _transform.rect.width + 0.5f, 0, 1);
            outPosition.y = 8 * Mathf.Clamp(outPosition.y / _transform.rect.height + 0.5f, 0, 1);
            return new Vector2Int((int)outPosition.x, (int)outPosition.y);
        }
    }
}