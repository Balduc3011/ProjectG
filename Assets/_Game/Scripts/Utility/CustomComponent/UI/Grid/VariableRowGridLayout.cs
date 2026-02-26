using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Balduu
{
    [ExecuteAlways]
    public class VariableRowGridLayout : LayoutGroup
    {
        [Tooltip("Số column cho từng hàng")] public List<int> rowColumns = new List<int>() { 1, 2, 3 };

        [SerializeField] private float cellHeight = 300f;
        [SerializeField] private Vector2 spacing = new Vector2(25, 25);

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
        }

        public override void CalculateLayoutInputVertical()
        {
        }

        public override void SetLayoutHorizontal()
        {
            Layout();
        }

        public override void SetLayoutVertical()
        {
            Layout();
        }

        private void Layout()
        {
            if (rowColumns == null || rowColumns.Count == 0)
                return;

            float width = rectTransform.rect.width;
            float startY = padding.top;

            int childIndex = 0;

            for (int row = 0; row < rowColumns.Count; row++)
            {
                if (childIndex >= rectChildren.Count)
                    break;

                int columnCount = Mathf.Max(1, rowColumns[row]);

                int remaining = rectChildren.Count - childIndex;
                int actualCount = Mathf.Min(columnCount, remaining);

                float usableWidth = width
                                    - padding.left
                                    - padding.right
                                    - spacing.x * (actualCount - 1);

                float cellWidth = usableWidth / actualCount;

                float x = padding.left;

                for (int col = 0; col < actualCount; col++)
                {
                    RectTransform child = rectChildren[childIndex];

                    SetChildAlongAxis(child, 0, x, cellWidth);
                    SetChildAlongAxis(child, 1, startY, cellHeight);

                    x += cellWidth + spacing.x;
                    childIndex++;
                }

                startY += cellHeight + spacing.y;
            }
        }
    }
}