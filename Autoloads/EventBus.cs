using Godot;
using System;

public partial class EventBus : Node
{
    [Signal]
    public delegate void ExternalPassTurnEventHandler();
    [Signal]
    public delegate void TurnChangedEventHandler(GameTurnEnum to);
    [Signal]
    public delegate void TokenSelectedEventHandler(TokenCounterControl what);
    [Signal]
    public delegate void ScoreIncreasedEventHandler(GameTurnEnum who, int amount);
}
