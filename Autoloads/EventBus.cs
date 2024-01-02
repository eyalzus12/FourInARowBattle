using Godot;

namespace FourInARowBattle;

public partial class EventBus : Node
{
    public override void _Ready()
    {
        Autoloads.EventBus = this;
    }
}
