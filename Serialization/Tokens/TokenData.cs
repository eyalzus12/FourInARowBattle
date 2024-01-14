using Godot;

namespace FourInARowBattle;

/// <summary>
/// Token data resource
/// </summary>
[GlobalClass]
public partial class TokenData : Resource
{
    /// <summary>
    /// The token scene path
    /// </summary>
    [Export]
    public string TokenScenePath{get; set;} = "";
    /// <summary>
    /// The token color
    /// </summary>
    [Export]
    public Color TokenColor{get; set;}
    /// <summary>
    /// The modulate of the token
    /// </summary>
    [Export]
    public Color TokenModulate{get; set;}
    /// <summary>
    /// The token's global position
    /// </summary>
    [Export]
    public Vector2 GlobalPosition{get; set;}
    /// <summary>
    /// The token's desired position. It will be Vector2(NaN, NaN) if the desired position is null.
    /// </summary>
    [Export]
    public Vector2 DesiredPosition{get; set;}
}
