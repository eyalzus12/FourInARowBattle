using Godot;
using Godot.Collections;

namespace FourInARowBattle;

/// <summary>
/// Game data resource
/// </summary>
[GlobalClass]
public partial class GameData : Resource
{
    /// <summary>
    /// The current turn
    /// </summary>
    [Export]
    public GameTurnEnum Turn{get; set;}
    /// <summary>
    /// The player data
    /// </summary>
    [Export]
    public Array<TokenCounterListData> Players{get; set;} = new();
    /// <summary>
    /// The board data
    /// </summary>
    [Export]
    public BoardData Board{get; set;} = null!;
}
