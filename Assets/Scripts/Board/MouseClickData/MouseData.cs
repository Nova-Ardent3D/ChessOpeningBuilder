using UnityEngine;

namespace Board.MouseClickData
{
    public struct MouseData
    {
        public const int FromPositionShiftX = 1 << 0;
        public const int FromPositionShiftY = 1 << 4;
        public const int ToPositionShiftX = 1 << 8;
        public const int ToPositionShiftY = 1 << 12;
        public const int MouseDownShift = 1 << 20;


        public bool IsMouseDown;
        public Vector2Int FromPosition;
        public Vector2Int ToPosition;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int hash = 
                + (FromPosition.x * FromPositionShiftX)
                + (FromPosition.y * FromPositionShiftY)
                + (ToPosition.x * ToPositionShiftX)
                + (ToPosition.y * ToPositionShiftY)
                + (IsMouseDown ? MouseDownShift : 0);
            return hash;
        }
    }
}