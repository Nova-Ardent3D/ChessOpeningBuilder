using TMPro;
using UnityEngine;

namespace Board.History
{

    public class MoveIndexLabel : MonoBehaviour
    {
        public TextMeshProUGUI label;

        public void SetMove(int index)
        {
            label.text = index.ToString() + ".";
        }
    }
}
