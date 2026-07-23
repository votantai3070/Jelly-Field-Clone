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

        if (board == null || !board.IsInsideGrid(placedCoord))
            return false;

        CellData placedCell = board.GetCell(placedCoord);
        if (placedCell == null || placedCell.IsPieceEmpty || placedCell.CurrentPiece == null)
            return false;

        JellyPiece placedPiece = placedCell.CurrentPiece;
        Dictionary<string, MatchedSubCellData> unique = new Dictionary<string, MatchedSubCellData>();

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int dir = Directions[i];
            Vector2Int neighborCoord = placedCoord + dir;

            if (!board.IsInsideGrid(neighborCoord))
                continue;

            CellData neighborCell = board.GetCell(neighborCoord);
            if (neighborCell == null || neighborCell.IsPieceEmpty || neighborCell.CurrentPiece == null)
                continue;

            JellyPiece neighborPiece = neighborCell.CurrentPiece;
            if (neighborPiece == placedPiece)
                continue;

            CollectTouchPairsBetweenPieces(
                placedPiece,
                neighborPiece,
                dir,
                VectorToDirection(dir),
                unique
            );
        }

        foreach (var kv in unique)
            matches.Add(kv.Value);

        return matches.Count > 0;
    }

    private void CollectTouchPairsBetweenPieces(
        JellyPiece sourcePiece,
        JellyPiece targetPiece,
        Vector2Int neighborOffset,
        ContactDirection dir,
        Dictionary<string, MatchedSubCellData> unique)
    {
        if (sourcePiece == null || targetPiece == null)
            return;

        Vector2 offset = new Vector2(neighborOffset.x, neighborOffset.y);

        for (int i = 0; i < sourcePiece.SubCells.Count; i++)
        {
            JellySubCell sourceSub = sourcePiece.SubCells[i];
            if (sourceSub == null || !sourceSub.HasValidRuntimeLayout)
                continue;

            for (int j = 0; j < targetPiece.SubCells.Count; j++)
            {
                JellySubCell targetSub = targetPiece.SubCells[j];
                if (targetSub == null || !targetSub.HasValidRuntimeLayout)
                    continue;

                if (sourceSub.color != targetSub.color)
                    continue;

                Rect sourceRect = sourceSub.localRect;
                Rect targetRect = OffsetRect(targetSub.localRect, offset);

                if (!AreRectsTouching(sourceRect, targetRect, dir))
                    continue;

                AddUnique(unique, sourcePiece, sourceSub);
                AddUnique(unique, targetPiece, targetSub);
            }
        }
    }

    private Rect OffsetRect(Rect rect, Vector2 offset)
    {
        return new Rect(rect.x + offset.x, rect.y + offset.y, rect.width, rect.height);
    }

    private bool AreRectsTouching(Rect a, Rect b, ContactDirection dir)
    {
        switch (dir)
        {
            case ContactDirection.Up:
                return Mathf.Abs(a.yMax - b.yMin) <= edgeTolerance &&
                       GetOverlap(a.xMin, a.xMax, b.xMin, b.xMax) > overlapTolerance;

            case ContactDirection.Down:
                return Mathf.Abs(a.yMin - b.yMax) <= edgeTolerance &&
                       GetOverlap(a.xMin, a.xMax, b.xMin, b.xMax) > overlapTolerance;

            case ContactDirection.Right:
                return Mathf.Abs(a.xMax - b.xMin) <= edgeTolerance &&
                       GetOverlap(a.yMin, a.yMax, b.yMin, b.yMax) > overlapTolerance;

            case ContactDirection.Left:
                return Mathf.Abs(a.xMin - b.xMax) <= edgeTolerance &&
                       GetOverlap(a.yMin, a.yMax, b.yMin, b.yMax) > overlapTolerance;
        }

        return false;
    }

    private float GetOverlap(float minA, float maxA, float minB, float maxB)
    {
        float min = Mathf.Max(minA, minB);
        float max = Mathf.Min(maxA, maxB);
        return Mathf.Max(0f, max - min);
    }

    private void AddUnique(
        Dictionary<string, MatchedSubCellData> unique,
        JellyPiece piece,
        JellySubCell subCell)
    {
        if (piece == null || subCell == null || string.IsNullOrEmpty(subCell.id))
            return;

        string key = piece.GetInstanceID() + "_" + subCell.id;

        if (unique.ContainsKey(key))
            return;

        unique.Add(key, new MatchedSubCellData(
            piece,
            subCell.id,
            subCell.color,
            subCell.slot
        ));
    }

    private ContactDirection VectorToDirection(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return ContactDirection.Up;
        if (dir == Vector2Int.right) return ContactDirection.Right;
        if (dir == Vector2Int.down) return ContactDirection.Down;
        return ContactDirection.Left;
    }
}