using System;
using UnityEngine;

[Serializable]
public class JellySubCell
{
    public string id;
    public JellyColor color;

    [NonSerialized] public JellySlot slot;
    [NonSerialized] public Rect localRect;

    public bool HasValidRuntimeLayout =>
        slot != JellySlot.None &&
        localRect.width > 0f &&
        localRect.height > 0f;

    public JellySubCell(JellyColor color)
    {
        id = Guid.NewGuid().ToString();
        this.color = color;
        slot = JellySlot.None;
        localRect = default;
    }

    public JellySubCell(string id, JellyColor color)
    {
        this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
        this.color = color;
        slot = JellySlot.None;
        localRect = default;
    }

    public void SetRuntimeLayout(JellySlot slot, Rect localRect)
    {
        this.slot = slot;
        this.localRect = localRect;
    }

    public void ClearRuntimeLayout()
    {
        slot = JellySlot.None;
        localRect = default;
    }
}