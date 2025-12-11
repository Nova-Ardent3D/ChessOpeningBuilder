using Board;
using Board.Common;
using Board.FlipBoard;
using Board.Moves;
using History.MoveHistory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Trainer.Data;
using Trainer.Data.Moves;
using UnityEngine;
using Common;

namespace Trainer.AI
{
    public class AIController : MonoBehaviour
    {
        [SerializeField] BoardObject _boardObject;
        [SerializeField] FlipBoardHandler _flipBoardHandler;

        [SerializeField] BoardHistoryObject _boardHistoryObject;

        [SerializeField] GameObject _nextVariationButton;
        [SerializeField] TextMeshProUGUI _CurrentVariationText;

        [System.Serializable]
        public struct NonTrainingModule
        {
            public GameObject Target;
            [NonSerialized] public bool WasActive;
        }

        [SerializeField] public NonTrainingModule[] NonTrainingModules;
        [SerializeField] public GameObject[] TrainingModules;

        public class Variation
        {
            public int CurrentMoveIndex = 0;
            public bool WasPerfect = true;
            public List<TrainerMoveInformation> MoveList;
        }

        public struct TrainingSession
        {
            public List<Variation> FailedVariations;
            public List<Variation> Variations;
            public Variation CurrentVariation;

            public int Index;
        }

        TrainerMoveInformation _startingMove = null;

        public TrainerData TrainerData;
        public bool IsTrainingActive = false;
        public TrainingSession CurrentTrainingSession;

        public bool HasUsedHint = false;
        public bool HasUsedBothHints = false;
        Coroutine _runner;

        public int _marathonIndex = 0;
        public int _variationDepth = 0;
        public bool _marathonIndexWasFailed = false;

        bool _buildingNewSession = false;

        private void Start()
        {
            _boardObject.RegisterOnMoveCallback(OnPlayerMoved);
        }

        private void Update()
        {
            if (IsTrainingActive && _nextVariationButton.activeSelf & Input.GetKey(KeyCode.Space))
            {
                LoadNextVariation();
            }
        }

        public void StartTraining(TrainerData trainerData, TrainerMoveInformation startingMove = null)
        {
            TurnOffNonTrainingModules();

            TrainerData = trainerData;

            _startingMove = startingMove;

            _variationDepth = 0;
            _marathonIndex = 1;

            BuildTrainingSession();
            if (CurrentTrainingSession.Variations == null || CurrentTrainingSession.Variations.Count == 0)
            {
                return;
            }

            IsTrainingActive = true;

            SetVariation();
            _runner = StartCoroutine(Run());
            _flipBoardHandler.SetBoardFlipped(TrainerData.Color == PieceColor.Black);
        }

        public void StopTraining()
        {
            TurnOnNonTrainingModules();

            TrainerData = null;
            CurrentTrainingSession = new TrainingSession();
            IsTrainingActive = false;

            if (_runner != null)
            {
                StopCoroutine(_runner);
            }
        }

        void TurnOffNonTrainingModules()
        {
            for (int i = 0; i < NonTrainingModules.Length; i++)
            {
                NonTrainingModules[i].WasActive = NonTrainingModules[i].Target.activeSelf;
                NonTrainingModules[i].Target.SetActive(false);
            }

            foreach (GameObject module in TrainingModules)
            {
                module.SetActive(true);
            }

            _nextVariationButton.SetActive(false);
        }

        void TurnOnNonTrainingModules()
        {
            for (int i = 0; i < NonTrainingModules.Length; i++)
            {
                if (NonTrainingModules[i].WasActive)
                    NonTrainingModules[i].Target.SetActive(true);
            }

            foreach (GameObject module in TrainingModules)
            {
                module.SetActive(false);
            }
        }

        void BuildTrainingSession()
        {
            CurrentTrainingSession = new TrainingSession();
            CurrentTrainingSession.Variations = new List<Variation>();
            CurrentTrainingSession.FailedVariations = new List<Variation>();
            CurrentTrainingSession.Index = 0;

            _variationDepth = 0;

            _marathonIndexWasFailed = false;

            if (TrainerData.DepthType == TrainerData.TrainerType.MarathonMode)
            {
                _marathonIndex++;
                BuildVariations(_startingMove ?? TrainerData.StartingMove, 0);

                if (_marathonIndex > _variationDepth)
                {
                    StopTraining();
                    return;
                }
            }
            else
            {
                BuildVariations(_startingMove ?? TrainerData.StartingMove, 0);
            }

            CurrentTrainingSession.Variations.Shuffle();
        }

