using Godot;
using System;

public partial class TokenCounterListData : Resource
{
    [Export]
    public int Score{get; set;}
    [Export]
    public bool RefillLocked{get; set;}
    [Export]
    public bool RefillUnlockedNextTurn{get; set;}
    [Export]
    public Godot.Collections.Array<TokenCounterData> Counters{get; set;} = new();
}
