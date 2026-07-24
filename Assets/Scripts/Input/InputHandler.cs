using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoardManager board;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Vector3 spawnPreviewPosition = new Vector3(0f, 3.5f, 0f);

    [Header("Pooling")]
    [SerializeField] private string jellyPoolTag = "JellyPiece";

    [Header("Drag Feel")]
    [SerializeField] private float dragSmoothTime = 0.045f;
    [SerializeField] private float dragMaxSpeed = 30f;

    private JellyPiece currentPiece;
    private JellyPiece selectedPiece;
    private Vector3 selectedStartPosition;

    private Vector3 dragTargetPosition;
    private Vector3 dragVelocity;
    private Vector3 lastPiecePosition;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (gameManager == null || gameManager.IsGameEnded || gameManager.IsResolving)
            return;

        if (Input.touchCount > 0)
            HandleTouch();

        UpdateDraggedPieceFollow();
    }

    private void HandleTouch()
    {
        if (mainCamera == null)
            return;

        Touch touch = Input.GetTouch(0);
        Vector3 world = mainCamera.ScreenToWorldPoint(
            new Vector3(touch.position.x, touch.position.y, Mathf.Abs(mainCamera.transform.position.z))
        );
        world.z = 0f;

        switch (touch.phase)
        {
            case TouchPhase.Began:
                TrySelectPiece(world);
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                DragSelectedPiece(world);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                ReleaseSelectedPiece();
                break;
        }
    }

    private void TrySelectPiece(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        if (hits == null || hits.Length == 0)
            return;

        JellyPiece piece = null;

        for (int i = 0; i < hits.Length; i++)
        {
            piece = hits[i].GetComponent<JellyPiece>();
            if (piece != null)
                break;
        }

        if (piece == null || piece != currentPiece)
            return;

        selectedPiece = piece;
        selectedStartPosition = piece.transform.position;

        dragTargetPosition = new Vector3(worldPos.x, worldPos.y, 0f);
        dragVelocity = Vector3.zero;
        lastPiecePosition = selectedPiece.transform.position;

        selectedPiece.StartDragJiggle();
    }

    private void DragSelectedPiece(Vector3 worldPos)
    {
        if (selectedPiece == null)
            return;

        dragTargetPosition = new Vector3(worldPos.x, worldPos.y, 0f);
    }

    private void UpdateDraggedPieceFollow()
    {
        if (selectedPiece == null)
            return;

        Vector3 current = selectedPiece.transform.position;
        Vector3 next = Vector3.SmoothDamp(
            current,
            dragTargetPosition,
            ref dragVelocity,
            dragSmoothTime,
            dragMaxSpeed
        );

        next.z = 0f;
        selectedPiece.transform.position = next;

        Vector3 jiggleDelta = next - lastPiecePosition;
        selectedPiece.UpdateDragJiggle(jiggleDelta);
        lastPiecePosition = next;
    }

    private void ReleaseSelectedPiece()
    {
        if (selectedPiece == null || board == null)
            return;

        Vector2Int coord = board.WorldToGrid(selectedPiece.transform.position);
        bool placed = board.TryPlacePiece(selectedPiece, coord);

        if (placed)
        {
            JellyPiece placedPiece = selectedPiece;
            placedPiece.StopDragJiggle(false);

            selectedPiece = null;
            currentPiece = null;
            dragVelocity = Vector3.zero;

            if (gameManager != null)
                gameManager.ResolveTurn(placedPiece, coord);
        }
        else
        {
            selectedPiece.StopDragJiggle(true);
            selectedPiece.transform.position = selectedStartPosition;

            selectedPiece = null;
            dragVelocity = Vector3.zero;
        }
    }

    public void SpawnNextPiece()
    {
        if (gameManager == null || gameManager.IsGameEnded)
            return;

        if (currentPiece != null)
            return;

        if (ObjectPool.Instance == null)
        {
            Debug.LogError("SpawnNextPiece failed: ObjectPool.Instance is null");
            return;
        }

        Vector3 spawnPos = GetSpawnPreviewWorldPosition();
        JellyPiece piece = ObjectPool.Instance.Spawn<JellyPiece>(
            jellyPoolTag,
            null
        );

        if (piece == null)
        {
            Debug.LogError($"SpawnNextPiece failed: pool tag '{jellyPoolTag}' not found or missing JellyPiece component");
            return;
        }

        piece.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
        piece.transform.localScale = Vector3.one;

        currentPiece = piece;
        gameManager.SetupSpawnedPiece(currentPiece);
    }

    private Vector3 GetSpawnPreviewWorldPosition()
    {
        if (mainCamera == null)
            return spawnPreviewPosition;

        Vector3 viewPos = mainCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.2f, Mathf.Abs(mainCamera.transform.position.z))
        );

        viewPos.z = 0f;
        return viewPos;
    }

    public void ClearCurrentPiece()
    {
        if (ObjectPool.Instance == null)
        {
            Debug.LogError("ClearCurrentPiece failed: ObjectPool.Instance is null");
            return;
        }

        if (selectedPiece != null && selectedPiece == currentPiece)
        {
            ObjectPool.Instance.Despawn(selectedPiece.gameObject);
            selectedPiece = null;
            currentPiece = null;
        }
        else
        {
            if (selectedPiece != null)
                ObjectPool.Instance.Despawn(selectedPiece.gameObject);

            if (currentPiece != null)
                ObjectPool.Instance.Despawn(currentPiece.gameObject);

            selectedPiece = null;
            currentPiece = null;
        }

        dragVelocity = Vector3.zero;
    }
}