        void BuildVariations(TrainerMoveInformation trainerMoveInformation, int depth)
        {
            _variationDepth = Math.Max(_variationDepth, depth);

            trainerMoveInformation.TimesCorrect = 0;
            trainerMoveInformation.TimesGuessed = 0;

            trainerMoveInformation.VariationTimesGuessed = 0;
            trainerMoveInformation.VariationTimesCorrect = 0;

            if ((depth == TrainerData.Depth && (TrainerData.DepthType == TrainerData.TrainerType.ByMoveCount || TrainerData.DepthType == TrainerData.TrainerType.MarathonMode))
                || (depth == _marathonIndex && TrainerData.DepthType == TrainerData.TrainerType.MarathonMode))
            {
                CurrentTrainingSession.Variations.Add(new Variation()
                {
                    WasPerfect = false,
                    MoveList = trainerMoveInformation.GetMoveChain()
                });
                return;
            }
            else if (TrainerData.DepthType == TrainerData.TrainerType.ByCompleteVariation && CurrentTrainingSession.Variations.Count == TrainerData.Depth)
            {
                return;
            }

            if (trainerMoveInformation.PossibleNextMoves.Count == 0)
            {
                CurrentTrainingSession.Variations.Add(new Variation()
                {
                    WasPerfect = false,
                    MoveList = trainerMoveInformation.GetMoveChain()
                });
            }
            else
            {
                foreach (var nextMove in trainerMoveInformation.PossibleNextMoves)
                {
                    BuildVariations(nextMove, depth + 1);
                }
            }
        }

        public void LoadNextVariation()
        {
            _nextVariationButton.SetActive(false);


            Variation CurrentVariation = CurrentTrainingSession.CurrentVariation;
            foreach (var move in CurrentVariation.MoveList)
            {
                move.VariationTimesGuessed++;
                if (CurrentVariation.WasPerfect)
                {
                    move.VariationTimesCorrect++;
                }
            }

            if (TrainerData.Method == TrainerData.TrainingMethod.ReplayFailedMoves)
            {
                if (!CurrentTrainingSession.CurrentVariation.WasPerfect)
                {
                    CurrentTrainingSession.FailedVariations.Add(CurrentTrainingSession.CurrentVariation);
                }
            }
            else if (TrainerData.Method == TrainerData.TrainingMethod.RepeatVariationOnFailed)
            {
                if (!CurrentTrainingSession.CurrentVariation.WasPerfect)
                {
                    SetVariation();
                    return;
                }
            }

            CurrentTrainingSession.Index++;
            if (CurrentTrainingSession.Index >= CurrentTrainingSession.Variations.Count)
            {
                if (TrainerData.Method == TrainerData.TrainingMethod.ReplayFailedMoves && CurrentTrainingSession.FailedVariations.Count > 0)
                {
                    CurrentTrainingSession.Variations.AddRange(CurrentTrainingSession.FailedVariations);
                    CurrentTrainingSession.FailedVariations.Clear();
                    SetVariation();
                }
                else
                {
                    if (TrainerData.DepthType == TrainerData.TrainerType.MarathonMode)
                    {
                        LoadNextDepthForMarathon();
                    }
                    else
                    {
                        StopTraining();
                    }
                }
            }
            else
            {
                SetVariation();
            }
        }

        void SetVariation()
        {
            _boardHistoryObject.ClearHistory();

            if (TrainerData.DepthType == TrainerData.TrainerType.MarathonMode)
            {
                _CurrentVariationText.text = $"Marathon Depth {_marathonIndex}\nVariation {CurrentTrainingSession.Index + 1} / {CurrentTrainingSession.Variations.Count}";
            }
            else
            {
                _CurrentVariationText.text = $"Variation {CurrentTrainingSession.Index + 1} / {CurrentTrainingSession.Variations.Count}";
            }
            CurrentTrainingSession.CurrentVariation = CurrentTrainingSession.Variations[CurrentTrainingSession.Index];
            CurrentTrainingSession.CurrentVariation.WasPerfect = true;
            CurrentTrainingSession.CurrentVariation.CurrentMoveIndex = 0;
        }

