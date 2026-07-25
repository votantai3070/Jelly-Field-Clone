using System;
using System.Collections.Generic;
using UnityEngine;

public class JellyPiece : MonoBehaviour, IPoolable
{
    [Header("References")]
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private JellyAnimation jellyAnimation;
    [SerializeField] private JellyPieceView pieceView;

    [Header("Data")]
    [SerializeField] private List<JellySubCell> subCells = new List<JellySubCell>();

    public Vector2Int CurrentCoord { get; private set; }
    public bool HasCell { get; private set; }
    public IReadOnlyList<JellySubCell> SubCells => subCells;

    public void Setup(List<JellySubCell> newSubCells)
    {
        if (newSubCells == null)
            subCells = new List<JellySubCell>();
        else
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
        CurrentCoord = default;
        HasCell = false;
    }

    public bool RemoveSubCellById(string subCellId)
    {
        if (string.IsNullOrEmpty(subCellId))
            return false;

        for (int i = 0; i < subCells.Count; i++)
        {
            JellySubCell subCell = subCells[i];

            if (subCell == null)
                continue;

            if (subCell.id != subCellId)
                continue;

            subCells.RemoveAt(i);
            RefreshVisual();
            return true;
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

        Dictionary<JellyColor, int> colorCounts = CountColors();
        return FindMostCommonColor(colorCounts);
    }

    public void RefreshVisual()
    {
        CacheReferences();

        if (pieceView != null)
            pieceView.Render(subCells);

        if (jellyAnimation != null)
            jellyAnimation.SetBaseScale(Vector3.one);

        if (boxCollider != null)
            boxCollider.size = Vector2.one;
    }

    public void StartDragJiggle()
    {
        if (jellyAnimation != null)
            jellyAnimation.StartDragJiggle();
    }

    public void UpdateDragJiggle(Vector3 worldDelta)
    {
        if (jellyAnimation != null)
            jellyAnimation.SetDragVelocity(worldDelta);
    }

    public void StopDragJiggle(bool snapToBase = true)
    {
        if (jellyAnimation != null)
            jellyAnimation.StopDragJiggle(snapToBase);
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

    public void OnSpawned()
    {
        ResetStateForPool();
    }

    public void OnDespawned()
    {
        ResetStateForPool();
    }

    private void CacheReferences()
    {
        if (pieceView == null)
            pieceView = GetComponentInChildren<JellyPieceView>();

        if (jellyAnimation == null)
            jellyAnimation = GetComponent<JellyAnimation>();
    }

    private Dictionary<JellyColor, int> CountColors()
    {
        Dictionary<JellyColor, int> colorCounts = new Dictionary<JellyColor, int>();

        for (int i = 0; i < subCells.Count; i++)
        {
            JellySubCell subCell = subCells[i];

            if (subCell == null)
                continue;

            JellyColor color = subCell.color;

            if (!colorCounts.ContainsKey(color))
                colorCounts[color] = 0;

            colorCounts[color]++;
        }

        return colorCounts;
    }

    private JellyColor FindMostCommonColor(Dictionary<JellyColor, int> colorCounts)
    {
        JellyColor result = JellyColor.Red;
        int highestCount = -1;

        foreach (KeyValuePair<JellyColor, int> pair in colorCounts)
        {
            if (pair.Value <= highestCount)
                continue;

            highestCount = pair.Value;
            result = pair.Key;
        }

        return result;
    }

    private void ResetStateForPool()
    {
        ClearCoord();
        ClearSubCells();
        CacheReferences();
        ResetVisualState();
        ResetTransformState();
    }

    private void ClearSubCells()
    {
        if (subCells == null)
            subCells = new List<JellySubCell>();
        else
            subCells.Clear();
    }

    private void ResetVisualState()
    {
        if (pieceView != null)
            pieceView.ClearVisualsForPool();

        if (jellyAnimation != null)
        {
            jellyAnimation.StopAllCoroutines();
            jellyAnimation.SetBaseScale(Vector3.one);
        }

        if (boxCollider != null)
            boxCollider.size = Vector2.one;
    }

    private void ResetTransformState()
    {
        transform.localScale = Vector3.one;
    }
}