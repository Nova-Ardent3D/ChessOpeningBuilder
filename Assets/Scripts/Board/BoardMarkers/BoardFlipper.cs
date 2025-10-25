using UnityEngine;
using UnityEngine.UIElements;

namespace Board.BoardMarkers
{
    public class BoardFlipper : MonoBehaviour
    {
        RectTransform _transform;

        protected virtual void Awake()
        {
            if (this.gameObject.transform is RectTransform rectTransform)
            {
                _transform = rectTransform;
            }
            else
            {
                Debug.LogError("This GameObject needs to be a RectTransform for ParentClamp to work.");
            }
        }

        public virtual void UpdateRotation(bool _isRotated)
        {
            _transform.rotation = Quaternion.identity;
        }
    }
}
