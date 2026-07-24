using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    private GridVisual gridVisual;
    private Camera cam;

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
        gridVisual = GetComponent<GridVisual>();
        cam = Camera.main;
    }

    public void ConfigureBoard(int newWidth, int newHeight)
    {
        ClearBoardRuntime();

        width = Mathf.Max(1, newWidth);
        height = Mathf.Max(1, newHeight);

        if (cam != null)
            CenterBoardToCamera(cam);

        InitBoard();

        if (gridVisual != null)
            gridVisual.GenerateGridVisual();
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

    public Vector3 GetBoardCenterWorld()
    {
        float w = (width - 1) * cellSize;
        float h = (height - 1) * cellSize;
        return origin + new Vector2(w * 0.5f, h * 0.5f);
    }

    public CellData GetCell(Vector2Int coord)
    {
        if (!IsInsideGrid(coord) || grid == null)
            return null;

        return grid[coord.x, coord.y];
    }

    public void CenterBoardToCamera(Camera cam)
    {
        if (cam == null)
            return;

        Vector3 center = cam.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, Mathf.Abs(cam.transform.position.z))
        );

        SetBoardCenter(new Vector2(center.x, center.y));
    }

    public void SetBoardCenter(Vector2 centerWorld)
    {
        origin = centerWorld - new Vector2(
            (width - 1) * cellSize * 0.5f,
            (height - 1) * cellSize * 0.5f
        );
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
        if (targetCell == null || !targetCell.IsPieceEmpty)
            return false;

        if (piece.HasCell)
            RemovePiece(piece.CurrentCoord);

        targetCell.SetPiece(piece);
        piece.SetCoord(targetCoord);
        piece.transform.position = GridToWorld(targetCoord);
        piece.PlayLanding();

        return true;
    }

    public void RemovePiece(Vector2Int coord)
    {
        CellData cell = GetCell(coord);
        if (cell == null || cell.IsPieceEmpty)
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
                if (grid[x, y].IsPieceEmpty)
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
                if (!grid[x, y].IsPieceEmpty)
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
            if (cell == null || cell.IsPieceEmpty || cell.CurrentPiece == null)
                continue;

            result.Add(cell.CurrentPiece);
        }

        return result;
    }

    public bool IsCellEmpty(Vector2Int coord)
    {
        CellData cell = GetCell(coord);
        return cell != null && cell.IsPieceEmpty;
    }

    public void ClearBoardRuntime()
    {
        if (grid != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CellData cell = grid[x, y];
                    if (cell == null || cell.IsPieceEmpty)
                        continue;

                    JellyPiece piece = cell.CurrentPiece;

                    if (piece != null)
                    {
                        piece.ClearCoord();

                        if (ObjectPool.Instance != null)
                            ObjectPool.Instance.Despawn(piece.gameObject);
                        else
                            Destroy(piece.gameObject);
                    }

                    cell.Clear();
                }
            }
        }

        grid = null;
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