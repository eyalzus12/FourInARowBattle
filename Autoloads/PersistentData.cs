using Godot;

namespace FourInARowBattle;

/// <summary>
/// Class used to transfer information when changing scene
/// </summary>
public partial class PersistentData : Node
{
    public GameData? ContinueFromState{get; set;}

    public override void _Ready()
    {
        Autoloads.PersistentData = this;
    }
}
