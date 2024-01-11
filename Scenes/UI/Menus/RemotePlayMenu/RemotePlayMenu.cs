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
    public delegate void LobbyNumberWasInvalidEventHandler();
    [Signal]
    public delegate void GoBackRequestedEventHandler(string path);

    [ExportCategory("Nodes")]
    [Export]
    private CreateLobbyButton CreateLobby = null!;
    [Export]
    private JoinLobbyButton JoinLobby = null!;
    [Export]
    private GoBackButton GoBack = null!;
    [Export]
    private LineEdit PlayerNameField = null!;

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
        JoinLobby.LobbyNumberWasInvalid += OnJoinLobbyLobbyNumberWasInvalid;
        GoBack.ChangeSceneRequested += OnGoBackChangeSceneRequested;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
        PlayerNameField.MaxLength = Globals.NAME_LENGTH_LIMIT;
    }

    private void OnCreateLobbyCreateLobbyButtonPressed()
    {
        EmitSignal(SignalName.CreateLobbyRequested, PlayerNameField?.Text ?? "");
    }

    private void OnJoinLobbyJoinLobbyButtonPressed(uint lobbyId)
    {
        EmitSignal(SignalName.JoinLobbyRequested, lobbyId, PlayerNameField?.Text ?? "");
    }

    private void OnJoinLobbyLobbyNumberWasInvalid()
    {
        EmitSignal(SignalName.LobbyNumberWasInvalid);
    }

    private void OnGoBackChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.GoBackRequested, path);
    }
}
