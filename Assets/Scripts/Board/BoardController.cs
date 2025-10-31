using Board.Audio;
using Board.BoardMarkers.Letters;
using Board.BoardMarkers.Promotion;
using Board.History;
using Board.MouseClickData;
using Board.Pieces;
using Board.Pieces.Moves;
using SimpleFileBrowser;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using static Board.BoardMarkers.Highlighting;

namespace Board.BoardMarkers
{
    public class BoardController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] PiecePrefabs piecePrefabs;

        public MouseData rightClickData;
        public MouseData leftClickData;

        public Ranks ranks;
        public Files files;
        public Highlighting highlighting;
        public Arrows arrows;
        public MoveDisplayManager moveDisplayManager;
        public GameObject piecesContainer;
        public MoveAudio moveAudio;
        public PromotionModule promotionModule;

        public BoardHistory boardHistory;

        public RectTransform pieceContainer;

        BoardState _boardState;
        RectTransform _transform;
        bool _isRotated;

        Piece _highlightedPiece;
        IEnumerable<MoveData> _highlightedPieceMoves;

        Piece _pieceBeingAnimation;

        [SerializeField] bool OpeningIsWhite = true;

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

            _boardState = new BoardState(boardHistory, moveAudio, pieceContainer, piecePrefabs);
            _boardState.SetStartingFEN(BoardState.DefaultFEN);
        }

        void Update()
        {
            if (_pieceBeingAnimation != null && _pieceBeingAnimation.UpdateAnimation())
            {
                _pieceBeingAnimation = null;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (FileBrowser.IsOpen)
                return;

            if (promotionModule.IsActive)
                return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                highlighting.ClearAll(HighlightType.Left);

                rightClickData.FromPosition = ToLocalPosition(eventData.pressPosition);
                rightClickData.IsMouseDown = true;
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (!boardHistory.IsLookingAtLatestMove)
                    return;

                highlighting.ClearAll(HighlightType.Right);
                highlighting.ClearAll(HighlightType.Left);
                arrows.ClearAll();

                leftClickData.FromPosition = ToLocalPosition(eventData.pressPosition);
                leftClickData.IsMouseDown = true;

                if (_highlightedPieceMoves != null && _highlightedPieceMoves.Any(x => (int)x.File == leftClickData.FromPosition.x && (int)x.Rank == leftClickData.FromPosition.y))
                {
                    MovePiece(_highlightedPieceMoves.First(x => (int)x.File == leftClickData.FromPosition.x && (int)x.Rank == leftClickData.FromPosition.y));
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
            if (FileBrowser.IsOpen)
                return;

            if (promotionModule.IsActive)
                return;

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
                if (!boardHistory.IsLookingAtLatestMove)
                    return;

                leftClickData.ToPosition = ToLocalPosition(eventData.position);

                if (_highlightedPiece != null && leftClickData.IsMouseDown)
                {
                    if (_highlightedPieceMoves != null && _highlightedPieceMoves.Any(x => (int)x.File == leftClickData.ToPosition.x && (int)x.Rank == leftClickData.ToPosition.y))
                    {
                        MovePiece(_highlightedPieceMoves.First(x => (int)x.File == leftClickData.ToPosition.x && (int)x.Rank == leftClickData.ToPosition.y));
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
            if (FileBrowser.IsOpen)
                return;

            rightClickData.IsMouseDown = false;
            leftClickData.IsMouseDown = false;

            _highlightedPiece?.UpdatePosition();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (FileBrowser.IsOpen)
                return;

            if (promotionModule.IsActive)
                return;

            if (_highlightedPiece != null && leftClickData.IsMouseDown)
            {
                _highlightedPiece.transform.localPosition = pieceContainer.transform.InverseTransformPoint(eventData.position);
            }
        }

        void MovePiece(MoveData moveData)
        {
            if (_pieceBeingAnimation != null)
            {
                _pieceBeingAnimation.ForceFinishAnimation();
                _pieceBeingAnimation = null;
            }

            if (moveData.IsPromotion)
            {
                highlighting.ClearAll();

                promotionModule.Spawn(_highlightedPiece.IsWhite, moveData.File, moveData.Rank
                , (piece) => {
                    highlighting.SetLastMove
                        ( new Vector2Int((int)_highlightedPiece.CurrentFile, (int)_highlightedPiece.CurrentRank)
                        , new Vector2Int((int)moveData.File, (int)moveData.Rank)
                        );
                    highlighting.ClearAll();

                    _boardState.MovePiece((int)_highlightedPiece.CurrentFile, (int)_highlightedPiece.CurrentRank, (int)moveData.File, (int)moveData.Rank, moveData.Type, piece);
                    _highlightedPiece = null;
                    _highlightedPieceMoves = null;

                    moveDisplayManager.ClearMarkers();
                }
                , () => {
                    _highlightedPiece.UpdatePosition();
                });
            }
            else
            {
                highlighting.SetLastMove
                        ( new Vector2Int((int)_highlightedPiece.CurrentFile, (int)_highlightedPiece.CurrentRank)
                        , new Vector2Int((int)moveData.File, (int)moveData.Rank)
                        );
                highlighting.ClearAll();

                _boardState.MovePiece((int)_highlightedPiece.CurrentFile, (int)_highlightedPiece.CurrentRank, (int)moveData.File, (int)moveData.Rank, moveData.Type);
                _highlightedPiece = null;
                _highlightedPieceMoves = null;

                moveDisplayManager.ClearMarkers();
            }
        }

        Vector2Int ToLocalPosition(Vector2 position)
        {
            Vector3 outPosition = transform.InverseTransformPoint(position);
            outPosition.x = 8 * Mathf.Clamp(outPosition.x / _transform.rect.width + 0.5f, 0, 1);
            outPosition.y = 8 * Mathf.Clamp(outPosition.y / _transform.rect.height + 0.5f, 0, 1);
            return new Vector2Int((int)outPosition.x, (int)outPosition.y);
        }

        public void RotateBoard()
        {
            _isRotated = ! _isRotated;
            if (!_isRotated)
                _transform.rotation = Quaternion.Euler(0, 0, 0);
            else
                _transform.rotation = Quaternion.Euler(0, 0, 180);

            ranks.UpdateRotation(_isRotated);
            files.UpdateRotation(_isRotated);

            foreach (var piece in _boardState.Pieces)
            {
                if (piece != null)
                    piece.UpdateRotation(_isRotated);
            }
        }

        public void ViewingOldMove(int x1, int y1, int x2, int y2)
        {
            if (promotionModule.IsActive)
                promotionModule.CancelPressed();

            _highlightedPiece = null;
            _highlightedPieceMoves = null;

            highlighting.SetLastMove(new Vector2Int(x1, y1), new Vector2Int(x2, y2));
            highlighting.ClearAll();
            arrows.ClearAll();
        }

        public void AnimatePieceMove(int pieceFile, int pieceRank, int fromFile, int fromRank, int toFile, int toRank, MoveAudio.Clips audio)
        {
            _pieceBeingAnimation = _boardState.Pieces[pieceFile, pieceRank];
            _pieceBeingAnimation.StartAnimation(fromFile, fromRank, toFile, toRank, () => 
            {
                moveAudio.Play(audio);
            });
        }

        public void ClearAllHighlights()
        {
            highlighting.ClearLastMove();
            highlighting.ClearAll();
            arrows.ClearAll();
        }
    }
}