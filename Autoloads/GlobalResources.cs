using Godot;

namespace FourInARowBattle;

/// <summary>
/// Class to hold resources used globally, to avoid re-loading them multiple times.
/// </summary>
public partial class GlobalResources : Node
{
    public AudioStream TOKEN_LAND_SOUND{get; private set;} = null!;

    public override void _Ready()
    {
        Autoloads.GlobalResources = this;

        TOKEN_LAND_SOUND = ResourceLoader.Load<AudioStream>("res://Resources/Audio/TokenLand.ogg");
    }
}
