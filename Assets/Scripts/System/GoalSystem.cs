using System;
using System.Collections.Generic;
using UnityEngine;

public class GoalSystem : MonoBehaviour
{
    public event Action OnWin;
    public Action<Dictionary<JellyColor, int>> OnCollectedChanged;

    [SerializeField] private LevelGoalData currentLevel;

    private readonly Dictionary<JellyColor, int> collected = new Dictionary<JellyColor, int>();

    public bool IsWin { get; private set; }

    public void Initialize(LevelGoalData levelData)
    {
        currentLevel = levelData;
        collected.Clear();
        IsWin = false;

        if (currentLevel == null)
        {
            Debug.LogError("GoalSystem Initialize failed: levelData is null");
            return;
        }

        CreateCollectedEntries();
    }

    public void CollectRemovedColor(JellyColor color, int removedCount)
    {
        if (removedCount <= 0)
            return;

        if (IsWin)
            return;

        if (currentLevel == null)
            return;

        if (!IsGoalColor(color))
            return;

        AddCollectedAmount(color, removedCount);

        OnCollectedChanged?.Invoke(collected);
        CheckWinCondition();
    }

    public int GetCollected(JellyColor color)
    {
        if (collected.TryGetValue(color, out int value))
            return value;

        return 0;
    }

    public int GetRequired(JellyColor color)
    {
        if (currentLevel == null)
            return 0;

        for (int i = 0; i < currentLevel.goals.Count; i++)
        {
            if (currentLevel.goals[i].color == color)
                return currentLevel.goals[i].required;
        }

        return 0;
    }

    public void CheckWinCondition()
    {
        if (currentLevel == null)
            return;

        if (IsWin)
            return;

        for (int i = 0; i < currentLevel.goals.Count; i++)
        {
            var goal = currentLevel.goals[i];
            int collectedAmount = GetCollected(goal.color);

            if (collectedAmount < goal.required)
                return;
        }

        IsWin = true;

        if (UI.Instance != null && UI.Instance.winPanel != null)
            UI.Instance.winPanel.SetActive(true);

        OnWin?.Invoke();
    }

    private void CreateCollectedEntries()
    {
        for (int i = 0; i < currentLevel.goals.Count; i++)
        {
            JellyColor color = currentLevel.goals[i].color;

            if (!collected.ContainsKey(color))
                collected.Add(color, 0);
        }
    }

    private bool IsGoalColor(JellyColor color)
    {
        if (currentLevel == null)
            return false;

        for (int i = 0; i < currentLevel.goals.Count; i++)
        {
            if (currentLevel.goals[i].color == color)
                return true;
        }

        return false;
    }

    private void AddCollectedAmount(JellyColor color, int amount)
    {
        if (!collected.ContainsKey(color))
            collected[color] = 0;

        collected[color] += amount;
    }
}