using System;
using UnityEngine;

[Serializable]
public struct LayoutSlotData
{
    public JellySlot slot;
    public Rect rect;

    public LayoutSlotData(JellySlot slot, Rect rect)
    {
        this.slot = slot;
        this.rect = rect;
    }
}
