using Godot;
using Godot.Collections;

namespace FourInARowBattle;

/// <summary>
/// Token counter list data resource
/// </summary>
[GlobalClass]
public partial class TokenCounterListData : Resource
{
    /// <summary>
    /// The player's score
    /// </summary>
    [Export]
    public int Score{get; set;}
    /// <summary>
    /// Whether the player has a locked refill button
    /// </summary>
    [Export]
    public bool RefillLocked{get; set;}
    /// <summary>
    /// Whether the player's refill button will be unlocked next turn
    /// </summary>
    [Export]
    public bool RefillUnlockedNextTurn{get; set;}
    /// <summary>
    /// The counters data
    /// </summary>
    [Export]
    public Array<TokenCounterData> Counters{get; set;} = new();
}
