using Godot;
using System;

namespace FourInARowBattle;

public partial class JoinLobbyButton : Button
{
    [Signal]
    public delegate void JoinLobbyButtonPressedEventHandler(uint with);

    [Export]
    public LobbyIdField? Field{get; set;} = null;

    public override void _Pressed()
    {
        if(Field is null) return;
        if(uint.TryParse(Field.Text, out uint result))
            EmitSignal(SignalName.JoinLobbyButtonPressed, result);
        else
        {
            //invalid lobby number
        }
    }
}
