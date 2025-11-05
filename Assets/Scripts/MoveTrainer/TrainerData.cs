using UnityEngine;
using MoveTrainer.Move;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace MoveTrainer
{
    public class TrainerData
    {
        public enum TrainerType
        {
            DepthFirst,
            BreadthFirst
        }

        public enum StatsView
        {
            ByMove,
            ByBranch
        }

        public enum TrainingMethod
        {
            RunOnce,
            ReplayFailedMoves,
            RepeatVariationOnFailed,
        }

        public const int Version = 3;

        public bool IsWhiteTrainer = true;
        public int Depth = -1;
        public TrainerType DepthType = TrainerType.DepthFirst;
        public StatsView StatsDisplay = StatsView.ByMove;
        public TrainingMethod Method = TrainingMethod.RunOnce;
        public MoveInformation StartingMove;

        public IEnumerable<string> Serialize(TrainerData trainerData)
        {
            yield return TrainerData.Version.ToString();
            yield return (trainerData.IsWhiteTrainer ? "W" : "B");
            yield return trainerData.Depth.ToString();
            yield return trainerData.DepthType.ToString();
            yield return trainerData.StatsDisplay.ToString();
            yield return trainerData.Method.ToString();

            foreach (var line in trainerData.StartingMove.Serialize(0))
            {
                yield return line;
            } 
        }

        public static TrainerData Deserialize(IEnumerator<string> contents)
        {
            TrainerData trainerData = new TrainerData();

            contents.MoveNext();
            Debug.Log(contents.Current);
            int version = int.Parse(contents.Current.Trim());

            contents.MoveNext();
            Debug.Log(contents.Current);
            trainerData.IsWhiteTrainer = contents.Current == "W";

            contents.MoveNext();
            Debug.Log(contents.Current);
            trainerData.Depth = int.Parse(contents.Current.Trim());

            contents.MoveNext();
            Debug.Log(contents.Current);
            trainerData.DepthType = (TrainerType)System.Enum.Parse(typeof(TrainerType), contents.Current.Trim());

            if (version <= 1)
            {
                trainerData.StatsDisplay = StatsView.ByMove;
            }
            else
            {
                contents.MoveNext();
                Debug.Log(contents.Current);
                trainerData.StatsDisplay = (StatsView)System.Enum.Parse(typeof(StatsView), contents.Current.Trim());
            }

            if (version <= 2)
            {
                trainerData.Method = TrainingMethod.RunOnce;
            }
            else
            {
                contents.MoveNext();
                Debug.Log(contents.Current);
                trainerData.Method = (TrainingMethod)System.Enum.Parse(typeof(TrainingMethod), contents.Current.Trim());
            }

            trainerData.StartingMove = new MoveInformation();
            trainerData.StartingMove.Deserialize(contents);

            SetMoveColors(trainerData.StartingMove);

            return trainerData;
        }

        public static void SetMoveColors(MoveInformation move)
        {
            if (move.ParentMove == null)
            {
                move.IsWhite = false;
            }
            else
            {
                move.IsWhite = !move.ParentMove.IsWhite;
            }

            foreach (var child in move.PossibleNextMoves)
            {
                SetMoveColors(child);
            }
        }
    }
}