using System.Collections.Generic;
using UnityEngine;

public class MergeSystem : MonoBehaviour
{
    [SerializeField] private BoardManager board;
    [SerializeField] private int minMatchCount = 2;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public bool TryGetMatchGroup(Vector2Int startCoord, out List<JellyPiece> matchedPieces, out JellyColor matchedColor)
    {
        matchedPieces = null;
        matchedColor = JellyColor.Red;

        if (board == null || !board.IsInside(startCoord))
            return false;

        CellData startCell = board.GetCell(startCoord);
        if (startCell == null || startCell.IsEmpty || startCell.CurrentPiece == null)
            return false;

        JellyPiece startPiece = startCell.CurrentPiece;
        List<JellyColor> candidateColors = startPiece.GetDistinctColors();

        List<JellyPiece> bestGroup = null;
        bool found = false;

        for (int i = 0; i < candidateColors.Count; i++)
        {
            JellyColor testColor = candidateColors[i];
            List<JellyPiece> group = FloodFillSameColor(startCoord, testColor);

            if (group.Count >= minMatchCount)
            {
                if (!found || group.Count > bestGroup.Count)
                {
                    found = true;
                    bestGroup = group;
                    matchedColor = testColor;
                }
            }
        }

        if (!found)
            return false;

        matchedPieces = bestGroup;
        return true;
    }

    private List<JellyPiece> FloodFillSameColor(Vector2Int startCoord, JellyColor color)
    {
        List<JellyPiece> result = new List<JellyPiece>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startCoord);
        visited.Add(startCoord);

        while (queue.Count > 0)
        {
            Vector2Int coord = queue.Dequeue();

            if (!board.IsInside(coord))
                continue;

            CellData cell = board.GetCell(coord);
            if (cell == null || cell.IsEmpty || cell.CurrentPiece == null)
                continue;

            JellyPiece piece = cell.CurrentPiece;
            if (!piece.HasColor(color))
                continue;

            if (!result.Contains(piece))
                result.Add(piece);

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int next = coord + Directions[i];
                if (visited.Contains(next))
                    continue;

                visited.Add(next);

                if (!board.IsInside(next))
                    continue;

                CellData nextCell = board.GetCell(next);
                if (nextCell == null || nextCell.IsEmpty || nextCell.CurrentPiece == null)
                    continue;

                if (!nextCell.CurrentPiece.HasColor(color))
                    continue;

                queue.Enqueue(next);
            }
        }

        return result;
    }
}