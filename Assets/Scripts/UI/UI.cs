using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI : MonoBehaviour
{
    public static UI Instance { get; private set; }

    [SerializeField] private GameManager gameManager;
    [SerializeField] private GoalSystem goalSystem;
    [SerializeField] private LevelManager levelManager;

    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject playPanel;
    public GameObject winPanel;

    private GoalObjectiveUI goalObjective;

    private void Awake()
    {
        Instance = this;
        goalObjective = GetComponentInChildren<GoalObjectiveUI>(true);
    }

    private void OnEnable()
    {
        if (gameManager != null)
            gameManager.OnGoalObjectiveUIChanged += SetupGoalObjective;

        if (goalSystem != null)
            goalSystem.OnCollectedChanged += UpdateGoalObjective;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnGoalObjectiveUIChanged -= SetupGoalObjective;

        if (goalSystem != null)
            goalSystem.OnCollectedChanged -= UpdateGoalObjective;
    }

    public void PlayBtn()
    {
        levelManager.Play();
        playPanel.SetActive(false);
    }

    public void NextBtn()
    {
        levelManager.Next();
        winPanel.SetActive(false);
    }

    public void Retry()
    {

    }

    private void SetupGoalObjective(LevelGoalData levelGoal)
    {
        if (goalObjective == null)
            return;

        goalObjective.SetGoalData(levelGoal);
        levelText.text = $"Level {levelGoal.level}";
    }

    private void UpdateGoalObjective(Dictionary<JellyColor, int> jellyCollected)
    {
        if (goalObjective == null)
            return;

        goalObjective.UpdateGoalSlotUI(jellyCollected);
    }
}