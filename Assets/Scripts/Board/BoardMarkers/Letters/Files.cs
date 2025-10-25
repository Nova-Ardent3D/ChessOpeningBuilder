using TMPro;
using UnityEngine;

namespace Board.BoardMarkers.Letters
{
    public class Files : BoardFlipper
    {
        [SerializeField] TextMeshProUGUI[] Letters;

        public override void UpdateRotation(bool _isRotated)
        {
            base.UpdateRotation(_isRotated);
            if (_isRotated)
            {
                for (int i = 0; i < Letters.Length; i++)
                {
                    Letters[i].text = ((char)('h' - i)).ToString();
                }
            }
            else
            {
                for (int i = 0; i < Letters.Length; i++)
                {
                    Letters[i].text = ((char)('a' + i)).ToString();
                }
            }
        }
    }
}
