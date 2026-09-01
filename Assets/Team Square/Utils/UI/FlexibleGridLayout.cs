using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


namespace Utils.UI
{
    public class FlexibleGridLayout : LayoutGroup
    {
        private enum FitType
        {
            Uniform,
            Width,
            Height,
            FixedRows,
            FixedColumns
        }

        [Space(10)]
        [SerializeField] private FitType fitType;

        [Space(10)]
        [SerializeField] private int rows;
        [SerializeField] private int columns;
        [SerializeField] private Vector2 cellSize;
        [SerializeField] private Vector2 spacingTarget;

        [Space(10)]
        [SerializeField] private bool proportionalSpacing;
        [SerializeField] private bool fitX;
        [SerializeField] private bool fitY;
        [SerializeField] private bool square;


        [Header("Auto Size")]
        [Space(10)]
        [SerializeField] private float minSize;
        [SerializeField] private bool autoSize;
        [SerializeField] private bool autoSizeVertical;
        [SerializeField] private bool autoSizeHorizontal;

        [Header("Brick paterne")]
        [SerializeField] private float brickPaterneRatio;
        [SerializeField] private bool paterneVertical;


        public override void CalculateLayoutInputVertical()
        {
            base.CalculateLayoutInputHorizontal();

            if (fitType == FitType.Width || fitType == FitType.Height || fitType == FitType.Uniform)
            {
                fitX = true;
                fitY = true;
                float sqrRt = Mathf.Sqrt(transform.childCount);
                rows = Mathf.CeilToInt(sqrRt);
                columns = Mathf.CeilToInt(sqrRt);
            }

            if (fitType == FitType.Width || fitType == FitType.FixedColumns || fitType == FitType.Uniform)
            {
                rows = Mathf.CeilToInt(transform.childCount / (float)columns);
            }
            if (fitType == FitType.Height || fitType == FitType.FixedRows || fitType == FitType.Uniform)
            {
                columns = Mathf.CeilToInt(transform.childCount / (float)rows);
            }

            float parentWidth = rectTransform.rect.width;
            float parentHeight = rectTransform.rect.height;

            Vector2 spacing;
            if (proportionalSpacing)
            {
                spacing = new Vector2(spacingTarget.x * parentWidth, spacingTarget.y * parentHeight);
            }
            else
            {
                spacing = spacingTarget;
            }

            float cellWidth = (parentWidth / (float)columns) - ((spacing.x / (float)columns) * (columns - 1)) - (padding.left / (float)columns) - (padding.right / (float)columns);
            float cellHeight = (parentHeight / (float)rows) - ((spacing.y / (float)rows) * (rows - 1)) - (padding.top / (float)rows) - (padding.bottom / (float)rows);

            cellSize.x = fitX ? cellWidth : cellSize.x;
            cellSize.y = fitY ? cellHeight : cellSize.y;

            if (square)
            {
                if (fitX)
                {
                    cellSize.y = cellSize.x;
                }
                else if (fitY)
                {
                    cellSize.x = cellSize.y;
                }
            }

            int columnCount = 0;
            int rowCount = 0;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                rowCount = i / columns;
                columnCount = i % columns;

                var item = rectChildren[i];

                var xPos = (cellSize.x * columnCount) + (spacing.x * columnCount) + padding.left;
                var yPos = (cellSize.y * rowCount) + (spacing.y * rowCount) + padding.top;


                if (paterneVertical)
                {
                    yPos += brickPaterneRatio * parentHeight * (columnCount % 2);
                }
                else
                {
                    xPos += brickPaterneRatio * parentWidth * (rowCount % 2);
                }

                SetChildAlongAxis(item, 0, xPos, cellSize.x);
                SetChildAlongAxis(item, 1, yPos, cellSize.y);
            }


            float newSize;
            if (autoSizeVertical)
            {
                newSize = (cellSize.y * (rowCount + 1)) + (spacing.y * rowCount) + padding.top + padding.bottom;

                if (autoSize)
                {
                    minSize = rectTransform.rect.height;
                }

                if (newSize < minSize)
                {
                    rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)1, minSize);
                }
                else
                {
                    rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)1, newSize);
                }
            }
            else if (autoSizeHorizontal)
            {
                newSize = (cellSize.x * (columnCount + 1)) + (spacing.x * columnCount) + padding.left + padding.right;

                if (autoSize)
                {
                    minSize = rectTransform.rect.width;
                }

                if (newSize < minSize)
                {
                    rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)0, minSize);
                }
                else
                {
                    rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)0, newSize);
                }
            }
        }

        public override void SetLayoutHorizontal() { }

        public override void SetLayoutVertical() { }
    }
}