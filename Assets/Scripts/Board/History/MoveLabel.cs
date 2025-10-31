using SimpleFileBrowser;
using System;
using TMPro;
using UnityEngine;

namespace Board.History
{
    public class MoveLabel : MonoBehaviour
    {
        Action _callback;
        public TextMeshProUGUI label;

        public void SetCallback(Action callback)
        {
            _callback = callback;
        }

        public void SetMove(Move move)
        {
            label.text = move.ToString();
        }

        public void OnClick()
        {
            if (FileBrowser.IsOpen)
                return;

            _callback();
        }
    }
}
