using Board.BoardMarkers;
using Board.History.Pairs;
using MoveTrainer;
using NUnit.Framework;
using SimpleFileBrowser;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Board.History
{
    public class BoardHistory : MonoBehaviour
    {
        [System.Serializable]
        public struct MovePositionData
        {
            public float indexX;

            public float x;
            public float y;
            public float dx;
            public float dy;
        }

        public struct MoveView
        {
            public int Index;
            public bool IsWhite;
        }

        public bool IsLookingAtLatestMove
        {
            get
            {
                return (_viewingMove.Index == _latestMove.Index && _viewingMove.IsWhite == _latestMove.IsWhite);
            }
        }


        public MovePositionData movePositionData;

        MoveView _latestMove = new MoveView() { Index = 0, IsWhite = true };
        MoveView _viewingMove = new MoveView() { Index = 0, IsWhite = true };

        List<MoveLabelPair> _moveLabels = new List<MoveLabelPair>();
        List<MovePair> _moves = new List<MovePair>();
        public BoardState _boardState;
        public BoardController _boardController;
        public Trainer _trainer;

        public MoveIndexLabel moveIndexLabelPrefab;
        public MoveLabel moveLabelPrefab;
        public GameObject MoveListObject;

        public string StartingFen;

        private void Awake()
        {
            AddMovePair();
        }

        private void Update()
        {
            if (FileBrowser.IsOpen)
                return;

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                StepInOneMove();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                StepOutOneMove();
            }
        }

        void StepInOneMove()
        {
            if (IsLookingAtLatestMove)
            {
                return;
            }

            if (_viewingMove.IsWhite)
            {
                GoToMove(_viewingMove.Index, false);
            }
            else
            {
                GoToMove(_viewingMove.Index + 1, true);
            }
        }

        void StepOutOneMove()
        {
            if (_viewingMove.Index == 0 && _viewingMove.IsWhite)
            {
                return;
            }

            if (_viewingMove.IsWhite)
            {
                GoToMove(_viewingMove.Index - 1, false);
            }
            else
            {
                GoToMove(_viewingMove.Index, true);
            }
        }

        public void SetBoardState(BoardState boardState)
        {
            _boardState = boardState;
        }

        public void GoToMove(int index, bool isWhite)
        {
            bool markForAnimation = false;
            bool animateInReverse = false;
            Move previousMove = null;

            if (_viewingMove.IsWhite)
            {
                if (!isWhite && index == _viewingMove.Index)
                {
                    markForAnimation = true;
                }
                else if (!isWhite && index == _viewingMove.Index - 1)
                {
                    markForAnimation = true;
                    animateInReverse = true;
                    previousMove = _moves[index + 1].White;
                }
            }
            else
            {
                if (isWhite && index == _viewingMove.Index + 1)
                {
                    markForAnimation = true;
                }
                else if (isWhite && index == _viewingMove.Index)
                {
                    markForAnimation = true;
                    animateInReverse = true;
                    previousMove = _moves[index].Black;
                }
            }

            _viewingMove = new MoveView() { Index = index, IsWhite = isWhite };

            if (_boardState == null)
            {
                Debug.LogError("BoardState is not set in BoardHistory.");
            }

            Move move = isWhite ? _moves[index].White : _moves[index].Black;

            _boardController.ViewingOldMove((int)move.FromFile, (int)move.FromRank, (int)move.ToFile, (int)move.ToRank);
            
            if (markForAnimation && !animateInReverse)
            {
                _boardState.MovePiece(move.ToString(), move.IsWhite, false, false);
            }
            else
            {
                _boardState.SetFEN(move.resultingFen);
            }

            if (markForAnimation)
            {
                if (animateInReverse)
                {
                    _boardController.AnimatePieceMove
                        ( (int)previousMove.FromFile
                        , (int)previousMove.FromRank
                        , (int)previousMove.ToFile
                        , (int)previousMove.ToRank
                        , (int)previousMove.FromFile
                        , (int)previousMove.FromRank
                        , previousMove.GetMoveAudio()
                        );
                }
                else
                {
                    _boardController.AnimatePieceMove
                        ( (int)move.ToFile
                        , (int)move.ToRank
                        , (int)move.FromFile
                        , (int)move.FromRank
                        , (int)move.ToFile
                        , (int)move.ToRank
                        , move.GetMoveAudio()
                        );
                }
            }
        }

        public void AddMove(Move move)
        {
            MoveLabel moveLabel = Instantiate<MoveLabel>(moveLabelPrefab, MoveListObject.transform);
            moveLabel.SetMove(move);
            moveLabel.transform.localPosition = new Vector3
                ( movePositionData.x + movePositionData.dx * (move.IsWhite ? 0 : 1)
                , movePositionData.y - movePositionData.dy * _moveLabels.Count
                , 0
                );

            if (!move.IsWhite)
            {
                _moves.Last().Black = move;
                _moveLabels.Last().Black = moveLabel;

                int moveCount = _moves.Count;
                moveLabel.SetCallback(() => GoToMove(moveCount - 1, false));
                _latestMove = new MoveView() { Index = moveCount - 1, IsWhite = false };
                _viewingMove = _latestMove;

                AddMovePair();
            }
            else
            {
                _moves.Last().White = move;
                _moveLabels.Last().White = moveLabel;

                int moveCount = _moves.Count;
                moveLabel.SetCallback(() => GoToMove(moveCount - 1, true));

                _latestMove = new MoveView() { Index = moveCount - 1, IsWhite = true };
                _viewingMove = _latestMove;
            }
        }

        void AddMovePair()
        {
            _moveLabels.Add(new MoveLabelPair());
            _moves.Add(new MovePair());

            MoveIndexLabel indexLabel = Instantiate<MoveIndexLabel>(moveIndexLabelPrefab, MoveListObject.transform);
            indexLabel.SetMove(_moves.Count);
            indexLabel.transform.localPosition = new Vector3
                ( movePositionData.indexX
                , movePositionData.y - movePositionData.dy * _moveLabels.Count
                , 0
                );
            _moveLabels.Last().Index = indexLabel;
        }

        public void ClearHistory()
        {
            if (FileBrowser.IsOpen)
                return;

            _moves.Clear();
            foreach (var moveLabel in _moveLabels)
            {
                if (moveLabel.Index != null)
                    GameObject.Destroy(moveLabel.Index.gameObject);
                if (moveLabel.White != null)
                    GameObject.Destroy(moveLabel.White.gameObject);
                if (moveLabel.Black != null)
                    GameObject.Destroy(moveLabel.Black.gameObject);
            }

            _moveLabels.Clear();

            _latestMove = new MoveView() { Index = 0, IsWhite = true };
            _viewingMove = new MoveView() { Index = 0, IsWhite = true };
            
            _boardState.SetFEN(StartingFen);
            _boardController.ClearAllHighlights();
            
            AddMovePair();
        }

        public void AddVariationToTrainer()
        {
            _trainer.AddVariation(GetMoves().Select(x => (x.ToString(), x.resultingFen)));
        }

        public IEnumerable<Move> GetMoves()
        {
            foreach (var movePair in _moves)
            {
                if (movePair.White != null)
                {
                    yield return movePair.White;
                }
                else
                {
                    yield break;
                }

                if (movePair.Black != null)
                {
                    yield return movePair.Black;
                }
                else
                {
                    yield break;
                }
            }
        }
    }
}
