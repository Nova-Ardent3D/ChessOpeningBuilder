using UnityEngine;
using Utilities;
using TMPro;
using System;

namespace MoveTrainer.Move
{
    public class MoveInformationDisplay : MonoBehaviour
    {
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
    }
}
