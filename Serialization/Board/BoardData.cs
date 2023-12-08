using Godot;
using Godot.Collections;

namespace FourInARowBattle;

public partial class BoardData : Resource
{
    [Export]
    public int Rows{get; set;}
    [Export]
    public int Columns{get; set;}
    [Export]
    public int WinRequirement{get; set;}
    [Export]
    public Array<Array<TokenData?>> Grid{get; set;} = new();
    [Export]
    public Vector2 BoardPosition{get; set;}
    [Export]
    public Vector2 BoardSize{get; set;}
}
