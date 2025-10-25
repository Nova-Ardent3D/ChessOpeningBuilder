using System;
using UnityEngine;
using static Board.Pieces.Piece;

namespace Board.BoardMarkers.Promotion
{
    public class PromotionModule : BoardFlipper
    {
        public RectTransform Transform;

        public bool IsActive { get; private set; }
        public PromotionOption[] Options;

        Action<PieceTypes> _callBack;
        Action _cancelled;

        public void Spawn(bool isWhite, File file, Rank rank, Action<PieceTypes> callBack, Action cancelled)
        {
            _callBack = callBack;
            _cancelled = cancelled;

            IsActive = true;
            this.gameObject.SetActive(true);

            foreach (var option in Options)
            {
                option.SetColor(isWhite);
            }

            UpdateRotation(true);

            int x = (int)file;
            int y = (int)rank;
            Vector2 p1 = 100 * new Vector2(x, y) - new Vector2(350, 350);

            Transform.localPosition = p1;
        }

        public void QueenPressed()
        {
            OptionPressed(PieceTypes.Queen);
        }

        public void RookPressed()
        {
            OptionPressed(PieceTypes.Rook);
        }

        public void BishopPressed()
        {
            OptionPressed(PieceTypes.Bishop);
        }

        public void KnightPressed()
        {
            OptionPressed(PieceTypes.Knight);
        }

        public void CancelPressed()
        {
            IsActive = false;
            gameObject.SetActive(false);
            _cancelled();
        }

        public void OptionPressed(PieceTypes pieceType)
        {
            IsActive = false;
            gameObject.SetActive(false);
            _callBack(pieceType);
        }
    }
}