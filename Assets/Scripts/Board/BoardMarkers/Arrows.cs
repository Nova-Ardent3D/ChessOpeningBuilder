using Board.MouseClickData;
using System.Collections.Generic;
using UnityEngine;

namespace Board.BoardMarkers
{
    public class Arrows : MonoBehaviour
    {
        public Arrow ArrowPrefab;

        Dictionary<MouseData, Arrow> ArrowLookup = new Dictionary<MouseData, Arrow>();

        public void UpdateArrow(MouseData mouseData)
        {
            if (ArrowLookup.ContainsKey(mouseData))
            {
                Destroy(ArrowLookup[mouseData].gameObject);
                ArrowLookup.Remove(mouseData);
            }
            else
            {
                Vector2 p1 = 100 * new Vector2(mouseData.FromPosition.x, mouseData.FromPosition.y) - new Vector2(350, 350);
                Vector2 p2 = 100 * new Vector2(mouseData.ToPosition.x, mouseData.ToPosition.y) - new Vector2(350, 350);
                Vector2 position = (p1 + p2) / 2;
                float size = Vector2.Distance(p1, p2);
                float angle = Vector2.SignedAngle(Vector2.right, p2 - p1);

                Arrow arrow = Instantiate<Arrow>(ArrowPrefab, this.transform);

                arrow.Size = size;
                arrow.transform.localPosition = position;
                arrow.transform.localRotation = Quaternion.Euler(0, 0, angle);
                
                ArrowLookup[mouseData] = arrow;
            }
        }

        public void ClearAll()
        {
            foreach (var arrow in ArrowLookup.Values)
            {
                Destroy(arrow.gameObject);
            }
            ArrowLookup.Clear();
        }
    }
}
