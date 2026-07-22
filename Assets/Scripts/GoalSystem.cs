using System;
using System.Collections.Generic;
using UnityEngine;

public class GoalSystem : MonoBehaviour
{
    [SerializeField] private LevelGoalData currentLevel;

    private Dictionary<JellyColor, int> collected = new Dictionary<JellyColor, int>();

    public bool IsWin { get; private set; }

    public event Action OnWin;

    public void Initialize(LevelGoalData levelData)
    {
        currentLevel = levelData;
        collected.Clear();
        IsWin = false;

        if (currentLevel == null)
        {
            Debug.LogError("GoalSystem Initialize failed: currentLevel is null");
            return;
        }

        for (int i = 0; i < currentLevel.goals.Count; i++)
        {
            JellyColor color = currentLevel.goals[i].color;
            if (!collected.ContainsKey(color))
                collected.Add(color, 0);
        }
    }

    public void CollectIfGoalColor(JellyColor color, int amount)
    {
        if (amount <= 0) return;
        if (!IsGoalColor(color)) return;
        if (IsWin) return;

        if (!collected.ContainsKey(color))
            collected[color] = 0;

        collected[color] += amount;
        CheckWinCondition();
    }

    public void CollectRemovedColor(JellyColor color, int removedCount)
    {
        CollectIfGoalColor(color, removedCount);
    }

    public bool IsGoalColor(JellyColor color)
    {
        if (currentLevel == null) return false;

        for (int i = 0; i < currentLevel.goals.Count; i++)
        {
            if (currentLevel.goals[i].color == color)
                return true;
        }

        return false;
    }

    public int GetCollected(JellyColor color)
    {
        return collected.ContainsKey(color) ? collected[color] : 0;
    }

    public int GetRequired(JellyColor color)
    {
        if (currentLevel == null) return 0;

        for (int i = 0; i < currentLevel.goals.Count; i++)
        {
            if (currentLevel.goals[i].color == color)
                return currentLevel.goals[i].required;
        }

        return 0;
    }

    public void CheckWinCondition()
    {
        if (currentLevel == null || IsWin) return;

        for (int i = 0; i < currentLevel.goals.Count; i++)
        {
            ColorGoalEntry goal = currentLevel.goals[i];
            if (GetCollected(goal.color) < goal.required)
                return;
        }

        IsWin = true;
        Debug.Log("GoalSystem: WIN");
        OnWin?.Invoke();
    }
}