using System.Collections.Generic;
using UnityEngine;

public class JellyPieceView : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SpriteRenderer subCellPrefab;
    [SerializeField] private float padding = 0.04f;
    [SerializeField] private float spacing = 0.02f;
    [SerializeField] private int sortingOrder = 1;

    private readonly List<SpriteRenderer> spawnedCells = new List<SpriteRenderer>();

    public void Render(IReadOnlyList<JellySubCell> cells)
    {
        ClearVisuals();

        if (cells == null || cells.Count == 0 || contentRoot == null || subCellPrefab == null)
            return;

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] != null)
                cells[i].ClearRuntimeLayout();
        }

        List<LayoutSlotData> layout = BuildLayout(cells.Count);

        for (int i = 0; i < cells.Count && i < layout.Count; i++)
        {
            if (cells[i] == null)
                continue;

            cells[i].SetRuntimeLayout(layout[i].slot, layout[i].rect);

            SpriteRenderer cell = Instantiate(subCellPrefab, contentRoot);
            cell.gameObject.name = "SubCell_" + layout[i].slot + "_" + i;
            cell.color = ToUnityColor(cells[i].color);
            cell.sortingOrder = sortingOrder;
            cell.transform.localPosition = new Vector3(layout[i].rect.center.x, layout[i].rect.center.y, 0f);
            cell.transform.localScale = new Vector3(layout[i].rect.width, layout[i].rect.height, 1f);

            spawnedCells.Add(cell);
        }
    }

    //  Tính bố cục các phần của jelly dựa trên số lượng subcell
    private List<LayoutSlotData> BuildLayout(int count)
    {
        List<LayoutSlotData> result = new List<LayoutSlotData>();

        float minX = -0.5f + padding;
        float maxX = 0.5f - padding;
        float minY = -0.5f + padding;
        float maxY = 0.5f - padding;

        float fullW = maxX - minX;
        float fullH = maxY - minY;

        if (count <= 0)
            return result;

        if (count == 1)
        {
            result.Add(new LayoutSlotData(JellySlot.Full, new Rect(minX, minY, fullW, fullH)));
        }
        else if (count == 2)
        {
            float w = (fullW - spacing) * 0.5f;
            result.Add(new LayoutSlotData(JellySlot.Left, new Rect(minX, minY, w, fullH)));
            result.Add(new LayoutSlotData(JellySlot.Right, new Rect(minX + w + spacing, minY, w, fullH)));
        }
        else if (count == 3)
        {
            float topH = (fullH - spacing) * 0.52f;
            float bottomH = fullH - spacing - topH;
            float bottomW = (fullW - spacing) * 0.5f;

            result.Add(new LayoutSlotData(JellySlot.Top, new Rect(minX, minY + bottomH + spacing, fullW, topH)));
            result.Add(new LayoutSlotData(JellySlot.BottomLeft, new Rect(minX, minY, bottomW, bottomH)));
            result.Add(new LayoutSlotData(JellySlot.BottomRight, new Rect(minX + bottomW + spacing, minY, bottomW, bottomH)));
        }
        else
        {
            float w = (fullW - spacing) * 0.5f;
            float h = (fullH - spacing) * 0.5f;

            result.Add(new LayoutSlotData(JellySlot.TopLeft, new Rect(minX, minY + h + spacing, w, h)));
            result.Add(new LayoutSlotData(JellySlot.TopRight, new Rect(minX + w + spacing, minY + h + spacing, w, h)));
            result.Add(new LayoutSlotData(JellySlot.BottomLeft, new Rect(minX, minY, w, h)));
            result.Add(new LayoutSlotData(JellySlot.BottomRight, new Rect(minX + w + spacing, minY, w, h)));
        }

        return result;
    }

    private void ClearVisuals()
    {
        for (int i = spawnedCells.Count - 1; i >= 0; i--)
        {
            if (spawnedCells[i] != null)
                Destroy(spawnedCells[i].gameObject);
        }

        spawnedCells.Clear();
    }

    private Color ToUnityColor(JellyColor color)
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
        }

        return Color.white;
    }
}