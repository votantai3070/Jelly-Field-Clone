using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private const string CurrentLevelKey = "CURRENT_LEVEL";

    [SerializeField] private LevelGoalData[] levelGoals;
    [SerializeField] private GameManager gameManager;

    public int CurrentLevelIndex { get; private set; }

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
    }

    private void Start()
    {
        CurrentLevelIndex = Mathf.Max(0, PlayerPrefs.GetInt(CurrentLevelKey, 0));
    }

    public void Play()
    {
        CurrentLevelIndex = 0;
        PlayerPrefs.SetInt(CurrentLevelKey, CurrentLevelIndex);
        PlayerPrefs.Save();

        LoadCurrentLevel();
    }

    public void Next()
    {
        if (levelGoals == null || levelGoals.Length == 0)
            return;

        CurrentLevelIndex++;

        if (CurrentLevelIndex >= levelGoals.Length)
            CurrentLevelIndex = 0;

        PlayerPrefs.SetInt(CurrentLevelKey, CurrentLevelIndex);
        PlayerPrefs.Save();

        LoadCurrentLevel();
    }

    public void Retry()
    {
        if (levelGoals == null || levelGoals.Length == 0)
            return;

        PlayerPrefs.SetInt(CurrentLevelKey, CurrentLevelIndex);
        PlayerPrefs.Save();

        LoadCurrentLevel();
    }

    public void LoadCurrentLevel()
    {
        if (gameManager == null)
        {
            Debug.LogError("LevelManager missing GameManager");
            return;
        }

        if (levelGoals == null || levelGoals.Length == 0)
        {
            Debug.LogError("LevelManager has no levelGoals");
            return;
        }

        if (CurrentLevelIndex < 0 || CurrentLevelIndex >= levelGoals.Length)
        {
            Debug.LogError("CurrentLevelIndex out of range: " + CurrentLevelIndex);
            return;
        }

        LevelGoalData data = levelGoals[CurrentLevelIndex];
        gameManager.InitializeLevel(data);
    }

    public LevelGoalData GetCurrentLevelData()
    {
        if (levelGoals == null || levelGoals.Length == 0)
            return null;

        if (CurrentLevelIndex < 0 || CurrentLevelIndex >= levelGoals.Length)
            return null;

        return levelGoals[CurrentLevelIndex];
    }
}