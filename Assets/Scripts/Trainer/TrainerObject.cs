using Board.Moves;
using Trainer.Data;
using Trainer.Data.Moves;
using Trainer.MoveViewer;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Board;
using Board.Common;
using History.MoveHistory;
using System.Collections;
using Trainer.Options;
using Trainer.AI;

namespace Trainer
{
    public class TrainerObejct : MonoBehaviour
    {
        [SerializeField] AIController _aiController;
        [SerializeField] BoardObject _boardObject;
        [SerializeField] BoardHistoryObject _boardHistory;

        [SerializeField] TrainerMoveInfoContainer _previousMoveContainer;
        [SerializeField] TrainerMoveInfoContainer _currentMoveContainer;
        [SerializeField] TrainerMoveInfoContainer _nextMoveContainer;

        [SerializeField] TrainerOptionsObject _trainerOptionsObject;

        TrainerMoveInformation _previousMove = null;
        TrainerMoveInformation _currentMove = null;
        List<TrainerMoveInformation> _nextPossibleMoves = null;

        private void Awake()
        {
            if (_currentMove == null)
            {
                UpdateViewedMove(_trainerOptionsObject.TrainerData.StartingMove);
            }
        }

        public void Run()
        {
            _aiController.StartTraining(_trainerOptionsObject.TrainerData);
        }

        public void RunFrom()
        {
            _aiController.StartTraining(_trainerOptionsObject.TrainerData, _currentMove);
        }

        public void UpdateViewedMove(TrainerMoveInformation trainerMoveInformation)
        {
            if (trainerMoveInformation == null)
            {
                trainerMoveInformation = _trainerOptionsObject.TrainerData.StartingMove;
            }

            _currentMove = trainerMoveInformation;
            _previousMove = trainerMoveInformation.ParentMove;
            _nextPossibleMoves = trainerMoveInformation.PossibleNextMoves;

            if (_currentMove != null)
            {
                _currentMoveContainer.SetMoves(_trainerOptionsObject.TrainerData.StatsDisplay, _currentMove);
                TrainerMoveInformationDisplay currentMove = _currentMoveContainer.Moves.First();
                currentMove.SetCallBack(x =>
                {
                    StartCoroutine(SetChain(x));
                });
            }
            else
            {
                _currentMoveContainer.ClearMoves();
            }

            if (_previousMove != null)
            {
                _previousMoveContainer.SetMoves(_trainerOptionsObject.TrainerData.StatsDisplay, _previousMove);
                foreach (var move in _previousMoveContainer.Moves)
                {
                    move.SetCallBack(UpdateViewedMove);
                }
            }
            else
            {
                _previousMoveContainer.ClearMoves();
            }

            if (_nextPossibleMoves != null && _nextPossibleMoves.Count > 0)
            {
                _nextMoveContainer.SetMoves(_trainerOptionsObject.TrainerData.StatsDisplay, _nextPossibleMoves);
                foreach (var move in _nextMoveContainer.Moves)
                {
                    move.SetCallBack(UpdateViewedMove);
                }
            }
            else
            {
                _nextMoveContainer.ClearMoves();
            }
        }

        public void AddVariation()
        {
            bool brokenChain = false;
            TrainerMoveInformation currentMove = _trainerOptionsObject.TrainerData.StartingMove;

            foreach (MoveInformation move in _boardObject.GetMoveHistory())
            {
                if (!brokenChain && currentMove.PossibleNextMoves.Any(x => x.MoveNotation == move.ToString()))
                {
                    currentMove = currentMove.PossibleNextMoves.First(x => x.MoveNotation == move.ToString());
                }
                else
                {
                    brokenChain = true;

                    TrainerMoveInformation newMove = new TrainerMoveInformation()
                    {
                        ParentMove = currentMove,
                        MoveNotation = move.ToString(),
                        HintOne = $"{move.From.File.AsText()}{move.From.Rank.AsText()}",
                        HintTwo = $"{move.To.File.AsText()}{move.To.Rank.AsText()}",
                        Color = currentMove.Color == PieceColor.White ? PieceColor.Black : PieceColor.White
                    };

                    currentMove.PossibleNextMoves.Add(newMove);
                    currentMove = newMove;
                }
            }

            UpdateViewedMove(_currentMove);
        }

        public IEnumerator SetChain(TrainerMoveInformation leaf)
        {
            yield return new WaitUntil(() => _boardObject.CanMakeMoves());

            _boardHistory.ClearHistory();
            foreach (var move in leaf.GetMoveChain())
            {
                _boardObject.MovePieceAlgebraic(move.MoveNotation, false, false);
            }
        }

        public void RemoveBranch()
        {
            if (_currentMove.ParentMove == null)
            {
                return;
            }

            _currentMove.PossibleNextMoves.Clear();
            _currentMove.ParentMove.PossibleNextMoves.Remove(_currentMove);
            UpdateViewedMove(_currentMove.ParentMove);
        }
    }
}

