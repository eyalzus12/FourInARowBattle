using Godot;
using System;

namespace FourInARowBattle;

public partial class RemotePlayMenu : Control
{
    [Signal]
    public delegate void CreateLobbyRequestedEventHandler(string playerName);
    [Signal]
    public delegate void JoinLobbyRequestedEventHandler(uint lobbyId, string playerName);
    [Signal]
    public delegate void GoBackRequestedEventHandler(string path);

    [Export]
    public CreateLobbyButton? CreateLobby{get; set;}
    [Export]
    public JoinLobbyButton? JoinLobby{get; set;}
    [Export]
    public GoBackButton? GoBack{get; set;}
    [Export]
    public LineEdit? PlayerNameField{get; set;}

    public override void _Ready()
    {
        if(CreateLobby is not null)
        {
            CreateLobby.CreateLobbyButtonPressed += () => EmitSignal(SignalName.CreateLobbyRequested, PlayerNameField?.Text ?? "");
        }

        if(JoinLobby is not null)
        {
            JoinLobby.JoinLobbyButtonPressed += (uint lobbyId) => EmitSignal(SignalName.JoinLobbyRequested, lobbyId, PlayerNameField?.Text ?? "");
        }

        if(GoBack is not null)
        {
            GoBack.ChangeSceneRequested += (string path) => EmitSignal(SignalName.GoBackRequested, path);
        }
    }
}
