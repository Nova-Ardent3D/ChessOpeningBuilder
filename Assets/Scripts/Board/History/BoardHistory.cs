using Board.BoardMarkers;
using Board.History.Pairs;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
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
        }

        void StepOutOneMove()
        {
            if (_viewingMove.Index == 0 && _viewingMove.IsWhite)
            {
                return;
            }
        }

        public void SetBoardState(BoardState boardState)
        {
            _boardState = boardState;
        }

        public void SetBoardController(BoardController boardController)
        {
            _boardController = boardController;
        }

        public void GoToMove(int index, bool isWhite)
        {
            _viewingMove = new MoveView() { Index = index, IsWhite = isWhite  };

            if (_boardState == null)
            {
                Debug.LogError("BoardState is not set in BoardHistory.");
            }

            Move move = isWhite ? _moves[index].White : _moves[index].Black;

            _boardController.ViewingOldMove((int)move.FromFile, (int)move.FromRank, (int)move.ToFile, (int)move.ToRank);
            _boardState.SetFEN(move.resultingFen);
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
    }
}
