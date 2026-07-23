using System.Collections.Generic;
using UnityEngine;

public class GoalObjectiveUI : MonoBehaviour
{
    private LevelGoalData goalData;
    private GoalSlot[] goalSlots;

    private void Awake()
    {
        goalSlots = GetComponentsInChildren<GoalSlot>(true);
        DisableAllGoalSlots();
    }

    public void SetGoalData(LevelGoalData data)
    {
        goalData = data;
        RefreshUI();
    }

    private void RefreshUI()
    {
        DisableAllGoalSlots();

        if (goalData == null || goalData.goals == null)
            return;

        int count = Mathf.Min(goalData.goals.Count, goalSlots.Length);

        for (int i = 0; i < count; i++)
        {
            ColorGoalEntry goal = goalData.goals[i];
            if (goal == null)
                continue;

            goalSlots[i].gameObject.SetActive(true);
            goalSlots[i].Setup(goal);
        }
    }

    private void DisableAllGoalSlots()
    {
        if (goalSlots == null)
            return;

        for (int i = 0; i < goalSlots.Length; i++)
        {
            goalSlots[i].gameObject.SetActive(false);
        }
    }

    public void UpdateGoalSlotUI(Dictionary<JellyColor, int> jellyCollected)
    {
        if (goalData == null || goalData.goals == null || jellyCollected == null)
            return;

        int count = Mathf.Min(goalData.goals.Count, goalSlots.Length);

        for (int i = 0; i < count; i++)
        {
            if (!goalSlots[i].gameObject.activeSelf)
                continue;

            goalSlots[i].UpdateSlotUI(jellyCollected);
        }
    }
}