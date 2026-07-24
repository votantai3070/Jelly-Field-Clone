using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoardManager board;
    [SerializeField] private GameManager gameManager;

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

    private int activeFingerId = -1;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        if (selectedPiece != null)
            ReleaseSelectedPieceForced();

        EnhancedTouchSupport.Disable();
        activeFingerId = -1;
    }

    private void Update()
    {
        if (gameManager == null || gameManager.IsGameEnded || gameManager.IsResolving)
            return;

        HandleTouch();
        UpdateDraggedPieceFollow();
    }

    private void HandleTouch()
    {
        if (mainCamera == null)
            return;

        var touches = EnhancedTouch.activeTouches;
        if (touches.Count == 0)
            return;

        if (activeFingerId == -1)
        {
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.phase != TouchPhase.Began)
                    continue;

                Vector3 world = ScreenToWorld(touch.screenPosition);
                if (TrySelectPiece(world))
                {
                    activeFingerId = touch.touchId;
                    break;
                }
            }

            return;
        }

        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            if (touch.touchId != activeFingerId)
                continue;

            Vector3 world = ScreenToWorld(touch.screenPosition);

            switch (touch.phase)
            {
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    DragSelectedPiece(world);
                    return;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    ReleaseSelectedPiece();
                    activeFingerId = -1;
                    return;
            }
        }
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCamera.transform.position.z))
        );
        world.z = 0f;
        return world;
    }

    private bool TrySelectPiece(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        if (hits == null || hits.Length == 0)
            return false;

        JellyPiece piece = null;

        for (int i = 0; i < hits.Length; i++)
        {
            piece = hits[i].GetComponent<JellyPiece>();
            if (piece != null)
                break;
        }

        if (piece == null || piece != currentPiece)
            return false;

        selectedPiece = piece;
        selectedStartPosition = piece.transform.position;

        dragTargetPosition = new Vector3(worldPos.x, worldPos.y, 0f);
        dragVelocity = Vector3.zero;
        lastPiecePosition = selectedPiece.transform.position;

        selectedPiece.StartDragJiggle();
        return true;
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

    private void ReleaseSelectedPieceForced()
    {
        if (selectedPiece == null)
            return;

        selectedPiece.StopDragJiggle(true);
        selectedPiece.transform.position = selectedStartPosition;
        selectedPiece = null;
        dragVelocity = Vector3.zero;
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
        JellyPiece piece = ObjectPool.Instance.Spawn<JellyPiece>(jellyPoolTag, null);

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

        activeFingerId = -1;
        dragVelocity = Vector3.zero;
    }
}