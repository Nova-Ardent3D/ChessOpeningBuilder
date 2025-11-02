using SimpleFileBrowser;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Board.History
{
    public class MoveLabel : MonoBehaviour
    {
        [SerializeField] Color failedColor;
        [SerializeField] RawImage backGround;

        Action _callback;
        public TextMeshProUGUI label;

        public void SetCallback(Action callback)
        {
            _callback = callback;
        }

        public void SetMove(Move move)
        {
            label.text = move.ToString();
            this.name = move.ToString();
        }

        public void OnClick()
        {
            if (FileBrowser.IsOpen)
                return;

            _callback();
        }

        public void SetColorToFailed()
        {
            backGround.color = failedColor;
        }
    }
}
