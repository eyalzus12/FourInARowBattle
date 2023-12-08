using Godot;

namespace FourInARowBattle;

public partial class GlobalResources : Node
{
    public AudioStream TEST_LAND{get; private set;} = null!;

    public override void _Ready()
    {
        Autoloads.GlobalResources = this;

        TEST_LAND = ResourceLoader.Load<AudioStream>("res://Resources/Audio/TestLand.ogg");
    }
}
