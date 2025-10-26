using UnityEngine;

namespace Board.History
{
    public class BoardHistory
    {
        public string StartingFen;

        public void AddMove(Move move)
        {
            Debug.Log(move);
        }
    }
}
