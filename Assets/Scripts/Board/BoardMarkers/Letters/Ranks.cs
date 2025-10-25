using TMPro;
using UnityEngine;

namespace Board.BoardMarkers.Letters
{
    public class Ranks : BoardFlipper
    {
        [SerializeField] TextMeshProUGUI[] Numbers;

        public override void UpdateRotation(bool _isRotated)
        {
            base.UpdateRotation(_isRotated);
            if (_isRotated)
            {
                for (int i = 0; i < Numbers.Length; i++)
                {
                    Numbers[i].text = ((char)('8' - i)).ToString();
                }
            }
            else
            {
                for (int i = 0; i < Numbers.Length; i++)
                {
                    Numbers[i].text = ((char)('1' + i)).ToString();
                }
            }
        }
    }
}
