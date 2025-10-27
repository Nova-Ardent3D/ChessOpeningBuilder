using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Utilities
{
    public class ExpanderBar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform _this;
        [SerializeField] RectTransform _container;

        [SerializeField] RectTransform _left;
        [SerializeField] RectTransform _right;

        bool _isMouseDown = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            _isMouseDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isMouseDown = false;
        }

        void Awake()
        {
            _left.offsetMax = new Vector2(_this.position.x - _container.rect.width, _left.offsetMax.y);
            _right.offsetMin = new Vector2(_this.position.x, _right.offsetMin.y);
        }

        void Update()
        {
            if (_isMouseDown)
            {
                var position = Input.mousePosition;
                var containerSize = _container.rect.width + _left.offsetMax.x;
                if (_left != null)
                {
                    _left.offsetMax = new Vector2(position.x - _container.rect.width, _left.offsetMax.y);
                }

                if (_right != null)
                {
                    _right.offsetMin = new Vector2(position.x, _right.offsetMin.y);
                }
                _this.position = new Vector3(position.x, _this.position.y, _this.position.z);
            }
        }
    }
}

