using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int width = 6;
    [SerializeField] private int height = 8;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 origin = new Vector2(-2.5f, -3.5f);

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private CellData[,] grid;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public Vector2 Origin => origin;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Awake()
    {
        InitBoard();
    }

    public void ConfigureBoard(int newWidth, int newHeight)
    {
        width = Mathf.Max(1, newWidth);
        height = Mathf.Max(1, newHeight);
        InitBoard();
    }

    public void InitBoard()
    {
        grid = new CellData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new CellData(new Vector2Int(x, y));
            }
        }
    }

    public bool IsInsideGrid(Vector2Int coord)
    {
        return coord.x >= 0 && coord.x < width &&
               coord.y >= 0 && coord.y < height;
    }

    public CellData GetCell(Vector2Int coord)
    {
        if (!IsInsideGrid(coord))
            return null;

        return grid[coord.x, coord.y];
    }

    public Vector2 GridToWorld(Vector2Int coord)
    {
        return origin + new Vector2(coord.x * cellSize, coord.y * cellSize);
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        Vector2 local = worldPos - origin;
        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.y / cellSize);
        return new Vector2Int(x, y);
    }

    public bool TryPlacePiece(JellyPiece piece, Vector2Int targetCoord)
    {
        if (piece == null)
            return false;

        if (!IsInsideGrid(targetCoord))
            return false;

        CellData targetCell = GetCell(targetCoord);
        if (targetCell == null || !targetCell.IsEmpty)
            return false;

        if (piece.HasCell)
            RemovePiece(piece.CurrentCoord);

        targetCell.SetPiece(piece);
        piece.SetCoord(targetCoord);
        piece.transform.position = GridToWorld(targetCoord);
        piece.PlayLanding(); // Animation

        return true;
    }

    public void RemovePiece(Vector2Int coord)
    {
        CellData cell = GetCell(coord);
        if (cell == null || cell.IsEmpty)
            return;

        JellyPiece piece = cell.CurrentPiece;
        if (piece != null)
            piece.ClearCoord();

        cell.Clear();
    }

    public bool HasEmptyCell()
    {
        if (grid == null)
            return false;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y].IsEmpty)
                    return true;
            }
        }

        return false;
    }

    public List<CellData> GetAllFilledCells()
    {
        List<CellData> result = new List<CellData>();

        if (grid == null)
            return result;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!grid[x, y].IsEmpty)
                    result.Add(grid[x, y]);
            }
        }

        return result;
    }

    public List<JellyPiece> GetNeighbors(Vector2Int coord)
    {
        List<JellyPiece> result = new List<JellyPiece>();

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int next = coord + Directions[i];
            if (!IsInsideGrid(next))
                continue;

            CellData cell = GetCell(next);
            if (cell == null || cell.IsEmpty || cell.CurrentPiece == null)
                continue;

            result.Add(cell.CurrentPiece);
        }

        return result;
    }

    public bool IsCellEmpty(Vector2Int coord)
    {
        CellData cell = GetCell(coord);
        return cell != null && cell.IsEmpty;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 pos = origin + new Vector2(x * cellSize, y * cellSize);
                Gizmos.DrawWireCube(pos, Vector3.one * cellSize * 0.95f);
            }
        }
    }
}