using Godot;

namespace FourInARowBattle;

public static class InputEventExtensions
{
    public static bool IsJustPressed(this InputEvent @event) => @event.IsPressed() && !@event.IsEcho();
}