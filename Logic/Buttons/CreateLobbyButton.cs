using Godot;
using System;

namespace FourInARowBattle;

public partial class CreateLobbyButton : Button
{
    [Signal]
    public delegate void CreateLobbyButtonPressedEventHandler();

    public override void _Pressed()
    {
        EmitSignal(SignalName.CreateLobbyButtonPressed);
    }
}
