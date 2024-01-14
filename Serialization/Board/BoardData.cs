using Godot;
using Godot.Collections;

namespace FourInARowBattle;

/// <summary>
/// Board data resource
/// </summary>
[GlobalClass]
public partial class BoardData : Resource
{
    /// <summary>
    /// How many rows the board has
    /// </summary>
    [Export]
    public int Rows{get; set;}
    /// <summary>
    /// How many columns the board has
    /// </summary>
    [Export]
    public int Columns{get; set;}
    /// <summary>
    /// The token streak required
    /// </summary>
    [Export]
    public int WinRequirement{get; set;}
    /// <summary>
    /// The token grid
    /// </summary>
    [Export]
    public Array<Array<TokenData?>> Grid{get; set;} = new();
    /// <summary>
    /// The board position
    /// </summary>
    [Export]
    public Vector2 BoardPosition{get; set;}
    /// <summary>
    /// The board size
    /// </summary>
    [Export]
    public Vector2 BoardSize{get; set;}
}
