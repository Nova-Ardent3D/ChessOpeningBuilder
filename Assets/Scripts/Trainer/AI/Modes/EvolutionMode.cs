using Common;
using System.Collections.Generic;
using System.Linq;
using Trainer.Data;
using Trainer.Data.Moves;
using UnityEngine;

namespace Trainer.AI.Modes
{
    public class EvolutionMode : Mode
    {
        int _messups;
        bool _initialized = false;

        List<Variation> _currentEvolution = new List<Variation>();

        public override void BuildVariations(TrainerMoveInformation trainerMoveInformation, int depth)
        {
            if (!_initialized)
            {
                foreach (var move in trainerMoveInformation.PossibleNextMoves)
                {
                    _currentEvolution.Add(new Variation()
                    {
                        WasPerfect = true,
                        WasPerfectThisIteration = true,
                        MoveList = move.GetMoveChain()
                    });
                }

                _initialized = true;
            }
            else
            {
                List<Variation> nextEvolution = new List<Variation>();

                foreach (var move in _currentEvolution)
                {
                    if (move.WasPerfectThisIteration)
                    {
                        foreach (var nextMove in move.MoveList.Last().PossibleNextMoves)
                        {
                            nextEvolution.Add(new Variation()
                            {
                                WasPerfect = true,
                                WasPerfectThisIteration = true,
                                MoveList = nextMove.GetMoveChain()
                            });
                        }
                    }
                    else
                    {
                        nextEvolution.Add(new Variation()
                        {
                            WasPerfect = true,
                            WasPerfectThisIteration = true,
                            MoveList = move.MoveList.Last().GetMoveChain()
                        });
                    }
                }

                _currentEvolution = nextEvolution;
            }

            foreach (var move in _currentEvolution)
            {
                CurrentTrainingSession.Variations.Add(move);
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
                    BuildTrainingSession();
                    if (CurrentTrainingSession.Variations == null || CurrentTrainingSession.Variations.Count == 0)
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
            _messups++;
        }

        public override string ToString()
        {
            return $"Redos {_messups}\nCurrent Depth {CurrentTrainingSession.CurrentVariation.MoveList.Count}\nVariation {CurrentTrainingSession.Index + 1} / {CurrentTrainingSession.Variations.Count}";
        }
    }
}
