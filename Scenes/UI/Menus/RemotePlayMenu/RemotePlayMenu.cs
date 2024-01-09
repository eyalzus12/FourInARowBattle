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
    public CreateLobbyButton CreateLobby{get; set;} = null!;
    [Export]
    public JoinLobbyButton JoinLobby{get; set;} = null!;
    [Export]
    public GoBackButton GoBack{get; set;} = null!;
    [Export]
    public LineEdit PlayerNameField{get; set;} = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(CreateLobby);
        ArgumentNullException.ThrowIfNull(JoinLobby);
        ArgumentNullException.ThrowIfNull(GoBack);
        ArgumentNullException.ThrowIfNull(PlayerNameField);
    }

    private void ConnectSignals()
    {
        CreateLobby.CreateLobbyButtonPressed += OnCreateLobbyCreateLobbyButtonPressed;
        JoinLobby.JoinLobbyButtonPressed += OnJoinLobbyJoinLobbyButtonPressed;
        GoBack.ChangeSceneRequested += OnGoBackChangeSceneRequested;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnCreateLobbyCreateLobbyButtonPressed()
    {
        EmitSignal(SignalName.CreateLobbyRequested, PlayerNameField?.Text ?? "");
    }

    private void OnJoinLobbyJoinLobbyButtonPressed(uint lobbyId)
    {
        EmitSignal(SignalName.JoinLobbyRequested, lobbyId, PlayerNameField?.Text ?? "");
    }

    private void OnGoBackChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.GoBackRequested, path);
    }
}
