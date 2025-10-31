using UnityEngine;
using System.Collections.Generic;

namespace MoveTrainer.Move
{
    public class MoveInformation
    {
        public MoveInformation ParentMove;
        public List<MoveInformation> PossibleNextMoves = new List<MoveInformation>();
        public string Fen;
        public string MoveNotation;

        public override string ToString()
        {
            return MoveNotation;
        }
    }
}
