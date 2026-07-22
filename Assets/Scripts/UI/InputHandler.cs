using System.Collections;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoardManager board;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Vector3 spawnPreviewPosition = new Vector3(0f, 3.5f, 0f);

    private JellyPiece currentPiece;
    private JellyPiece selectedPiece;
    private Vector3 selectedStartPosition;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        SpawnNextPiece();
    }

    private void Update()
    {
        if (gameManager.IsGameEnded) return;
        if (Input.touchCount <= 0) return;

        HandleTouch();
    }

    private void HandleTouch()
    {
        Touch touch = Input.GetTouch(0);
        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 0f));
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
                ReleaseSelectedPiece(world);
                break;
        }
    }

    private void TrySelectPiece(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        if (hits.Length == 0) return;

        JellyPiece piece = null;

        for (int i = 0; i < hits.Length; i++)
        {
            piece = hits[i].GetComponent<JellyPiece>();
            if (piece != null) break;
        }

        if (piece == null) return;
        if (piece != currentPiece) return;

        selectedPiece = piece;
        selectedStartPosition = piece.transform.position;
    }

    private void DragSelectedPiece(Vector3 worldPos)
    {
        if (selectedPiece == null) return;
        selectedPiece.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
    }

    private void ReleaseSelectedPiece(Vector3 worldPos)
    {
        if (selectedPiece == null) return;

        Vector2Int coord = board.WorldToGrid(worldPos);
        bool placed = board.TryPlacePiece(selectedPiece, coord);

        if (placed)
        {
            gameManager.ResolveTurn(selectedPiece, coord);
            selectedPiece = null;
            currentPiece = null;

            if (!gameManager.IsGameEnded)
                StartCoroutine(SpawnNextDelayed(0.25f));
        }
        else
        {
            selectedPiece.transform.position = selectedStartPosition;
            selectedPiece = null;
        }
    }

    private IEnumerator SpawnNextDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!gameManager.IsGameEnded)
            SpawnNextPiece();
    }

    public void SpawnNextPiece()
    {
        if (gameManager.IsGameEnded) return;

        JellyPiece prefab = gameManager.GetNextPiecePrefab();
        if (prefab == null)
        {
            Debug.LogError("SpawnNextPiece failed: prefab is null");
            return;
        }

        currentPiece = Instantiate(prefab, spawnPreviewPosition, Quaternion.identity);
        gameManager.SetupSpawnedPiece(currentPiece);
    }
}