using UnityEngine;
using MoveTrainer.Move;

namespace MoveTrainer
{
    public class TrainerData
    {
        public enum TrainerType
        {
            DepthFirst,
            BreadthFirst
        }

        public bool IsWhiteTrainer;
        public TrainerType Type;
        public MoveInformation StartingMove;

        public static string Serialize(TrainerData trainerData)
        {
            return JsonUtility.ToJson(trainerData);
        }

        public static TrainerData Deserialize(string json)
        {
            return JsonUtility.FromJson<TrainerData>(json);
        }
    }
}