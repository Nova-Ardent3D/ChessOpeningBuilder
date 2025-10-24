using UnityEngine;

namespace Utilities
{
    public class BoardClamp : MonoBehaviour
    {
        public float HorizontalPadding = 100;
        public float VerticalPadding = 100;

        RectTransform _parent;
        RectTransform _transform;

        private void Start()
        {
            if (this.gameObject.transform.parent is RectTransform transform)
            {
                _parent = transform;
            }
            else
            {
                Debug.LogError("Parent needs to be a RectTransform for ParentClamp to work.");
            }

            if (this.gameObject.transform is RectTransform rectTransform)
            {
                _transform = rectTransform;
            }
            else
            {
                Debug.LogError("This GameObject needs to be a RectTransform for ParentClamp to work.");
            }
        }

        private void Update()
        {
            float width = (_parent.rect.width - HorizontalPadding);
            float height = (_parent.rect.height - VerticalPadding);

            float size = Mathf.Min(width, height);
            _transform.offsetMin = new Vector2(-size / 2, -size / 2);
            _transform.offsetMax = new Vector2(size / 2, size / 2);
        }
    }
}