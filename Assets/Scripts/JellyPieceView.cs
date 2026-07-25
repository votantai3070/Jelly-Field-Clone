using System.Collections.Generic;
using UnityEngine;

public class JellyPieceView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SpriteRenderer subCellPrefab;

    [Header("Layout")]
    [SerializeField] private float padding = 0.04f;
    [SerializeField] private float spacing = 0.02f;

    [Header("Render")]
    [SerializeField] private int sortingOrder = 1;

    private readonly List<SpriteRenderer> spawnedCells = new List<SpriteRenderer>();

    public void Render(IReadOnlyList<JellySubCell> cells)
    {
        ClearVisuals();

        if (!CanRender(cells))
            return;

        ResetCellLayoutData(cells);

        List<LayoutSlotData> layoutSlots = BuildLayout(cells.Count);
        CreateSubCellVisuals(cells, layoutSlots);
    }

    public void ClearVisualsForPool()
    {
        ClearVisuals();
    }

    private bool CanRender(IReadOnlyList<JellySubCell> cells)
    {
        if (cells == null || cells.Count == 0)
            return false;

        if (contentRoot == null)
            return false;

        if (subCellPrefab == null)
            return false;

        return true;
    }

    private void ResetCellLayoutData(IReadOnlyList<JellySubCell> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] != null)
                cells[i].ClearRuntimeLayout();
        }
    }

    private void CreateSubCellVisuals(IReadOnlyList<JellySubCell> cells, List<LayoutSlotData> layoutSlots)
    {
        int count = Mathf.Min(cells.Count, layoutSlots.Count);

        for (int i = 0; i < count; i++)
        {
            JellySubCell data = cells[i];
            if (data == null)
                continue;

            LayoutSlotData slotData = layoutSlots[i];

            data.SetRuntimeLayout(slotData.slot, slotData.rect);

            SpriteRenderer subCellRenderer = Instantiate(subCellPrefab, contentRoot);
            SetupSubCellRenderer(subCellRenderer, data, slotData, i);

            spawnedCells.Add(subCellRenderer);
        }
    }

    private void SetupSubCellRenderer(SpriteRenderer subCellRenderer, JellySubCell cellData, LayoutSlotData slotData, int index)
    {
        subCellRenderer.gameObject.name = "SubCell_" + slotData.slot + "_" + index;
        subCellRenderer.color = GetColor(cellData.color);
        subCellRenderer.sortingOrder = sortingOrder;

        Vector2 center = slotData.rect.center;
        subCellRenderer.transform.localPosition = new Vector3(center.x, center.y, 0f);
        subCellRenderer.transform.localScale = new Vector3(slotData.rect.width, slotData.rect.height, 1f);
    }

    private List<LayoutSlotData> BuildLayout(int cellCount)
    {
        List<LayoutSlotData> result = new List<LayoutSlotData>();

        float minX = -0.5f + padding;
        float maxX = 0.5f - padding;
        float minY = -0.5f + padding;
        float maxY = 0.5f - padding;

        float fullWidth = maxX - minX;
        float fullHeight = maxY - minY;

        if (cellCount <= 0)
            return result;

        if (cellCount == 1)
        {
            AddLayoutForOneCell(result, minX, minY, fullWidth, fullHeight);
        }
        else if (cellCount == 2)
        {
            AddLayoutForTwoCells(result, minX, minY, fullWidth, fullHeight);
        }
        else if (cellCount == 3)
        {
            AddLayoutForThreeCells(result, minX, minY, fullWidth, fullHeight);
        }
        else
        {
            AddLayoutForFourCells(result, minX, minY, fullWidth, fullHeight);
        }

        return result;
    }

    private void AddLayoutForOneCell(List<LayoutSlotData> result, float minX, float minY, float fullWidth, float fullHeight)
    {
        Rect rect = new Rect(minX, minY, fullWidth, fullHeight);
        result.Add(new LayoutSlotData(JellySlot.Full, rect));
    }

    private void AddLayoutForTwoCells(List<LayoutSlotData> result, float minX, float minY, float fullWidth, float fullHeight)
    {
        float cellWidth = (fullWidth - spacing) * 0.5f;

        Rect leftRect = new Rect(minX, minY, cellWidth, fullHeight);
        Rect rightRect = new Rect(minX + cellWidth + spacing, minY, cellWidth, fullHeight);

        result.Add(new LayoutSlotData(JellySlot.Left, leftRect));
        result.Add(new LayoutSlotData(JellySlot.Right, rightRect));
    }

    private void AddLayoutForThreeCells(List<LayoutSlotData> result, float minX, float minY, float fullWidth, float fullHeight)
    {
        float topHeight = (fullHeight - spacing) * 0.52f;
        float bottomHeight = fullHeight - spacing - topHeight;
        float bottomWidth = (fullWidth - spacing) * 0.5f;

        Rect topRect = new Rect(minX, minY + bottomHeight + spacing, fullWidth, topHeight);
        Rect bottomLeftRect = new Rect(minX, minY, bottomWidth, bottomHeight);
        Rect bottomRightRect = new Rect(minX + bottomWidth + spacing, minY, bottomWidth, bottomHeight);

        result.Add(new LayoutSlotData(JellySlot.Top, topRect));
        result.Add(new LayoutSlotData(JellySlot.BottomLeft, bottomLeftRect));
        result.Add(new LayoutSlotData(JellySlot.BottomRight, bottomRightRect));
    }

    private void AddLayoutForFourCells(List<LayoutSlotData> result, float minX, float minY, float fullWidth, float fullHeight)
    {
        float cellWidth = (fullWidth - spacing) * 0.5f;
        float cellHeight = (fullHeight - spacing) * 0.5f;

        Rect topLeftRect = new Rect(minX, minY + cellHeight + spacing, cellWidth, cellHeight);
        Rect topRightRect = new Rect(minX + cellWidth + spacing, minY + cellHeight + spacing, cellWidth, cellHeight);
        Rect bottomLeftRect = new Rect(minX, minY, cellWidth, cellHeight);
        Rect bottomRightRect = new Rect(minX + cellWidth + spacing, minY, cellWidth, cellHeight);

        result.Add(new LayoutSlotData(JellySlot.TopLeft, topLeftRect));
        result.Add(new LayoutSlotData(JellySlot.TopRight, topRightRect));
        result.Add(new LayoutSlotData(JellySlot.BottomLeft, bottomLeftRect));
        result.Add(new LayoutSlotData(JellySlot.BottomRight, bottomRightRect));
    }

    private void ClearVisuals()
    {
        for (int i = 0; i < spawnedCells.Count; i++)
        {
            if (spawnedCells[i] != null)
                Destroy(spawnedCells[i].gameObject);
        }

        spawnedCells.Clear();
    }

    private Color GetColor(JellyColor color)
    {
        switch (color)
        {
            case JellyColor.Red:
                return new Color(1f, 0.35f, 0.35f);

            case JellyColor.Yellow:
                return new Color(1f, 0.87f, 0.25f);

            case JellyColor.Blue:
                return new Color(0.3f, 0.55f, 1f);

            case JellyColor.Green:
                return new Color(0.35f, 0.9f, 0.45f);

            default:
                return Color.white;
        }
    }
}