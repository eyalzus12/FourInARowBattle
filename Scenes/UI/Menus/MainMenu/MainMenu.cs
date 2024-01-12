using Godot;
using System;

namespace FourInARowBattle;

public partial class MainMenu : Control
{
    [Signal]
    public delegate void LocalPlayRequestedEventHandler(string path);
    [Signal]
    public delegate void RemotePlayRequestedEventHandler(string path);
    [Signal]
    public delegate void HostServerRequestedEventHandler(string path);

    [ExportCategory("Nodes")]
    [Export]
    private ChangeSceneOnPressButton _localPlayButton = null!;
    [Export]
    private ChangeSceneOnPressButton _remotePlayButton = null!;
    [Export]
    private ChangeSceneOnPressButton _hostServerButton = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_localPlayButton);
        ArgumentNullException.ThrowIfNull(_remotePlayButton);
        ArgumentNullException.ThrowIfNull(_hostServerButton);
    }

    private void ConnectSignals()
    {
        _localPlayButton.ChangeSceneRequested += OnLocalPlayButtonChangeSceneRequested;
        _remotePlayButton.ChangeSceneRequested += OnRemotePlayButtonChangeSceneRequested;
        _hostServerButton.ChangeSceneRequested += OnHostServerButtonChangeSceneRequested;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnLocalPlayButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    private void OnRemotePlayButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    private void OnHostServerButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }
}
