using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class TrayModel
{
    public int Id;
    public List<CardGroup> Composition;
    public List<int> BlockedByTrayIds;
    public TrayState State = TrayState.Filled;
    public CardColor CardColor;
    public bool IsLocked;

    public Vector3 Position;
    public Vector3 Rotation; // euler angles (degrees)

    // Remaining cards on the tray; counted down as cards are distributed.
    // Not stored in JSON, so RecalculateTotalCardCount() must seed it after load.
    public int TotalCardCount;

    public void RecalculateTotalCardCount()
    {
        TotalCardCount = Composition.Sum(g => g.Count);
    }
}

[Serializable]
public struct CardGroup
{
    public CardColor Color;
    public int Count;
}

public enum TrayState
{
    Filled,
    Empty,
    IsUsed
}