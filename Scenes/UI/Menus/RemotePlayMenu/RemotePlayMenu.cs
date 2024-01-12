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
    private CreateLobbyButton _createLobbyButton = null!;
    [Export]
    private JoinLobbyButton _joinLobbyButton = null!;
    [Export]
    private GoBackButton _goBackButton = null!;
    [Export]
    private LineEdit _playerNameField = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_createLobbyButton);
        ArgumentNullException.ThrowIfNull(_joinLobbyButton);
        ArgumentNullException.ThrowIfNull(_goBackButton);
        ArgumentNullException.ThrowIfNull(_playerNameField);
    }

    private void ConnectSignals()
    {
        _createLobbyButton.CreateLobbyButtonPressed += OnCreateLobbyButtonCreateLobbyButtonPressed;
        _joinLobbyButton.JoinLobbyButtonPressed += OnJoinLobbyButtonJoinLobbyButtonPressed;
        _joinLobbyButton.LobbyNumberWasInvalid += OnJoinLobbyButtonLobbyNumberWasInvalid;
        _goBackButton.ChangeSceneRequested += OnGoBackButtonChangeSceneRequested;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
        _playerNameField.MaxLength = Globals.NAME_LENGTH_LIMIT;
    }

    private void OnCreateLobbyButtonCreateLobbyButtonPressed()
    {
        EmitSignal(SignalName.CreateLobbyRequested, _playerNameField?.Text ?? "");
    }

    private void OnJoinLobbyButtonJoinLobbyButtonPressed(uint lobbyId)
    {
        EmitSignal(SignalName.JoinLobbyRequested, lobbyId, _playerNameField?.Text ?? "");
    }

    private void OnJoinLobbyButtonLobbyNumberWasInvalid()
    {
        EmitSignal(SignalName.LobbyNumberWasInvalid);
    }

    private void OnGoBackButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.GoBackRequested, path);
    }
}
