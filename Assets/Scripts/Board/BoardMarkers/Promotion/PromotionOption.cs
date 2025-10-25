using UnityEngine;
using UnityEngine.UI;

namespace Board.BoardMarkers.Promotion
{
    public class PromotionOption : BoardFlipper
    {
        public RawImage Image;
        public Texture WhiteQueen;
        public Texture BlackQueen;

        public void SetColor(bool isWhite)
        {
            Image.texture = isWhite ? WhiteQueen : BlackQueen;
        }
    }
}