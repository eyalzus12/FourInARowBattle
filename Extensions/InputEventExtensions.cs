using Godot;

namespace FourInARowBattle;

/// <summary>
/// Input event extensions
/// </summary>
public static class InputEventExtensions
{
    /// <summary>
    /// Helper method to check if an input event is a transition from non-pressing to pressing
    /// </summary>
    /// <param name="@event">The input event</param>
    /// <returns>Whether the input event is a just pressed event</returns>
    public static bool IsJustPressed(this InputEvent @event) => @event.IsPressed() && !@event.IsEcho();
}