using Trainer.Data.Moves;
using UnityEngine;

namespace Trainer.AI.Modes
{
    public class ByVariationMode : Mode
    {
        public override void BuildVariations(TrainerMoveInformation trainerMoveInformation, int depth)
        {
            trainerMoveInformation.TimesCorrect = 0;
            trainerMoveInformation.TimesGuessed = 0;

            trainerMoveInformation.VariationTimesGuessed = 0;
            trainerMoveInformation.VariationTimesCorrect = 0;

            if (CurrentTrainingSession.Variations.Count == TrainerData.Depth)
            {
                return;
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
    }
}