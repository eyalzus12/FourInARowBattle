using Godot;

namespace FourInARowBattle;

/// <summary>
/// The token button. Simply holds a scene.
/// </summary>
public partial class TokenCounterButton : Button
{
    /// <summary>
    /// The token scene
    /// </summary>
    [Export]
    public PackedScene AssociatedScene{get; private set;} = null!;
}
