using Godot;

namespace FourInARowBattle;

public partial class TokenCounterButton : Button
{
    [Export]
    public PackedScene AssociatedScene{get; set;} = null!;
}
