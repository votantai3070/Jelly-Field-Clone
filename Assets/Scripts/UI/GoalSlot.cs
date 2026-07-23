using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalSlot : MonoBehaviour
{
    private ColorGoalEntry goal;

    [SerializeField] private Image colorImage;
    [SerializeField] private TextMeshProUGUI goalText;
    [SerializeField] private Image checkImage;

    public void Setup(ColorGoalEntry colorGoalEntry)
    {
        goal = colorGoalEntry;

        colorImage.color = ToUnityColor(goal.color);
        goalText.text = goal.required.ToString();
        checkImage.gameObject.SetActive(false);
    }

    public void UpdateSlotUI(Dictionary<JellyColor, int> jellyCollected)
    {
        if (goal == null || jellyCollected == null)
            return;

        jellyCollected.TryGetValue(goal.color, out int collectedAmount);

        int remainAmount = Mathf.Max(0, goal.required - collectedAmount);

        colorImage.color = ToUnityColor(goal.color);
        goalText.text = remainAmount.ToString();
        checkImage.gameObject.SetActive(remainAmount <= 0);
    }

    private Color ToUnityColor(JellyColor color)
    {
        switch (color)
        {
            case JellyColor.Red: return new Color(1f, 0.35f, 0.35f);
            case JellyColor.Yellow: return new Color(1f, 0.87f, 0.25f);
            case JellyColor.Blue: return new Color(0.3f, 0.55f, 1f);
            case JellyColor.Green: return new Color(0.35f, 0.9f, 0.45f);
            default: return Color.white;
        }
    }
}