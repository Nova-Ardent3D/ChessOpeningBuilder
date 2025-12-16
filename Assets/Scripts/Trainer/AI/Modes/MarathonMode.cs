using Common;
using System.Collections.Generic;
using System.Linq;
using Trainer.Data;
using Trainer.Data.Moves;
using UnityEngine;

namespace Trainer.AI.Modes
{
    public class MarathonMode : Mode
    {
        bool _isUniqueMode;

        int _marathonIndex;
        bool _marathonIndexWasFailed = false;
        int _variationDepth;

        public MarathonMode(bool isUniqueMode)
        {
            _isUniqueMode = isUniqueMode;
        }

        public override void AdditionalSetup()
        {
            _marathonIndex = Mathf.Max(TrainerData.Depth, 1);
            _variationDepth = 0;
        }

        public override void BuildTrainingSession()
        {
            CurrentTrainingSession = new TrainingSession();
            CurrentTrainingSession.Variations = new List<Variation>();
            CurrentTrainingSession.FailedVariations = new List<Variation>();
            CurrentTrainingSession.Index = 0;

            _marathonIndexWasFailed = false; 
            _variationDepth = 0;

            BuildVariations(StartingMove, 0);
            _marathonIndex++;


            if (_isUniqueMode)
            {
                CurrentTrainingSession.Variations = CurrentTrainingSession.Variations
                    .Where(v => v.MoveList.Count == _marathonIndex)
                    .ToList();

                if (CurrentTrainingSession.Variations.Count == 0)
                {
                    return;
                }
            }

            CurrentTrainingSession.Variations.Shuffle();
        }

        public override void BuildVariations(TrainerMoveInformation trainerMoveInformation, int depth)
        {
            _variationDepth = Mathf.Max(depth, _variationDepth);

            trainerMoveInformation.TimesCorrect = 0;
            trainerMoveInformation.TimesGuessed = 0;

            trainerMoveInformation.VariationTimesGuessed = 0;
            trainerMoveInformation.VariationTimesCorrect = 0;

            if (depth == _marathonIndex)
            {
                CurrentTrainingSession.Variations.Add(new Variation()
                {
                    WasPerfect = true,
                    WasPerfectThisIteration = true,
                    MoveList = trainerMoveInformation.GetMoveChain()
                });
            }
            else if (trainerMoveInformation.PossibleNextMoves.Count == 0)
            {
                CurrentTrainingSession.Variations.Add(new Variation()
                {
                    WasPerfect = true,
                    WasPerfectThisIteration = true,
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

        public override bool IncrementVariation()
        {
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
                    return true;
                }
            }

            CurrentTrainingSession.Index++;
            if (CurrentTrainingSession.Index >= CurrentTrainingSession.Variations.Count)
            {
                if (TrainerData.Method == TrainerData.TrainingMethod.ReplayFailedMoves && CurrentTrainingSession.FailedVariations.Count > 0)
                {
                    CurrentTrainingSession.Variations.AddRange(CurrentTrainingSession.FailedVariations);
                    CurrentTrainingSession.FailedVariations.Clear();
                    return true;
                }
                else
                {
                    if (_marathonIndexWasFailed)
                    {
                        _marathonIndex--;
                    }

                    BuildTrainingSession();
                    if (CurrentTrainingSession.Variations == null || CurrentTrainingSession.Variations.Count == 0)
                    {
                        return false;
                    }

                    if (_marathonIndex - 1 > _variationDepth)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override void MarkFailure()
        {
            base.MarkFailure();
            _marathonIndexWasFailed = true;
        }

        public override string ToString()
        {
            return $"Marathon Depth {_marathonIndex}\nVariation {CurrentTrainingSession.Index + 1} / {CurrentTrainingSession.Variations.Count}";
        }
    }
}
