using Godot;
using Godot.Collections;

namespace FourInARowBattle;

[GlobalClass]
public partial class GameData : Resource
{
    [Export]
    public GameTurnEnum Turn{get; set;}
    [Export]
    public Array<TokenCounterListData> Players{get; set;} = new();
    [Export]
    public BoardData Board{get; set;} = null!;
}
