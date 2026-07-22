using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
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

    [Header("Timing")]
    [SerializeField] private float preCollectPulseDelay = 0.06f;
    [SerializeField] private float postResolveDelay = 0.08f;
    [SerializeField] private float nextSpawnDelay = 0.12f;

    [Header("Runtime")]
    [SerializeField] private int coins = 0;

    public bool IsGameEnded { get; private set; }
    public bool IsResolving { get; private set; }

    private void Start()
    {
        ValidateReferences();

        if (levelData != null)
        {
            board.ConfigureBoard(levelData.width, levelData.height);
            goalSystem.Initialize(levelData);
            goalSystem.OnWin += HandleWin;
        }

        if (inputHandler != null && !IsGameEnded)
            inputHandler.SpawnNextPiece();
    }

    private void OnDestroy()
    {
        if (goalSystem != null)
            goalSystem.OnWin -= HandleWin;
    }

    private void ValidateReferences()
    {
        if (board == null) Debug.LogError("GameManager missing BoardManager");
        if (mergeSystem == null) Debug.LogError("GameManager missing MergeSystem");
        if (goalSystem == null) Debug.LogError("GameManager missing GoalSystem");
        if (levelData == null) Debug.LogError("GameManager missing LevelGoalData");
        if (jellyPrefab == null) Debug.LogError("GameManager missing JellyPrefab");
        if (inputHandler == null) Debug.LogWarning("GameManager missing InputHandler");
        if (jellyPopEffectPrefab == null) Debug.LogWarning("GameManager missing JellyPopEffect prefab");
    }

    public JellyPiece GetNextPiecePrefab()
    {
        if (jellyPrefab == null)
            Debug.LogError("GameManager jellyPrefab is NULL");

        return jellyPrefab;
    }

    public void SetupSpawnedPiece(JellyPiece piece)
    {
        if (piece == null) return;
        piece.Setup(GenerateRandomSubCells());
    }

    private List<JellySubCell> GenerateRandomSubCells()
    {
        List<JellySubCell> result = new List<JellySubCell>();

        JellyColor dominant = GetRandomColor();
        result.Add(new JellySubCell(dominant));
        result.Add(new JellySubCell(dominant));

        result.Add(new JellySubCell(GetRandomColor()));
        result.Add(new JellySubCell(GetRandomColor()));

        return result;
    }

    private JellyColor GetRandomColor()
    {
        return (JellyColor)Random.Range(0, 4);
    }

    public void ResolveTurn(JellyPiece placedPiece, Vector2Int placedCoord)
    {
        if (IsGameEnded || IsResolving) return;
        StartCoroutine(ResolveTurnRoutine(placedCoord));
    }

    private IEnumerator ResolveTurnRoutine(Vector2Int placedCoord)
    {
        IsResolving = true;

        bool hasMatch = mergeSystem.TryGetMatchGroup(placedCoord, out List<JellyPiece> matchedPieces, out JellyColor matchedColor);

        if (hasMatch && matchedPieces != null && matchedPieces.Count > 0)
        {
            PlayPreCollectAnimation(matchedPieces);
            yield return new WaitForSeconds(preCollectPulseDelay);

            yield return StartCoroutine(RemoveOnlyMatchedColorRoutine(matchedPieces, matchedColor));

            yield return new WaitForSeconds(postResolveDelay);
        }

        if (goalSystem.IsWin)
        {
            IsResolving = false;
            yield break;
        }

        if (!board.HasEmptyCell())
        {
            IsGameEnded = true;
            Debug.Log("LOSE - Board Full");
            IsResolving = false;
            yield break;
        }

        yield return new WaitForSeconds(nextSpawnDelay);

        IsResolving = false;

        if (!IsGameEnded && inputHandler != null)
            inputHandler.SpawnNextPiece();
    }

    private void PlayPreCollectAnimation(List<JellyPiece> matchedPieces)
    {
        for (int i = 0; i < matchedPieces.Count; i++)
        {
            JellyPiece piece = matchedPieces[i];
            if (piece == null) continue;
            piece.PlayPreCollectPulse();
        }
    }

    private IEnumerator RemoveOnlyMatchedColorRoutine(List<JellyPiece> matchedPieces, JellyColor matchedColor)
    {
        List<JellyPiece> emptiedPieces = new List<JellyPiece>();
        List<JellyPiece> survivedPieces = new List<JellyPiece>();

        for (int i = 0; i < matchedPieces.Count; i++)
        {
            JellyPiece piece = matchedPieces[i];
            if (piece == null) continue;

            int removedCount = piece.RemoveColor(matchedColor);

            if (removedCount > 0)
                goalSystem.CollectRemovedColor(matchedColor, removedCount);

            if (piece.IsEmptyCompletely())
                emptiedPieces.Add(piece);
            else
                survivedPieces.Add(piece);
        }

        for (int i = 0; i < survivedPieces.Count; i++)
        {
            if (survivedPieces[i] != null)
                survivedPieces[i].PlayPreCollectPulse();
        }

        if (emptiedPieces.Count > 0)
        {
            Vector3 collectCenter = GetCollectCenter(emptiedPieces);
            yield return StartCoroutine(PlayCollectAndRemoveRoutine(emptiedPieces, collectCenter));
        }
    }

    private IEnumerator PlayCollectAndRemoveRoutine(List<JellyPiece> piecesToRemove, Vector3 collectCenter)
    {
        int completedCount = 0;
        int total = 0;

        for (int i = 0; i < piecesToRemove.Count; i++)
        {
            if (piecesToRemove[i] != null)
                total++;
        }

        if (total == 0)
            yield break;

        for (int i = 0; i < piecesToRemove.Count; i++)
        {
            JellyPiece piece = piecesToRemove[i];
            if (piece == null) continue;

            Vector2Int coord = piece.CurrentCoord;
            board.RemovePiece(coord);

            piece.PlayCollectToPoint(collectCenter, () =>
            {
                if (piece != null)
                {
                    SpawnPopEffect(piece.transform.position, piece);
                    Destroy(piece.gameObject);
                }

                completedCount++;
            });
        }

        while (completedCount < total)
            yield return null;
    }

    private Vector3 GetCollectCenter(List<JellyPiece> pieces)
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        for (int i = 0; i < pieces.Count; i++)
        {
            JellyPiece piece = pieces[i];
            if (piece == null) continue;

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

        JellyPopEffect fx = Instantiate(jellyPopEffectPrefab, position, Quaternion.identity);
        fx.Play(GetColorFromJelly(piece));
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
        }

        return Color.white;
    }

    private void HandleWin()
    {
        if (IsGameEnded) return;

        IsGameEnded = true;

        if (levelData != null)
            coins += levelData.winCoinReward;

        Debug.Log("WIN - Coins: " + coins);
    }
}