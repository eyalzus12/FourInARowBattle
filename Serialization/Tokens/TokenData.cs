using Godot;

namespace FourInARowBattle;

public partial class TokenData : Resource
{
    [Export]
    public string TokenScenePath{get; set;} = "";
    [Export]
    public Color TokenColor{get; set;}
    [Export]
    public Color TokenModulate{get; set;}
    [Export]
    public Vector2 GlobalPosition{get; set;}
    [Export]
    public Vector2 DesiredPosition{get; set;}
}
