using Godot;

namespace FourInARowBattle;

public partial class GoBackButton : ChangeSceneOnPressButton
{
    //switch to the selected scene when the back mouse button is pressed
    public override void _UnhandledInput(InputEvent @event)
    {
        if(@event is InputEventMouseButton mb &&
            mb.ButtonIndex == MouseButton.Xbutton1 &&
            mb.Pressed)
        {
            GetViewport().SetInputAsHandled();
            _Pressed();
        }
    }
}
