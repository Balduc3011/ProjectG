using UnityEngine;
using UnityEngine.UI;

namespace Balduu
{
    public class CenteredGridLayoutGroup : GridLayoutGroup
    {
        public override void SetLayoutHorizontal()
        {
            base.SetLayoutHorizontal();
        }

        public override void SetLayoutVertical()
        {
            base.SetLayoutVertical();

            if (constraint != Constraint.FixedColumnCount)
                return;

            int childCount = rectChildren.Count;
            int columns = constraintCount;

            if (columns <= 0 || childCount == 0)
                return;

            int rows = Mathf.CeilToInt((float)childCount / columns);
            int lastRowCount = childCount % columns;

            if (lastRowCount == 0)
                return;

            int lastRowStartIndex = (rows - 1) * columns;

            float requiredWidth = lastRowCount * cellSize.x +
                                  (lastRowCount - 1) * spacing.x;

            float startX = GetStartOffset(0, requiredWidth);

            float startY = GetStartOffset(
                1,
                rows * cellSize.y + (rows - 1) * spacing.y
            );

            float yPos = startY + (cellSize.y + spacing.y) * (rows - 1);

            for (int i = 0; i < lastRowCount; i++)
            {
                int childIndex = lastRowStartIndex + i;

                float xPos = startX + (cellSize.x + spacing.x) * i;

                SetChildAlongAxis(rectChildren[childIndex], 0, xPos, cellSize.x);
                SetChildAlongAxis(rectChildren[childIndex], 1, yPos, cellSize.y);
            }
        }
    }
}