using System;
using System.Collections.Generic;
using UnityEngine;

public class JellyPiece : MonoBehaviour
{
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private JellyAnimation jellyAnimation;
    [SerializeField] private JellyPieceView pieceView;
    [SerializeField] private List<JellySubCell> subCells = new List<JellySubCell>();

    public Vector2Int CurrentCoord { get; private set; }
    public bool HasCell { get; private set; }
    public IReadOnlyList<JellySubCell> SubCells => subCells;

    public void Setup(List<JellySubCell> newSubCells)
    {
        subCells = newSubCells;
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

    public bool RemoveSubCellById(string subCellId)
    {
        if (string.IsNullOrEmpty(subCellId))
            return false;

        for (int i = 0; i < subCells.Count; i++)
        {
            if (subCells[i] != null && subCells[i].id == subCellId)
            {
                subCells.RemoveAt(i);
                RefreshVisual();
                return true;
            }
        }

        return false;
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
            if (subCells[i] == null)
                continue;

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

    //private List<JellySubCell> CloneSubCells(List<JellySubCell> source)
    //{
    //    List<JellySubCell> clone = new List<JellySubCell>();

    //    if (source == null)
    //        return clone;

    //    for (int i = 0; i < source.Count; i++)
    //    {
    //        if (source[i] == null)
    //            continue;

    //        string newId = string.IsNullOrEmpty(source[i].id)
    //            ? Guid.NewGuid().ToString()
    //            : source[i].id;

    //        clone.Add(new JellySubCell(newId, source[i].color));
    //    }

    //    return clone;
    //}
}