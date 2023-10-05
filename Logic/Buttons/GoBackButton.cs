using Godot;
using System;

public partial class GoBackButton : ChangeSceneOnPressButton
{
    //switch to the selected scene when the back mouse button is pressed
    public override void _UnhandledInput(InputEvent @event)
    {
        if(@event is InputEventMouseButton mb &&
            mb.ButtonIndex == MouseButton.Xbutton2 &&
            mb.Pressed)
        {
            GetViewport().SetInputAsHandled();
            _Pressed();
        }
    }
}
