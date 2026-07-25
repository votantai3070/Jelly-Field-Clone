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

    private GridVisual gridVisual;
    private Camera mainCamera;
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
        mainCamera = Camera.main;
    }

    public void ConfigureBoard(int newWidth, int newHeight)
    {
        ClearBoardRuntime();

        width = Mathf.Max(1, newWidth);
        height = Mathf.Max(1, newHeight);

        if (mainCamera != null)
            CenterBoardToCamera(mainCamera);

        InitBoard();
        RefreshGridVisual();
    }

    public void InitBoard()
    {
        grid = new CellData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                grid[x, y] = new CellData(coord);
            }
        }
    }

    public bool IsInsideGrid(Vector2Int coord)
    {
        bool isInsideX = coord.x >= 0 && coord.x < width;
        bool isInsideY = coord.y >= 0 && coord.y < height;
        return isInsideX && isInsideY;
    }

    public CellData GetCell(Vector2Int coord)
    {
        if (grid == null)
            return null;

        if (!IsInsideGrid(coord))
            return null;

        return grid[coord.x, coord.y];
    }

    public bool IsCellEmpty(Vector2Int coord)
    {
        CellData cell = GetCell(coord);
        return cell != null && cell.IsPieceEmpty;
    }

    public Vector2 GridToWorld(Vector2Int coord)
    {
        float worldX = origin.x + coord.x * cellSize;
        float worldY = origin.y + coord.y * cellSize;
        return new Vector2(worldX, worldY);
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        Vector2 localPos = worldPos - origin;
        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int y = Mathf.RoundToInt(localPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 GetBoardCenterWorld()
    {
        float boardWidth = (width - 1) * cellSize;
        float boardHeight = (height - 1) * cellSize;

        float centerX = origin.x + boardWidth * 0.5f;
        float centerY = origin.y + boardHeight * 0.5f;

        return new Vector3(centerX, centerY, 0f);
    }

    public void CenterBoardToCamera(Camera targetCamera)
    {
        if (targetCamera == null)
            return;

        Vector3 worldCenter = targetCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, Mathf.Abs(targetCamera.transform.position.z))
        );

        SetBoardCenter(new Vector2(worldCenter.x, worldCenter.y));
    }

    public void SetBoardCenter(Vector2 centerWorld)
    {
        float halfWidth = (width - 1) * cellSize * 0.5f;
        float halfHeight = (height - 1) * cellSize * 0.5f;

        origin = centerWorld - new Vector2(halfWidth, halfHeight);
    }

    public bool TryPlacePiece(JellyPiece piece, Vector2Int targetCoord)
    {
        if (piece == null)
            return false;

        if (!IsInsideGrid(targetCoord))
            return false;

        CellData targetCell = GetCell(targetCoord);
        if (targetCell == null)
            return false;

        if (!targetCell.IsPieceEmpty)
            return false;

        RemovePieceFromOldCellIfNeeded(piece);
        PlacePieceToCell(piece, targetCell, targetCoord);

        return true;
    }

    public void RemovePiece(Vector2Int coord)
    {
        CellData cell = GetCell(coord);
        if (cell == null)
            return;

        if (cell.IsPieceEmpty)
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
        List<CellData> filledCells = new List<CellData>();

        if (grid == null)
            return filledCells;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!grid[x, y].IsPieceEmpty)
                    filledCells.Add(grid[x, y]);
            }
        }

        return filledCells;
    }

    public List<JellyPiece> GetNeighbors(Vector2Int coord)
    {
        List<JellyPiece> neighbors = new List<JellyPiece>();

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int neighborCoord = coord + Directions[i];
            JellyPiece neighborPiece = GetPieceAt(neighborCoord);

            if (neighborPiece != null)
                neighbors.Add(neighborPiece);
        }

        return neighbors;
    }

    public void ClearBoardRuntime()
    {
        if (grid == null)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                ClearCellRuntime(x, y);
            }
        }

        grid = null;
    }

    private void RefreshGridVisual()
    {
        if (gridVisual != null)
            gridVisual.GenerateGridVisual();
    }

    private void RemovePieceFromOldCellIfNeeded(JellyPiece piece)
    {
        if (piece.HasCell)
            RemovePiece(piece.CurrentCoord);
    }

    private void PlacePieceToCell(JellyPiece piece, CellData targetCell, Vector2Int targetCoord)
    {
        targetCell.SetPiece(piece);
        piece.SetCoord(targetCoord);
        piece.transform.position = GridToWorld(targetCoord);
        piece.PlayLanding();
    }

    private JellyPiece GetPieceAt(Vector2Int coord)
    {
        if (!IsInsideGrid(coord))
            return null;

        CellData cell = GetCell(coord);
        if (cell == null)
            return null;

        if (cell.IsPieceEmpty)
            return null;

        return cell.CurrentPiece;
    }

    private void ClearCellRuntime(int x, int y)
    {
        CellData cell = grid[x, y];
        if (cell == null || cell.IsPieceEmpty)
            return;

        JellyPiece piece = cell.CurrentPiece;
        if (piece != null)
        {
            piece.ClearCoord();
            DespawnOrDestroyPiece(piece);
        }

        cell.Clear();
    }

    private void DespawnOrDestroyPiece(JellyPiece piece)
    {
        if (piece == null)
            return;

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Despawn(piece.gameObject);
        else
            Destroy(piece.gameObject);
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
                Vector2 cellWorldPos = origin + new Vector2(x * cellSize, y * cellSize);
                Vector3 gizmoSize = Vector3.one * cellSize * 0.95f;

                Gizmos.DrawWireCube(cellWorldPos, gizmoSize);
            }
        }
    }
}