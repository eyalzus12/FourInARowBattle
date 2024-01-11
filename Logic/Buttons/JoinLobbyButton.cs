using Godot;
using System;

namespace FourInARowBattle;

public partial class JoinLobbyButton : Button
{
    [Signal]
    public delegate void JoinLobbyButtonPressedEventHandler(uint with);
    [Signal]
    public delegate void LobbyNumberWasInvalidEventHandler();

    [ExportCategory("Nodes")]
    [Export]
    private LobbyIdField Field = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(Field);
    }

    public override void _Ready()
    {
        VerifyExports();
    }

    public override void _Pressed()
    {
        if(uint.TryParse(Field.Text, out uint result))
            EmitSignal(SignalName.JoinLobbyButtonPressed, result);
        else
        {
            EmitSignal(SignalName.LobbyNumberWasInvalid);
        }
    }
}
