using System.Collections.Generic;
using UnityEngine;

public class UI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GoalSystem goalSystem;

    private GoalObjectiveUI goalObjective;

    private void Awake()
    {
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

    private void SetupGoalObjective(LevelGoalData levelGoal)
    {
        if (goalObjective == null)
            return;

        goalObjective.SetGoalData(levelGoal);
    }

    private void UpdateGoalObjective(Dictionary<JellyColor, int> jellyCollected)
    {
        if (goalObjective == null)
            return;

        goalObjective.UpdateGoalSlotUI(jellyCollected);
    }
}