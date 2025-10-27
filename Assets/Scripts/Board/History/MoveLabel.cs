using UnityEngine;
using TMPro;

namespace Board.History
{
    public class MoveLabel : MonoBehaviour
    {
        public TextMeshProUGUI label;

        public void SetMove(Move move)
        {
            label.text = move.ToString();
        }
    }
}
