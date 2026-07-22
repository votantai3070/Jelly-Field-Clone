using System.Collections.Generic;
using UnityEngine;

public class JellyPieceView : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SpriteRenderer subCellPrefab;
    [SerializeField] private float padding = 0.08f;
    [SerializeField] private float spacing = 0.04f;
    [SerializeField] private int sortingOrder = 1;

    private readonly List<SpriteRenderer> spawnedCells = new List<SpriteRenderer>();

    public void Render(IReadOnlyList<JellySubCell> cells)
    {
        ClearVisuals();

        if (cells == null || cells.Count == 0 || contentRoot == null || subCellPrefab == null)
            return;

        List<Rect> rects = BuildLayout(cells.Count);

        for (int i = 0; i < cells.Count; i++)
        {
            SpriteRenderer cell = Instantiate(subCellPrefab, contentRoot);
            cell.gameObject.name = "SubCell_" + i;
            cell.color = ToUnityColor(cells[i].color);
            cell.sortingOrder = sortingOrder;

            Rect rect = rects[i];
            cell.transform.localPosition = new Vector3(rect.center.x, rect.center.y, 0f);
            cell.transform.localScale = new Vector3(rect.width, rect.height, 1f);

            spawnedCells.Add(cell);
        }
    }

    private List<Rect> BuildLayout(int count)
    {
        List<Rect> result = new List<Rect>();

        float min = -0.5f + padding;
        float max = 0.5f - padding;
        float full = max - min;
        float half = (full - spacing) * 0.5f;

        if (count <= 0)
            return result;

        if (count == 1)
        {
            result.Add(RectFromMinSize(min, min, full, full));
        }
        else if (count == 2)
        {
            result.Add(RectFromMinSize(min, min, half, full));
            result.Add(RectFromMinSize(min + half + spacing, min, half, full));
        }
        else if (count == 3)
        {
            result.Add(RectFromMinSize(min, min + half + spacing, full, half));
            result.Add(RectFromMinSize(min, min, half, half));
            result.Add(RectFromMinSize(min + half + spacing, min, half, half));
        }
        else
        {
            result.Add(RectFromMinSize(min, min, half, half));
            result.Add(RectFromMinSize(min + half + spacing, min, half, half));
            result.Add(RectFromMinSize(min, min + half + spacing, half, half));
            result.Add(RectFromMinSize(min + half + spacing, min + half + spacing, half, half));
        }

        return result;
    }

    private Rect RectFromMinSize(float x, float y, float w, float h)
    {
        return new Rect(x, y, w, h);
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