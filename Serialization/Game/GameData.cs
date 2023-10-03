using Godot;
using System;

public partial class GameData : Resource
{
    [Export]
    public GameTurnEnum Turn{get; set;}
    [Export]
    public Godot.Collections.Array<TokenCounterListData> Players{get; set;} = new();
    [Export]
    public BoardData? Board{get; set;}
}
