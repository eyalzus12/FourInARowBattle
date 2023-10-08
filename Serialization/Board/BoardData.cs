using Godot;
using System;

public partial class BoardData : Resource
{
    [Export]
    public int Rows{get; set;}
    [Export]
    public int Columns{get; set;}
    [Export]
    public int WinRequirement{get; set;}
    [Export]
    public Godot.Collections.Array<Godot.Collections.Array<TokenData?>> Grid{get; set;} = new();
    [Export]
    public Vector2 BoardPosition{get; set;}
    [Export]
    public Vector2 BoardSize{get; set;}
}
