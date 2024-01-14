using Godot;

namespace FourInARowBattle;

/// <summary>
/// A scene change button that also treats ESC as a press.
/// </summary>
public partial class GoBackButton : ChangeSceneOnPressButton
{
    public override void _UnhandledInput(InputEvent @event)
    {
        if( @event.IsJustPressed() &&
            @event is InputEventKey ke &&
            ke.PhysicalKeycode == Key.Escape)
        {
            GetViewport().SetInputAsHandled();
            _Pressed();
        }
    }
}
