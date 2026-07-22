using System.Collections.Generic;
using UnityEngine;

public class MergeSystem : MonoBehaviour
{
    [SerializeField] private BoardManager board;

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
        if (placedCell == null || placedCell.IsEmpty || placedCell.CurrentPiece == null)
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
            if (neighborCell == null || neighborCell.IsEmpty || neighborCell.CurrentPiece == null)
                continue;

            JellyPiece neighborPiece = neighborCell.CurrentPiece;
            if (neighborPiece == placedPiece)
                continue;

            CollectEdgeMatches(placedPiece, neighborPiece, VectorToDirection(dir), unique);
        }

        foreach (var kv in unique)
            matches.Add(kv.Value);

        return matches.Count > 0;
    }

    private void CollectEdgeMatches(JellyPiece sourcePiece, JellyPiece targetPiece, ContactDirection dir, Dictionary<string, MatchedSubCellData> unique)
    {
        List<JellySubCell> sourceEdge = GetEdgeSubCells(sourcePiece, dir);
        List<JellySubCell> targetEdge = GetEdgeSubCells(targetPiece, Opposite(dir));

        for (int i = 0; i < sourceEdge.Count; i++)
        {
            JellySubCell a = sourceEdge[i];
            if (a == null)
                continue;

            for (int j = 0; j < targetEdge.Count; j++)
            {
                JellySubCell b = targetEdge[j];
                if (b == null)
                    continue;

                if (a.color != b.color)
                    continue;

                if (!SlotsActuallyTouch(a.slot, b.slot, dir))
                    continue;

                AddUnique(unique, sourcePiece, a);
                AddUnique(unique, targetPiece, b);
            }
        }
    }

    private List<JellySubCell> GetEdgeSubCells(JellyPiece piece, ContactDirection dir)
    {
        List<JellySubCell> result = new List<JellySubCell>();
        if (piece == null || piece.SubCells == null)
            return result;

        for (int i = 0; i < piece.SubCells.Count; i++)
        {
            JellySubCell sub = piece.SubCells[i];
            if (sub == null)
                continue;

            if (IsOnEdge(sub.slot, dir))
                result.Add(sub);
        }

        return result;
    }

    private bool IsOnEdge(JellySlot slot, ContactDirection dir)
    {
        switch (dir)
        {
            case ContactDirection.Up:
                return slot == JellySlot.Full ||
                       slot == JellySlot.Top ||
                       slot == JellySlot.TopLeft ||
                       slot == JellySlot.TopRight ||
                       slot == JellySlot.Left ||
                       slot == JellySlot.Right;

            case ContactDirection.Down:
                return slot == JellySlot.Full ||
                       slot == JellySlot.BottomLeft ||
                       slot == JellySlot.BottomRight ||
                       slot == JellySlot.Left ||
                       slot == JellySlot.Right;

            case ContactDirection.Left:
                return slot == JellySlot.Full ||
                       slot == JellySlot.Left ||
                       slot == JellySlot.Top ||
                       slot == JellySlot.TopLeft ||
                       slot == JellySlot.BottomLeft;

            case ContactDirection.Right:
                return slot == JellySlot.Full ||
                       slot == JellySlot.Right ||
                       slot == JellySlot.Top ||
                       slot == JellySlot.TopRight ||
                       slot == JellySlot.BottomRight;
        }

        return false;
    }

    private bool SlotsActuallyTouch(JellySlot a, JellySlot b, ContactDirection dir)
    {
        switch (dir)
        {
            case ContactDirection.Up:
                return TouchUp(a, b);

            case ContactDirection.Down:
                return TouchDown(a, b);

            case ContactDirection.Left:
                return TouchLeft(a, b);

            case ContactDirection.Right:
                return TouchRight(a, b);
        }

        return false;
    }

    private bool TouchUp(JellySlot a, JellySlot b)
    {
        if (a == JellySlot.Full)
            return b == JellySlot.Full || b == JellySlot.Left || b == JellySlot.Right || b == JellySlot.BottomLeft || b == JellySlot.BottomRight;

        if (a == JellySlot.Left)
            return b == JellySlot.Full || b == JellySlot.BottomLeft;

        if (a == JellySlot.Right)
            return b == JellySlot.Full || b == JellySlot.BottomRight;

        if (a == JellySlot.Top)
            return b == JellySlot.BottomLeft || b == JellySlot.BottomRight;

        if (a == JellySlot.TopLeft)
            return b == JellySlot.BottomLeft;

        if (a == JellySlot.TopRight)
            return b == JellySlot.BottomRight;

        return false;
    }

    private bool TouchDown(JellySlot a, JellySlot b)
    {
        if (a == JellySlot.Full)
            return b == JellySlot.Full || b == JellySlot.Left || b == JellySlot.Right || b == JellySlot.Top || b == JellySlot.TopLeft || b == JellySlot.TopRight;

        if (a == JellySlot.Left)
            return b == JellySlot.Full || b == JellySlot.TopLeft;

        if (a == JellySlot.Right)
            return b == JellySlot.Full || b == JellySlot.TopRight;

        if (a == JellySlot.BottomLeft)
            return b == JellySlot.Top || b == JellySlot.TopLeft;

        if (a == JellySlot.BottomRight)
            return b == JellySlot.Top || b == JellySlot.TopRight;

        return false;
    }

    private bool TouchLeft(JellySlot a, JellySlot b)
    {
        if (a == JellySlot.Full)
            return b == JellySlot.Full || b == JellySlot.Right || b == JellySlot.TopRight || b == JellySlot.BottomRight;

        if (a == JellySlot.Left)
            return b == JellySlot.Full || b == JellySlot.Right;

        if (a == JellySlot.Top)
            return b == JellySlot.Right || b == JellySlot.TopRight;

        if (a == JellySlot.TopLeft)
            return b == JellySlot.TopRight;

        if (a == JellySlot.BottomLeft)
            return b == JellySlot.BottomRight;

        return false;
    }

    private bool TouchRight(JellySlot a, JellySlot b)
    {
        if (a == JellySlot.Full)
            return b == JellySlot.Full || b == JellySlot.Left || b == JellySlot.TopLeft || b == JellySlot.BottomLeft;

        if (a == JellySlot.Right)
            return b == JellySlot.Full || b == JellySlot.Left;

        if (a == JellySlot.Top)
            return b == JellySlot.Left || b == JellySlot.TopLeft;

        if (a == JellySlot.TopRight)
            return b == JellySlot.TopLeft;

        if (a == JellySlot.BottomRight)
            return b == JellySlot.BottomLeft;

        return false;
    }

    private ContactDirection Opposite(ContactDirection dir)
    {
        switch (dir)
        {
            case ContactDirection.Up: return ContactDirection.Down;
            case ContactDirection.Down: return ContactDirection.Up;
            case ContactDirection.Left: return ContactDirection.Right;
            default: return ContactDirection.Left;
        }
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

[System.Serializable]
public class MatchedSubCellData
{
    public JellyPiece piece;
    public string subCellId;
    public JellyColor color;
    public JellySlot slot;

    public MatchedSubCellData(JellyPiece piece, string subCellId, JellyColor color, JellySlot slot)
    {
        this.piece = piece;
        this.subCellId = subCellId;
        this.color = color;
        this.slot = slot;
    }
}