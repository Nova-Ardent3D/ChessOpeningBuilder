using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

namespace Utilities
{
    public class PercentageBar : MonoBehaviour
    {
        public TextMeshProUGUI PercentageText;
        public RawImage Fill;
        public float BarWidth = 180;
        public Color FullColor;
        public Color EmptyColor;

        [SerializeField] float _percentage = 1f;
        public float Percentage
        {
            get { return _percentage; }
            set
            {
                _percentage = Mathf.Clamp01(value);
                UpdateBar();
            }
        }

        private void Start()
        {
            UpdateBar();
        }


        void UpdateBar()
        {
            PercentageText.text = "Rating: " + (Percentage * 100).ToString("0.00") + "%";
            
            Color color = Color.Lerp(EmptyColor, FullColor, Percentage);
            Fill.color = color;

            if (Fill.transform is RectTransform transform)
            {
                transform.offsetMax = new Vector2(-BarWidth * (1 - _percentage), transform.offsetMax.y);
            }
        }
    }
}
