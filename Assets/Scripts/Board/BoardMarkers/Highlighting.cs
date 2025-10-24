using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

namespace Board.BoardMarkers
{
    public class Highlighting : MonoBehaviour
    {
        public enum HighlightType
        {
            Off,
            Right,
            Left,
            LastMove,
        }

        public Color DarkRightClickColor;
        public Color LightRightClickColor;

        public Color DarkLeftClickColor;
        public Color LightLeftClickColor;

        public GameObject HighlightSquarePrefab;

        public GameObject[] HighlightSquares;
        public HighlightType[] HighlightTypes;

        bool _lastMoveActive;
        Vector2Int _lastMoveFrom;
        Vector2Int _lastMoveTo;

        private void Awake()
        {
            HighlightSquares = new GameObject[64];
            HighlightTypes = new HighlightType[64];

            int index = 0;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Vector2 p = 100 * new Vector2(x, y) - new Vector2(350, 350);

                    GameObject highlight = Instantiate<GameObject>(HighlightSquarePrefab, this.transform);
                    Image image = highlight.GetComponent<Image>();

                    highlight.transform.localPosition = p;

                    if (x == y)
                    {
                        image.color = DarkRightClickColor;
                    }
                    else
                    {
                        image.color = LightRightClickColor;
                    }

                    HighlightSquares[index] = highlight;
                    HighlightTypes[index] = HighlightType.Off;
                    index++;
                }
            }
        }

        public void Highlight(int x, int y, bool isRightClick)
        {
            if (x < 0 || y < 0 || x > 7 || y > 7)
            {
                Debug.LogError($"invalid index of {x} {y} for highlight");
                return;
            }

            int index = x * 8 + y;
            
            if (HighlightTypes[index] == HighlightType.LastMove && isRightClick)
            {
                HighlightTypes[index] = HighlightType.Right;
            }
            else if (!HighlightSquares[index].activeSelf)
            {
                HighlightTypes[index] = isRightClick ? HighlightType.Right : HighlightType.Left;
                HighlightSquares[index].SetActive(true);
            }
            else
            {
                HighlightTypes[index] = HighlightType.Off;
                HighlightSquares[index].SetActive(false);
            }


            Image image = HighlightSquares[index].GetComponent<Image>();
            if (isRightClick)
            {
                if (x % 2 == y % 2)
                {
                    image.color = DarkRightClickColor;
                }
                else
                {
                    image.color = LightRightClickColor;
                }
            }
            else
            {
                if (x % 2 == y % 2)
                {
                    image.color = DarkLeftClickColor;
                }
                else
                {
                    image.color = LightLeftClickColor;
                }
            }

            UpdateLastMove();
        }

        public void ClearAll(HighlightType clickType = HighlightType.Off)
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int index = x * 8 + y;
                    if (HighlightTypes[index] == clickType || clickType == HighlightType.Off)
                    {
                        HighlightSquares[index].SetActive(false);
                        HighlightTypes[index] = HighlightType.Off;
                    }
                }
            }

            UpdateLastMove();
        }

        public void SetLastMove(Vector2Int from, Vector2Int to)
        {
            _lastMoveActive = true;
            _lastMoveFrom = from;
            _lastMoveTo = to;

            UpdateLastMove();
        }

        void UpdateLastMove()
        {
            if (!_lastMoveActive)
            {
                return;
            }

            UpdateLastMoveSquare(_lastMoveFrom);
            UpdateLastMoveSquare(_lastMoveTo);
        }

        void UpdateLastMoveSquare(Vector2Int pos)
        {
            int index = pos.x * 8 + pos.y;
            if (HighlightTypes[index] != HighlightType.Right)
            {
                HighlightSquares[index].SetActive(true);
                HighlightTypes[index] = HighlightType.LastMove;

                Image image = HighlightSquares[index].GetComponent<Image>();
                if (pos.x % 2 == pos.y %2)
                {
                    image.color = DarkLeftClickColor;
                }
                else
                {
                    image.color = LightLeftClickColor;
                }
            }
        }
    }
}
