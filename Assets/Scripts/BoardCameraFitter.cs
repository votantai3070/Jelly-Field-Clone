using UnityEngine;

[ExecuteAlways]
public class BoardCameraFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private BoardManager board;
    [SerializeField] private float extraPadding = 0.5f;

    public void Fit()
    {
        if (targetCamera == null || board == null)
            return;

        float boardWidth = board.Width * board.CellSize;
        float boardHeight = board.Height * board.CellSize;

        float aspect = (float)Screen.width / Screen.height;

        float orthoByHeight = (boardHeight * 0.5f) + extraPadding;
        float orthoByWidth = (boardWidth * 0.5f) / aspect + extraPadding;

        targetCamera.orthographicSize = Mathf.Max(orthoByHeight, orthoByWidth);

        Vector3 boardCenter = board.GetBoardCenterWorld();
        targetCamera.transform.position = new Vector3(boardCenter.x, boardCenter.y, targetCamera.transform.position.z);
    }

    private void Start()
    {
        Fit();
    }

    private void OnValidate()
    {
        Fit();
    }
}