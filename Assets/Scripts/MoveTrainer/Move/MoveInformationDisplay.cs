using UnityEngine;
using Utilities;
using TMPro;
using System;
using UnityEngine.UI;

namespace MoveTrainer.Move
{
    public class MoveInformationDisplay : MonoBehaviour
    {
        public Color BaseColor;
        public Color WhiteColor;
        public Color BlackColor;

        public RawImage Background;
        public TextMeshProUGUI MoveNameText;
        public PercentageBar PercentageBar;

        Action<MoveInformation> _callBack = null;
        MoveInformation _moveInformation;

        public void Init(MoveInformation moveInformation)
        {
            _moveInformation = moveInformation;
            MoveNameText.text = moveInformation.MoveNotation;
            PercentageBar.Percentage = 1f;
        }

        public void SetCallBack(Action<MoveInformation> callBack)
        {
            _callBack = callBack;
        }

        public void OnClick()
        {
            _callBack?.Invoke(_moveInformation);
        }

        public void SetAsWhiteTile()
        {
            Background.color = WhiteColor;
        }

        public void SetAsBlackTile()
        {
            Background.color = BlackColor;
        }
    }
}
