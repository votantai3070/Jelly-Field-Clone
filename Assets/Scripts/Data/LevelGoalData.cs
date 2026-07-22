using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelGoalData", menuName = "JellyField/Level Goal Data")]
public class LevelGoalData : ScriptableObject
{
    public int width = 6;
    public int height = 8;
    public int winCoinReward = 50;
    public List<ColorGoalEntry> goals = new List<ColorGoalEntry>();
}

[Serializable]
public class ColorGoalEntry
{
    public JellyColor color;
    public int required;
}