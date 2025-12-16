using Board;
using Common;
using System.Collections;
using System.Collections.Generic;
using Trainer.Data;
using Trainer.Data.Moves;
using UnityEngine;


namespace Trainer.AI.Modes
{
    public abstract class Mode
    {
        public class Variation
        {
            public int CurrentMoveIndex = 0;
            public bool WasPerfect = true;
            public bool WasPerfectThisIteration = true;
            public List<TrainerMoveInformation> MoveList;
        }

        public struct TrainingSession
        {
            public List<Variation> FailedVariations;
            public List<Variation> Variations;
            public Variation CurrentVariation;

            public int Index;
        }

        public TrainingSession CurrentTrainingSession;
        public TrainerMoveInformation StartingMove;
        public TrainerData TrainerData { get; private set; }

        public bool Initialize(TrainerData trainerData, TrainerMoveInformation initialMove)
        {
            TrainerData = trainerData;
            StartingMove = initialMove ?? trainerData.StartingMove;

            AdditionalSetup();
            BuildTrainingSession();

            if (CurrentTrainingSession.Variations == null || CurrentTrainingSession.Variations.Count == 0)
            {
                return false;
            }

            return true;
        }

        public virtual void AdditionalSetup()
        {
        }

        public virtual void BuildTrainingSession()
        {
            CurrentTrainingSession = new TrainingSession();
            CurrentTrainingSession.Variations = new List<Variation>();
            CurrentTrainingSession.FailedVariations = new List<Variation>();
            CurrentTrainingSession.Index = 0;

            BuildVariations(StartingMove, 0);

            CurrentTrainingSession.Variations.Shuffle();
        }

        public Variation GetCurrentVariation()
        {
            return CurrentTrainingSession.CurrentVariation;
        }

        public virtual void MarkFailure()
        {
            CurrentTrainingSession.CurrentVariation.WasPerfect = false;
            CurrentTrainingSession.CurrentVariation.WasPerfectThisIteration = false;
        }

        public abstract void BuildVariations(TrainerMoveInformation trainerMoveInformation, int depth);
        public virtual bool IncrementVariation()
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
                    return false;
                }
            }

            return true;
        }

        public void SetNextVariation()
        {
            CurrentTrainingSession.CurrentVariation = CurrentTrainingSession.Variations[CurrentTrainingSession.Index];
            CurrentTrainingSession.CurrentVariation.WasPerfect = true;
            CurrentTrainingSession.CurrentVariation.CurrentMoveIndex = 0;
        }

        public override string ToString()
        {
            return $"Variation {CurrentTrainingSession.Index + 1} / {CurrentTrainingSession.Variations.Count}";
        }
    }
}
