using System;
using System.Collections.Generic;
using UnityEngine;

public class JellyPiece : MonoBehaviour
{
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private JellyAnimation jellyAnimation;
    [SerializeField] private JellyPieceView pieceView;

    [Header("Runtime Data")]
    [SerializeField] private List<JellySubCell> subCells = new List<JellySubCell>();

    public Vector2Int CurrentCoord { get; private set; }
    public bool HasCell { get; private set; }
    public IReadOnlyList<JellySubCell> SubCells => subCells;

    public void Setup(List<JellySubCell> newSubCells)
    {
        subCells = CloneSubCells(newSubCells);
        RefreshVisual();
    }

    public void SetCoord(Vector2Int coord)
    {
        CurrentCoord = coord;
        HasCell = true;
    }

    public void ClearCoord()
    {
        HasCell = false;
    }

    public bool HasColor(JellyColor color)
    {
        for (int i = 0; i < subCells.Count; i++)
        {
            if (subCells[i].color == color)
                return true;
        }

        return false;
    }

    public int CountColor(JellyColor color)
    {
        int count = 0;

        for (int i = 0; i < subCells.Count; i++)
        {
            if (subCells[i].color == color)
                count++;
        }

        return count;
    }

    public int RemoveColor(JellyColor color)
    {
        int removed = 0;

        for (int i = subCells.Count - 1; i >= 0; i--)
        {
            if (subCells[i].color == color)
            {
                subCells.RemoveAt(i);
                removed++;
            }
        }

        if (removed > 0)
            RefreshVisual();

        return removed;
    }

    public bool IsEmptyCompletely()
    {
        return subCells == null || subCells.Count == 0;
    }

    public JellyColor GetPrimaryColor()
    {
        if (subCells == null || subCells.Count == 0)
            return JellyColor.Red;

        Dictionary<JellyColor, int> counts = new Dictionary<JellyColor, int>();

        for (int i = 0; i < subCells.Count; i++)
        {
            JellyColor color = subCells[i].color;
            if (!counts.ContainsKey(color))
                counts[color] = 0;

            counts[color]++;
        }

        JellyColor result = subCells[0].color;
        int best = -1;

        foreach (var pair in counts)
        {
            if (pair.Value > best)
            {
                best = pair.Value;
                result = pair.Key;
            }
        }

        return result;
    }

    public List<JellyColor> GetDistinctColors()
    {
        List<JellyColor> colors = new List<JellyColor>();

        for (int i = 0; i < subCells.Count; i++)
        {
            JellyColor color = subCells[i].color;
            if (!colors.Contains(color))
                colors.Add(color);
        }

        return colors;
    }

    public void RefreshVisual()
    {
        if (pieceView == null)
            pieceView = GetComponentInChildren<JellyPieceView>();

        if (jellyAnimation == null)
            jellyAnimation = GetComponent<JellyAnimation>();

        if (pieceView != null)
            pieceView.Render(subCells);

        if (jellyAnimation != null)
            jellyAnimation.SetBaseScale(Vector3.one);

        if (boxCollider != null)
            boxCollider.size = Vector2.one;
    }

    public void PlayLanding()
    {
        if (jellyAnimation != null)
            jellyAnimation.PlayLanding();
    }

    public void PlayPreCollectPulse()
    {
        if (jellyAnimation != null)
            jellyAnimation.PlayPreCollectPulse();
    }

    public void PlayCollectToPoint(Vector3 target, Action onComplete)
    {
        if (jellyAnimation != null)
            jellyAnimation.PlayCollectToPoint(target, onComplete);
        else
            onComplete?.Invoke();
    }

    private List<JellySubCell> CloneSubCells(List<JellySubCell> source)
    {
        List<JellySubCell> clone = new List<JellySubCell>();

        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            clone.Add(new JellySubCell(source[i].color));
        }

        return clone;
    }
}