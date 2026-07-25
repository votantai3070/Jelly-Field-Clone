using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Action<LevelGoalData> OnGoalObjectiveUIChanged;

    [Header("Core Refs")]
    [SerializeField] private BoardManager board;
    [SerializeField] private MergeSystem mergeSystem;
    [SerializeField] private GoalSystem goalSystem;
    [SerializeField] private LevelGoalData levelData;
    [SerializeField] private JellyPiece jellyPrefab;

    [Header("Input")]
    [SerializeField] private InputHandler inputHandler;

    [Header("Effects")]
    [SerializeField] private JellyPopEffect jellyPopEffectPrefab;
    [SerializeField] private string jellyPopEffectPoolTag = "JellyPopEffect";

    [Header("Timing")]
    [SerializeField] private float preCollectPulseDelay = 0.06f;
    [SerializeField] private float postResolveDelay = 0.08f;
    [SerializeField] private float nextSpawnDelay = 0.12f;

    [Header("Runtime")]
    [SerializeField] private int coins = 0;

    public bool IsGameEnded { get; private set; }
    public bool IsResolving { get; private set; }

    private const int MaxResolveIterations = 200;

    private void Start()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (goalSystem != null)
            goalSystem.OnWin += HandleWin;
    }

    private void OnDisable()
    {
        if (goalSystem != null)
            goalSystem.OnWin -= HandleWin;
    }

    public LevelGoalData GetCurrentLevelData()
    {
        return levelData;
    }

    public void InitializeLevel(LevelGoalData levelGoal)
    {
        ResetCurrentLevelRuntime();

        if (levelGoal != null)
        {
            levelData = levelGoal;
            SetupBoardAndGoals(levelData);
            NotifyGoalUI();
        }

        TrySpawnNextPiece();
    }

    public void ResetCurrentLevelRuntime()
    {
        StopAllCoroutines();

        IsGameEnded = false;
        IsResolving = false;

        if (inputHandler != null)
            inputHandler.ClearCurrentPiece();

        if (board != null)
            board.ClearBoardRuntime();
    }

    public JellyPiece GetNextPiecePrefab()
    {
        if (jellyPrefab == null)
            Debug.LogError("GameManager jellyPrefab is NULL");

        return jellyPrefab;
    }

    public void SetupSpawnedPiece(JellyPiece piece)
    {
        if (piece == null)
            return;

        piece.Setup(GenerateRandomSubCells());
    }

    public void ResolveTurn(JellyPiece placedPiece, Vector2Int placedCoord)
    {
        if (IsGameEnded)
            return;

        if (IsResolving)
            return;

        StartCoroutine(ResolveTurnRoutine(placedCoord));
    }

    private void NotifyGoalUI()
    {
        if (levelData != null)
            OnGoalObjectiveUIChanged?.Invoke(levelData);
    }

    private void ValidateReferences()
    {
        if (board == null)
            Debug.LogError("GameManager missing BoardManager");

        if (mergeSystem == null)
            Debug.LogError("GameManager missing MergeSystem");

        if (goalSystem == null)
            Debug.LogError("GameManager missing GoalSystem");

        if (levelData == null)
            Debug.LogError("GameManager missing LevelGoalData");

        if (jellyPrefab == null)
            Debug.LogError("GameManager missing JellyPrefab");

        if (inputHandler == null)
            Debug.LogWarning("GameManager missing InputHandler");

        if (jellyPopEffectPrefab == null)
            Debug.LogWarning("GameManager missing JellyPopEffect prefab");
    }

    private void SetupBoardAndGoals(LevelGoalData levelGoal)
    {
        if (board != null)
            board.ConfigureBoard(levelGoal.width, levelGoal.height);

        if (goalSystem != null)
            goalSystem.Initialize(levelGoal);
    }

    private void TrySpawnNextPiece()
    {
        if (inputHandler != null && !IsGameEnded)
            inputHandler.SpawnNextPiece();
    }

    private List<JellySubCell> GenerateRandomSubCells()
    {
        int count = UnityEngine.Random.Range(1, 5);
        List<JellySubCell> result = new List<JellySubCell>(count);

        for (int i = 0; i < count; i++)
            result.Add(null);

        for (int i = 0; i < count; i++)
        {
            JellyColor color = GetRandomColorExceptAdjacent(result, count, i);
            result[i] = new JellySubCell(color);
        }

        return result;
    }

    private JellyColor GetRandomColorExceptAdjacent(List<JellySubCell> current, int count, int index)
    {
        List<JellyColor> candidates = new List<JellyColor>
        {
            JellyColor.Red,
            JellyColor.Yellow,
            JellyColor.Blue,
            JellyColor.Green
        };

        for (int i = 0; i < index; i++)
        {
            if (current[i] == null)
                continue;

            if (AreSubCellsAdjacentInSameJelly(count, index, i))
                candidates.Remove(current[i].color);
        }

        if (candidates.Count == 0)
            return GetRandomColor();

        int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[randomIndex];
    }

    private bool AreSubCellsAdjacentInSameJelly(int count, int a, int b)
    {
        if (a == b)
            return false;

        if (count == 1)
            return false;

        if (count == 2)
            return (a == 0 && b == 1) || (a == 1 && b == 0);

        if (count == 3)
        {
            return
                (a == 0 && (b == 1 || b == 2)) ||
                (b == 0 && (a == 1 || a == 2)) ||
                (a == 1 && b == 2) ||
                (a == 2 && b == 1);
        }

        return
            (a == 0 && (b == 1 || b == 2)) ||
            (a == 1 && (b == 0 || b == 3)) ||
            (a == 2 && (b == 0 || b == 3)) ||
            (a == 3 && (b == 1 || b == 2));
    }

    private JellyColor GetRandomColor()
    {
        return (JellyColor)UnityEngine.Random.Range(0, 4);
    }

    private IEnumerator ResolveTurnRoutine(Vector2Int placedCoord)
    {
        IsResolving = true;

        Queue<Vector2Int> pendingCoords = new Queue<Vector2Int>();
        HashSet<string> resolvedStates = new HashSet<string>();
        int iterationCount = 0;

        pendingCoords.Enqueue(placedCoord);

        while (pendingCoords.Count > 0 && iterationCount < MaxResolveIterations)
        {
            iterationCount++;

            Vector2Int currentCoord = pendingCoords.Dequeue();

            if (!CanResolveAtCoord(currentCoord))
                continue;

            bool hasMatch = mergeSystem.TryGetTouchMatchesForPlacedPiece(currentCoord, out List<MatchedSubCellData> matchedSubCells);
            if (!hasMatch || matchedSubCells == null || matchedSubCells.Count == 0)
                continue;

            string stateKey = BuildMatchStateKey(currentCoord, matchedSubCells);
            if (resolvedStates.Contains(stateKey))
                continue;

            resolvedStates.Add(stateKey);

            yield return StartCoroutine(ResolveSingleMatchRoutine(matchedSubCells, pendingCoords));

            if (goalSystem != null && goalSystem.IsWin)
            {
                IsResolving = false;
                yield break;
            }

            yield return new WaitForSeconds(postResolveDelay);
        }

        if (iterationCount >= MaxResolveIterations)
            Debug.LogWarning("ResolveTurnRoutine stopped by maxIterations safeguard");

        yield return StartCoroutine(FinishResolveRoutine());
    }

    private bool CanResolveAtCoord(Vector2Int coord)
    {
        if (board == null)
            return false;

        return board.IsInsideGrid(coord);
    }

    private IEnumerator ResolveSingleMatchRoutine(
        List<MatchedSubCellData> matchedSubCells,
        Queue<Vector2Int> pendingCoords)
    {
        HashSet<JellyPiece> touchedPieces = CollectTouchedPieces(matchedSubCells);

        PlayPreCollectPulseOnPieces(touchedPieces);
        yield return new WaitForSeconds(preCollectPulseDelay);

        RemoveMatchedSubCells(matchedSubCells);

        List<JellyPiece> emptiedPieces = new List<JellyPiece>();
        HashSet<JellyPiece> survivedPieces = new HashSet<JellyPiece>();

        SplitPiecesByEmptyState(touchedPieces, emptiedPieces, survivedPieces);

        PlayPreCollectPulseOnPieces(survivedPieces);

        if (emptiedPieces.Count > 0)
        {
            Vector3 collectCenter = GetCollectCenter(emptiedPieces);
            yield return StartCoroutine(PlayCollectAndRemoveRoutine(emptiedPieces, collectCenter));
        }

        EnqueueNeighborCoords(survivedPieces, pendingCoords);
    }

    private HashSet<JellyPiece> CollectTouchedPieces(List<MatchedSubCellData> matchedSubCells)
    {
        HashSet<JellyPiece> touchedPieces = new HashSet<JellyPiece>();

        for (int i = 0; i < matchedSubCells.Count; i++)
        {
            MatchedSubCellData data = matchedSubCells[i];
            if (data != null && data.piece != null)
                touchedPieces.Add(data.piece);
        }

        return touchedPieces;
    }

    private void PlayPreCollectPulseOnPieces(IEnumerable<JellyPiece> pieces)
    {
        foreach (JellyPiece piece in pieces)
        {
            if (piece != null)
                piece.PlayPreCollectPulse();
        }
    }

    private void RemoveMatchedSubCells(List<MatchedSubCellData> matchedSubCells)
    {
        for (int i = 0; i < matchedSubCells.Count; i++)
        {
            MatchedSubCellData data = matchedSubCells[i];
            if (data == null || data.piece == null || string.IsNullOrEmpty(data.subCellId))
                continue;

            bool removed = data.piece.RemoveSubCellById(data.subCellId);
            if (removed && goalSystem != null)
                goalSystem.CollectRemovedColor(data.color, 1);
        }
    }

    private void SplitPiecesByEmptyState(
        HashSet<JellyPiece> touchedPieces,
        List<JellyPiece> emptiedPieces,
        HashSet<JellyPiece> survivedPieces)
    {
        foreach (JellyPiece piece in touchedPieces)
        {
            if (piece == null)
                continue;

            if (piece.IsEmptyCompletely())
                emptiedPieces.Add(piece);
            else
                survivedPieces.Add(piece);
        }
    }

    private void EnqueueNeighborCoords(HashSet<JellyPiece> survivedPieces, Queue<Vector2Int> pendingCoords)
    {
        foreach (JellyPiece piece in survivedPieces)
        {
            if (piece == null || !piece.HasCell)
                continue;

            Vector2Int coord = piece.CurrentCoord;

            pendingCoords.Enqueue(coord);
            pendingCoords.Enqueue(coord + Vector2Int.up);
            pendingCoords.Enqueue(coord + Vector2Int.right);
            pendingCoords.Enqueue(coord + Vector2Int.down);
            pendingCoords.Enqueue(coord + Vector2Int.left);
        }
    }

    private IEnumerator FinishResolveRoutine()
    {
        if (board != null && !board.HasEmptyCell())
        {
            IsGameEnded = true;
            Debug.Log("LOSE - Board Full");

            if (UI.Instance != null && UI.Instance.losePanel != null)
                UI.Instance.losePanel.SetActive(true);

            IsResolving = false;
            yield break;
        }

        yield return new WaitForSeconds(nextSpawnDelay);

        IsResolving = false;
        TrySpawnNextPiece();
    }

    private string BuildMatchStateKey(Vector2Int coord, List<MatchedSubCellData> matchedSubCells)
    {
        if (matchedSubCells == null || matchedSubCells.Count == 0)
            return coord.ToString();

        List<string> ids = new List<string>();

        for (int i = 0; i < matchedSubCells.Count; i++)
        {
            MatchedSubCellData data = matchedSubCells[i];
            if (data == null || data.piece == null || string.IsNullOrEmpty(data.subCellId))
                continue;

            ids.Add(data.piece.GetInstanceID() + "_" + data.subCellId);
        }

        ids.Sort();
        return coord.x + "_" + coord.y + "|" + string.Join(",", ids);
    }

    private IEnumerator PlayCollectAndRemoveRoutine(List<JellyPiece> piecesToRemove, Vector3 collectCenter)
    {
        int completedCount = 0;
        int totalCount = CountValidPieces(piecesToRemove);

        if (totalCount == 0)
            yield break;

        for (int i = 0; i < piecesToRemove.Count; i++)
        {
            JellyPiece piece = piecesToRemove[i];
            if (piece == null)
                continue;

            Vector2Int coord = piece.CurrentCoord;

            if (board != null)
                board.RemovePiece(coord);

            piece.PlayCollectToPoint(collectCenter, () =>
            {
                if (piece != null)
                {
                    SpawnPopEffect(piece.transform.position, piece);

                    if (ObjectPool.Instance != null)
                        ObjectPool.Instance.Despawn(piece.gameObject);
                    else
                        Destroy(piece.gameObject);
                }

                completedCount++;
            });
        }

        while (completedCount < totalCount)
            yield return null;
    }

    private int CountValidPieces(List<JellyPiece> pieces)
    {
        int count = 0;

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] != null)
                count++;
        }

        return count;
    }

    private Vector3 GetCollectCenter(List<JellyPiece> pieces)
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        for (int i = 0; i < pieces.Count; i++)
        {
            JellyPiece piece = pieces[i];
            if (piece == null)
                continue;

            center += piece.transform.position;
            count++;
        }

        if (count == 0)
            return Vector3.zero;

        return center / count;
    }

    private void SpawnPopEffect(Vector3 position, JellyPiece piece)
    {
        if (jellyPopEffectPrefab == null || piece == null)
            return;

        if (ObjectPool.Instance == null)
            return;

        GameObject fxObject = ObjectPool.Instance.Spawn(
            jellyPopEffectPoolTag,
            position,
            Quaternion.identity,
            null
        );

        if (fxObject == null)
            return;

        JellyPopEffect popEffect = fxObject.GetComponent<JellyPopEffect>();
        if (popEffect == null)
            return;

        popEffect.Play(GetColorFromJelly(piece));
    }

    private Color GetColorFromJelly(JellyPiece piece)
    {
        if (piece == null || piece.IsEmptyCompletely())
            return Color.white;

        switch (piece.GetPrimaryColor())
        {
            case JellyColor.Red:
                return new Color(1f, 0.35f, 0.35f);

            case JellyColor.Yellow:
                return new Color(1f, 0.87f, 0.25f);

            case JellyColor.Blue:
                return new Color(0.3f, 0.55f, 1f);

            case JellyColor.Green:
                return new Color(0.35f, 0.9f, 0.45f);

            default:
                return Color.white;
        }
    }

    private void HandleWin()
    {
        if (IsGameEnded)
            return;

        IsGameEnded = true;

        if (levelData != null)
            coins += levelData.winCoinReward;

        Debug.Log("WIN - Coins: " + coins);
    }
}