using System.Collections.Generic;
using UnityEngine;

public class MergeSystem : MonoBehaviour
{
    [SerializeField] private BoardManager board;
    [SerializeField] private float edgeTolerance = 0.001f;
    [SerializeField] private float overlapTolerance = 0.001f;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public bool TryGetTouchMatchesForPlacedPiece(Vector2Int placedCoord, out List<MatchedSubCellData> matches)
    {
        matches = new List<MatchedSubCellData>();

        if (!CanCheckPlacedCoord(placedCoord))
            return false;

        JellyPiece placedPiece = board.GetCell(placedCoord).CurrentPiece;
        Dictionary<string, MatchedSubCellData> uniqueMatches = new Dictionary<string, MatchedSubCellData>();

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int direction = Directions[i];
            TryCollectMatchesFromNeighbor(placedPiece, placedCoord, direction, uniqueMatches);
        }

        foreach (KeyValuePair<string, MatchedSubCellData> pair in uniqueMatches)
        {
            matches.Add(pair.Value);
        }

        return matches.Count > 0;
    }

    private bool CanCheckPlacedCoord(Vector2Int placedCoord)
    {
        if (board == null)
            return false;

        if (!board.IsInsideGrid(placedCoord))
            return false;

        CellData placedCell = board.GetCell(placedCoord);
        if (placedCell == null)
            return false;

        if (placedCell.IsPieceEmpty)
            return false;

        if (placedCell.CurrentPiece == null)
            return false;

        return true;
    }

    private void TryCollectMatchesFromNeighbor(
        JellyPiece placedPiece,
        Vector2Int placedCoord,
        Vector2Int direction,
        Dictionary<string, MatchedSubCellData> uniqueMatches)
    {
        Vector2Int neighborCoord = placedCoord + direction;

        if (!board.IsInsideGrid(neighborCoord))
            return;

        CellData neighborCell = board.GetCell(neighborCoord);
        if (neighborCell == null || neighborCell.IsPieceEmpty || neighborCell.CurrentPiece == null)
            return;

        JellyPiece neighborPiece = neighborCell.CurrentPiece;
        if (neighborPiece == placedPiece)
            return;

        ContactDirection contactDirection = VectorToDirection(direction);

        CollectTouchPairsBetweenPieces(
            placedPiece,
            neighborPiece,
            direction,
            contactDirection,
            uniqueMatches
        );
    }

    private void CollectTouchPairsBetweenPieces(
        JellyPiece sourcePiece,
        JellyPiece targetPiece,
        Vector2Int neighborOffset,
        ContactDirection contactDirection,
        Dictionary<string, MatchedSubCellData> uniqueMatches)
    {
        if (sourcePiece == null || targetPiece == null)
            return;

        Vector2 worldOffset = new Vector2(neighborOffset.x, neighborOffset.y);

        for (int i = 0; i < sourcePiece.SubCells.Count; i++)
        {
            JellySubCell sourceSubCell = sourcePiece.SubCells[i];
            if (!IsValidSubCell(sourceSubCell))
                continue;

            for (int j = 0; j < targetPiece.SubCells.Count; j++)
            {
                JellySubCell targetSubCell = targetPiece.SubCells[j];
                if (!IsValidSubCell(targetSubCell))
                    continue;

                if (!DoSubCellsHaveSameColor(sourceSubCell, targetSubCell))
                    continue;

                if (!AreSubCellsTouching(sourceSubCell, targetSubCell, worldOffset, contactDirection))
                    continue;

                AddUniqueMatch(uniqueMatches, sourcePiece, sourceSubCell);
                AddUniqueMatch(uniqueMatches, targetPiece, targetSubCell);
            }
        }
    }

    private bool IsValidSubCell(JellySubCell subCell)
    {
        if (subCell == null)
            return false;

        if (!subCell.HasValidRuntimeLayout)
            return false;

        return true;
    }

    private bool DoSubCellsHaveSameColor(JellySubCell a, JellySubCell b)
    {
        return a.color == b.color;
    }

    private bool AreSubCellsTouching(
        JellySubCell sourceSubCell,
        JellySubCell targetSubCell,
        Vector2 offset,
        ContactDirection contactDirection)
    {
        Rect sourceRect = sourceSubCell.localRect;
        Rect targetRect = OffsetRect(targetSubCell.localRect, offset);

        return AreRectsTouching(sourceRect, targetRect, contactDirection);
    }

    private Rect OffsetRect(Rect rect, Vector2 offset)
    {
        return new Rect(
            rect.x + offset.x,
            rect.y + offset.y,
            rect.width,
            rect.height
        );
    }

    private bool AreRectsTouching(Rect a, Rect b, ContactDirection contactDirection)
    {
        switch (contactDirection)
        {
            case ContactDirection.Up:
                return IsTopTouching(a, b);

            case ContactDirection.Down:
                return IsBottomTouching(a, b);

            case ContactDirection.Right:
                return IsRightTouching(a, b);

            case ContactDirection.Left:
                return IsLeftTouching(a, b);

            default:
                return false;
        }
    }

    private bool IsTopTouching(Rect a, Rect b)
    {
        bool edgeMatches = Mathf.Abs(a.yMax - b.yMin) <= edgeTolerance;
        bool hasOverlap = GetOverlap(a.xMin, a.xMax, b.xMin, b.xMax) > overlapTolerance;
        return edgeMatches && hasOverlap;
    }

    private bool IsBottomTouching(Rect a, Rect b)
    {
        bool edgeMatches = Mathf.Abs(a.yMin - b.yMax) <= edgeTolerance;
        bool hasOverlap = GetOverlap(a.xMin, a.xMax, b.xMin, b.xMax) > overlapTolerance;
        return edgeMatches && hasOverlap;
    }

    private bool IsRightTouching(Rect a, Rect b)
    {
        bool edgeMatches = Mathf.Abs(a.xMax - b.xMin) <= edgeTolerance;
        bool hasOverlap = GetOverlap(a.yMin, a.yMax, b.yMin, b.yMax) > overlapTolerance;
        return edgeMatches && hasOverlap;
    }

    private bool IsLeftTouching(Rect a, Rect b)
    {
        bool edgeMatches = Mathf.Abs(a.xMin - b.xMax) <= edgeTolerance;
        bool hasOverlap = GetOverlap(a.yMin, a.yMax, b.yMin, b.yMax) > overlapTolerance;
        return edgeMatches && hasOverlap;
    }

    private float GetOverlap(float minA, float maxA, float minB, float maxB)
    {
        float overlapMin = Mathf.Max(minA, minB);
        float overlapMax = Mathf.Min(maxA, maxB);
        return Mathf.Max(0f, overlapMax - overlapMin);
    }

    private void AddUniqueMatch(
        Dictionary<string, MatchedSubCellData> uniqueMatches,
        JellyPiece piece,
        JellySubCell subCell)
    {
        if (piece == null || subCell == null)
            return;

        if (string.IsNullOrEmpty(subCell.id))
            return;

        string key = BuildMatchKey(piece, subCell.id);

        if (uniqueMatches.ContainsKey(key))
            return;

        MatchedSubCellData matchData = new MatchedSubCellData(
            piece,
            subCell.id,
            subCell.color,
            subCell.slot
        );

        uniqueMatches.Add(key, matchData);
    }

    private string BuildMatchKey(JellyPiece piece, string subCellId)
    {
        return piece.GetInstanceID() + "_" + subCellId;
    }

    private ContactDirection VectorToDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
            return ContactDirection.Up;

        if (direction == Vector2Int.right)
            return ContactDirection.Right;

        if (direction == Vector2Int.down)
            return ContactDirection.Down;

        return ContactDirection.Left;
    }
}