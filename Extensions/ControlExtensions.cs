using Godot;

namespace FourInARowBattle;

public static class ControlExtensions
{
    public static void CenterOn(this Control c, Vector2 at) => c.GlobalPosition = at - c.GetGlobalRect().Size/2;
}