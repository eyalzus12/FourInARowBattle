using Godot;
using System;

namespace FourInARowBattle;

/// <summary>
/// The menu for hosting a server. Mainly serves as a wrapper over GameServer.
/// </summary>
public partial class GameServerMenu : Node
{
    [ExportCategory("Nodes")]
    [Export]
    private GameServer _server = null!;
    [Export]
    private LineEdit _port = null!;
    [Export]
    private Button _startServerButton = null!;
    [Export]
    private Button _stopServerButton = null!;
    [Export]
    private CheckButton _refuseNewConnectionsCheckButton = null!;
    [Export]
    private AcceptDialog _errorPopup = null!;
    [Export]
    private GoBackButton _goBackButton = null!;
    [Export]
    private ConfirmationDialog _goBackConfirmationDialog = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_server);
        ArgumentNullException.ThrowIfNull(_port);
        ArgumentNullException.ThrowIfNull(_startServerButton);
        ArgumentNullException.ThrowIfNull(_stopServerButton);
        ArgumentNullException.ThrowIfNull(_refuseNewConnectionsCheckButton);
        ArgumentNullException.ThrowIfNull(_errorPopup);
        ArgumentNullException.ThrowIfNull(_goBackButton);
        ArgumentNullException.ThrowIfNull(_goBackConfirmationDialog);
    }

    private void ConnectSignals()
    {
        _startServerButton.Pressed += OnStartServerPressed;
        _stopServerButton.Pressed += OnStopServerPressed;
        _refuseNewConnectionsCheckButton.Pressed += OnRefuseNewConnectionsCheckButtonPressed;
        _goBackButton.ChangeSceneRequested += OnGoBackButtonChangeSceneRequested;
        _goBackConfirmationDialog.Confirmed += OnGoBackConfirmationDialogConfirmed;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();

        //--server is specified. simulate a press to host the server
        if(Autoloads.Startup.UserCmdlineArgs.TryGetValue(Globals.CMD_LINE_SERVER_KEY, out string? port))
        {
            _port.Text = port;
            _startServerButton.EmitSignal(BaseButton.SignalName.Pressed);
        }
    }

    /// <summary>
    /// Event: The start server button was pressed
    /// </summary>
    private void OnStartServerPressed()
    {
        if(ushort.TryParse(_port.Text, out ushort port))
        {
            Error err = _server.Listen(port);
            if(err != Error.Ok)
            {
                DisplayError($"Error when trying to listen on port {port}: {err}");
                return;
            }

            GD.Print($"Listening on port {port}");
            _startServerButton.Disabled = true;
            _stopServerButton.Disabled = false;
            _port.Editable = false;
            _refuseNewConnectionsCheckButton.Disabled = false;
            _refuseNewConnectionsCheckButton.SetPressedNoSignal(false);
        }
        else
        {
            GD.Print($"Error when trying to listen on port {_port.Text}: Invalid port");
            DisplayError("Invalid port");
        }
    }

    /// <summary>
    /// Event: The stop server button was pressed
    /// </summary>
    private void OnStopServerPressed()
    {
        _server.Stop();
        _startServerButton.Disabled = false;
        _stopServerButton.Disabled = true;
        _port.Editable = true;
        _refuseNewConnectionsCheckButton.Disabled = true;
        _refuseNewConnectionsCheckButton.SetPressedNoSignal(false);
    }

    /// <summary>
    /// Event: The refuse new connections toggle was toggled.
    /// </summary>
    private void OnRefuseNewConnectionsCheckButtonPressed()
    {
        _server.RefuseNewConnections = _refuseNewConnectionsCheckButton.ButtonPressed;
    }

    private string? _goBackRequestPath;

    /// <summary>
    /// Event: The exit button was pressed. Show confirmation dialog.
    /// </summary>
    /// <param name="path">The path to the main menu scene</param>
    private void OnGoBackButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _goBackRequestPath = path;
        _goBackConfirmationDialog.PopupCentered();
    }

    /// <summary>
    /// Event: Going back to main menu was confirmed.
    /// </summary>
    private void OnGoBackConfirmationDialogConfirmed()
    {
        _server.Stop();
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, _goBackRequestPath!);
    }

    /// <summary>
    /// Display an error to the screen.
    /// </summary>
    /// <param name="error">The error to display</param>
    private void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if(!_errorPopup.Visible)
        {
            _errorPopup.DialogText = error;
            _errorPopup.PopupCentered();
        }
    }
}
