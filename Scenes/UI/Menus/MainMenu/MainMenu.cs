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

    [Export]
    public ChangeSceneOnPressButton LocalPlayButton{get; set;} = null!;
    [Export]
    public ChangeSceneOnPressButton RemotePlayButton{get; set;} = null!;
    [Export]
    public ChangeSceneOnPressButton HostServerButton{get; set;} = null!;

    private void VerifyExports()
    {
        if(LocalPlayButton is null) { GD.PushError($"No {nameof(LocalPlayButton)} set"); return; }
        if(RemotePlayButton is null) { GD.PushError($"No {nameof(RemotePlayButton)} set"); return; }
        if(HostServerButton is null) { GD.PushError($"No {nameof(HostServerButton)} set"); return; }
    }

    private void ConnectSignals()
    {
        LocalPlayButton.ChangeSceneRequested += OnLocalPlayButtonChangeSceneRequested;
        RemotePlayButton.ChangeSceneRequested += OnRemotePlayButtonChangeSceneRequested;
        HostServerButton.ChangeSceneRequested += OnHostServerButtonChangeSceneRequested;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnLocalPlayButtonChangeSceneRequested(string path)
    {
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    private void OnRemotePlayButtonChangeSceneRequested(string path)
    {
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    private void OnHostServerButtonChangeSceneRequested(string path)
    {
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }
}
