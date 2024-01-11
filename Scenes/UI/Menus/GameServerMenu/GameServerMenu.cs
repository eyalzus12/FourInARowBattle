using Godot;
using System;

namespace FourInARowBattle;

public partial class GameServerMenu : Node
{
    [ExportCategory("Nodes")]
    [Export]
    private GameServer Server = null!;
    [Export]
    private LineEdit Port = null!;
    [Export]
    private Button StartServer = null!;
    [Export]
    private Button StopServer = null!;
    [Export]
    private CheckButton RefuseNewConnections = null!;
    [Export]
    private AcceptDialog ErrorPopup = null!;
    [Export]
    private GoBackButton GoBack = null!;
    [Export]
    private ConfirmationDialog GoBackConfirmationDialog = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(Server);
        ArgumentNullException.ThrowIfNull(Port);
        ArgumentNullException.ThrowIfNull(StartServer);
        ArgumentNullException.ThrowIfNull(StopServer);
        ArgumentNullException.ThrowIfNull(RefuseNewConnections);
        ArgumentNullException.ThrowIfNull(ErrorPopup);
        ArgumentNullException.ThrowIfNull(GoBack);
        ArgumentNullException.ThrowIfNull(GoBackConfirmationDialog);
    }

    private void ConnectSignals()
    {
        StartServer.Pressed += OnStartServerPressed;
        StopServer.Pressed += OnStopServerPressed;
        RefuseNewConnections.Pressed += OnRefuseNewConnectionsPressed;
        GoBack.ChangeSceneRequested += OnGoBackButtonChangeSceneRequested;
        GoBackConfirmationDialog.Confirmed += OnGoBackConfirmationDialogConfirmed;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnStartServerPressed()
    {
        if(ushort.TryParse(Port.Text, out ushort port))
        {
            Error err = Server.Listen(port);
            if(err != Error.Ok)
            {
                DisplayError($"Error when trying to listen on port: {err}");
                return;
            }

            StartServer.Disabled = true;
            StopServer.Disabled = false;
            Port.Editable = false;
            RefuseNewConnections.Disabled = false;
            RefuseNewConnections.SetPressedNoSignal(false);
        }
        else
        {
            DisplayError("Invalid port");
        }
    }

    private void OnStopServerPressed()
    {
        Server.Stop();
        StartServer.Disabled = false;
        StopServer.Disabled = true;
        Port.Editable = true;
        RefuseNewConnections.Disabled = true;
        RefuseNewConnections.SetPressedNoSignal(false);
    }

    private void OnRefuseNewConnectionsPressed()
    {
        Server.RefuseNewConnections = RefuseNewConnections.ButtonPressed;
    }

    private string? _goBackRequestPath;

    private void OnGoBackButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _goBackRequestPath = path;
        GoBackConfirmationDialog.PopupCentered();
    }

    private void OnGoBackConfirmationDialogConfirmed()
    {
        Server.Stop();
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, _goBackRequestPath!);
    }

    private void DisplayError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if(!ErrorPopup.Visible)
        {
            ErrorPopup.DialogText = error;
            ErrorPopup.PopupCentered();
        }
    }
}
