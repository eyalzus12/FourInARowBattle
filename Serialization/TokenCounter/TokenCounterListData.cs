using Godot;
using Godot.Collections;

namespace FourInARowBattle;

[GlobalClass]
public partial class TokenCounterListData : Resource
{
    [Export]
    public int Score{get; set;}
    [Export]
    public bool RefillLocked{get; set;}
    [Export]
    public bool RefillUnlockedNextTurn{get; set;}
    [Export]
    public Array<TokenCounterData> Counters{get; set;} = new();
}
