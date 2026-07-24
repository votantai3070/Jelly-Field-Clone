using UnityEngine;

public class GridVisual : MonoBehaviour
{
    [SerializeField] private BoardManager board;
    [SerializeField] private GameObject cellVisualPrefab;
    [SerializeField] private Transform container;

    public void GenerateGridVisual()
    {
        if (board == null || cellVisualPrefab == null) return;

        if (container == null)
        {
            GameObject root = new GameObject("GridVisualRoot");
            root.transform.SetParent(transform);
            container = root.transform;
        }

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                GameObject cell = Instantiate(cellVisualPrefab, container);
                cell.transform.position = board.GridToWorld(new Vector2Int(x, y));
                cell.transform.localScale = Vector3.one * board.CellSize * 0.95f;
                cell.name = $"Cell_{x}_{y}";
            }
        }
    }
}