        void LoadNextDepthForMarathon()
        {
            StopCoroutine(_runner);

            if (_marathonIndexWasFailed)
            {
                _marathonIndex--;
            }

            BuildTrainingSession();
            
            if (IsTrainingActive)
            {
                SetVariation();
                _runner = StartCoroutine(Run());
            }
        }

        void OnPlayerMoved()
        {
            HasUsedHint = false;
            HasUsedBothHints = false;

            if (!IsTrainingActive)
            {
                return;
            }

            if (_boardObject.GetCurrentColor() == TrainerData.Color)
            {
                return;
            }

            Variation variation = CurrentTrainingSession.CurrentVariation;
            if (variation.CurrentMoveIndex >= variation.MoveList.Count)
            {
                return;
            }

            TrainerMoveInformation currentTrainerMove = variation.MoveList[variation.CurrentMoveIndex];

            MoveInformation moveInformation = _boardObject.GetMoveHistory().Last();
            if (moveInformation.ToString() == currentTrainerMove.MoveNotation)
            {
                currentTrainerMove.TimesGuessed++;
                currentTrainerMove.TimesCorrect++;
                variation.CurrentMoveIndex++;

                if (variation.CurrentMoveIndex >= variation.MoveList.Count)
                {
                    _nextVariationButton.SetActive(true);
                }
            }
            else
            {
                currentTrainerMove.TimesGuessed++;
                CurrentTrainingSession.CurrentVariation.WasPerfect = false;

                _boardHistoryObject.RemoveLatestMove();
                _boardHistoryObject.MarkLabelAsIncorrect(variation.CurrentMoveIndex);

                _marathonIndexWasFailed = true;
            }
        }

        IEnumerator Run()
        {
            while (true)
            {
                Variation variation = CurrentTrainingSession.CurrentVariation;
                if (variation.CurrentMoveIndex >= variation.MoveList.Count)
                {
                    yield return null;
                    continue;
                }

                if (_boardObject.GetCurrentColor() == TrainerData.Color)
                {
                    yield return null;
                    continue;
                }

                if (!_boardObject.CanMakeMoves())
                {
                    yield return null;
                    continue;
                }

                yield return new WaitForSeconds(0.25f);

                if (!_boardObject.CanMakeMoves())
                {
                    yield return null;
                    continue;
                }

                // if color is white, then bot plays black.
                if (_boardObject.GetCurrentColor() != TrainerData.Color)
                {
                    _boardObject.MovePieceAlgebraic(variation.MoveList[variation.CurrentMoveIndex].MoveNotation);
                    variation.CurrentMoveIndex++;

                    if (variation.CurrentMoveIndex >= variation.MoveList.Count)
                    {
                        _nextVariationButton.SetActive(true);
                    }
                }

                yield return null;
            }
        }


        public void Hint()
        {
            Variation variation = CurrentTrainingSession.CurrentVariation;
            if (variation.CurrentMoveIndex >= variation.MoveList.Count)
            {
                return;
            }

            TrainerMoveInformation currentTrainerMove = variation.MoveList[variation.CurrentMoveIndex];

            Files fromFile = currentTrainerMove.HintOne.ToLower()[0].ToFile();
            Ranks fromRank = currentTrainerMove.HintOne.ToLower()[1].ToRank();

            if (!HasUsedHint)
            {
                _boardObject.MarkBoard(fromFile, fromRank, fromFile, fromRank);
                currentTrainerMove.TimesGuessed++;
                variation.WasPerfect = false;
                HasUsedHint = true;
                _marathonIndexWasFailed = true;
            }
            else
            {
                Files toFile = currentTrainerMove.HintTwo.ToLower()[0].ToFile();
                Ranks toRank = currentTrainerMove.HintTwo.ToLower()[1].ToRank();
                _boardObject.MarkBoard(fromFile, fromRank, toFile, toRank);

                if (!HasUsedBothHints)
                {
                    currentTrainerMove.TimesGuessed++;
                }

                HasUsedBothHints = true;
                _marathonIndexWasFailed = true;
            }
        }
    }
}
