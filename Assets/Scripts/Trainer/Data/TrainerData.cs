using Board.Moves;
using System.Collections.Generic;
using UnityEngine;
using Trainer.Data.Moves;
using Board.Common;

namespace Trainer.Data
{
    public class TrainerData
    {
        public const string StartingMoveNotation = "Start Position";

        public enum TrainerType
        {
            ByCompleteVariation,
            ByMoveCount,
            MarathonMode,
        }

        public enum StatsView
        {
            ByMove,
            ByBranch,
            ByVariation,
        }

        public enum TrainingMethod
        {
            RunOnce,
            ReplayFailedMoves,
            RepeatVariationOnFailed,
        }

        public const int Version = 3;

        public PieceColor Color = PieceColor.White;
        public int Depth = -1;
        public TrainerType DepthType = TrainerType.ByCompleteVariation;
        public StatsView StatsDisplay = StatsView.ByMove;
        public TrainingMethod Method = TrainingMethod.RunOnce;
        public TrainerMoveInformation StartingMove;

        public TrainerData()
        {
            StartingMove = new TrainerMoveInformation();
            StartingMove.Color = PieceColor.Black;
            StartingMove.MoveNotation = StartingMoveNotation;
        }

        public static IEnumerable<string> Serialize(TrainerData trainerData)
        {
            yield return TrainerData.Version.ToString();
            yield return (trainerData.Color == PieceColor.White ? "W" : "B");
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
            trainerData.Color = contents.Current == "W" ? PieceColor.White : PieceColor.Black;

            contents.MoveNext();
            Debug.Log(contents.Current);
            trainerData.Depth = int.Parse(contents.Current.Trim());

            contents.MoveNext();
            Debug.Log(contents.Current);
            if (!System.Enum.TryParse<TrainerType>(contents.Current.Trim(), out trainerData.DepthType))
            {
                Debug.LogWarning("Failed to read trainer type, defaulting value.");
            }

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

            trainerData.StartingMove = new TrainerMoveInformation();
            trainerData.StartingMove.Deserialize(contents);

            SetMoveColors(trainerData.StartingMove);

            return trainerData;
        }

        public static void SetMoveColors(TrainerMoveInformation move)
        {
            if (move.ParentMove == null)
            {
                move.Color = PieceColor.Black;
            }
            else
            {
                move.Color = move.ParentMove.Color == PieceColor.White ? PieceColor.Black : PieceColor.White;
            }

            foreach (var child in move.PossibleNextMoves)
            {
                SetMoveColors(child);
            }
        }
    }
}
