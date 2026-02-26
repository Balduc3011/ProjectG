using UnityEngine;
using UnityEngine.UI;

namespace Balduu
{
    [ExecuteAlways]
    public class AutoWidthFillLastRowGrid : GridLayoutGroup
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCellWidth();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateCellWidth();
        }
#endif

        void UpdateCellWidth()
        {
            if (constraint != Constraint.FixedColumnCount || constraintCount <= 0)
                return;

            float totalWidth = rectTransform.rect.width;

            float usableWidth = totalWidth
                                - padding.left
                                - padding.right
                                - spacing.x * (constraintCount - 1);

            float newWidth = usableWidth / constraintCount;

            cellSize = new Vector2(newWidth, cellSize.y);
        }

        public override void SetLayoutHorizontal()
        {
            UpdateCellWidth();
            base.SetLayoutHorizontal();
        }

        public override void SetLayoutVertical()
        {
            base.SetLayoutVertical();

            if (constraint != Constraint.FixedColumnCount)
                return;

            int childCount = rectChildren.Count;
            int columns = constraintCount;

            int rows = Mathf.CeilToInt((float)childCount / columns);
            int lastRowCount = childCount % columns;

            if (lastRowCount == 0)
                return;

            int lastRowStart = (rows - 1) * columns;

            float totalWidth = rectTransform.rect.width;
            float usableWidth = totalWidth
                                - padding.left
                                - padding.right
                                - spacing.x * (lastRowCount - 1);

            float newWidth = usableWidth / lastRowCount;

            float totalHeight = rows * cellSize.y + (rows - 1) * spacing.y;
            float startY = GetStartOffset(1, totalHeight);
            float yPos = startY + (cellSize.y + spacing.y) * (rows - 1);

            float x = padding.left;

            for (int i = 0; i < lastRowCount; i++)
            {
                int index = lastRowStart + i;

                SetChildAlongAxis(rectChildren[index], 0, x, newWidth);
                SetChildAlongAxis(rectChildren[index], 1, yPos, cellSize.y);

                x += newWidth + spacing.x;
            }
        }
    }
}