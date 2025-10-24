using UnityEngine;

namespace Board.BoardMarkers
{
    public class Arrow : MonoBehaviour
    {
        public RectTransform Container;
        public RectTransform Base;
        public RectTransform Head;

        [SerializeField] float _size = 100;
        public float Size
        {
            get => _size;
            set
            {
                _size = value;
                UpdateSize();
            }
        }

        private void Awake()
        {
            UpdateSize();
        }

        void UpdateSize()
        {
            Container.sizeDelta = new Vector2(_size, Container.sizeDelta.y);
            Base.sizeDelta = new Vector2(_size - Head.sizeDelta.x * 2, Base.sizeDelta.y);
        }
    }
}
