using Godot;

namespace FourInARowBattle;

public partial class EventBus : Node
{
    //a signal to be used for turn changes
    [Signal]
    public delegate void TurnChangedEventHandler(GameTurnEnum to, bool isStartupSignal=false);
    //a signal to be used for when a token selection button is pressed
    [Signal]
    public delegate void TokenSelectedEventHandler(TokenCounterControl what, TokenCounterButton who);
    //a signal to be used for when a player's score increases
    [Signal]
    public delegate void ScoreIncreasedEventHandler(GameTurnEnum who, int amount);

    [Signal]
    public delegate void TokenButtonHoveredEventHandler(GameTurnEnum turn, string description);
    [Signal]
    public delegate void TokenButtonStoppedHoverEventHandler(GameTurnEnum turn, string description);
}
