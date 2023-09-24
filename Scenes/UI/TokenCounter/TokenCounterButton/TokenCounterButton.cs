using Godot;
using System;

public partial class TokenCounterButton : Button
{
    [Export]
    public PackedScene AssociatedScene{get; set;} = null!;
}
