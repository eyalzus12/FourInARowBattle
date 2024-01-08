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
    public ChangeSceneOnPressButton? LocalPlayButton{get; set;}
    [Export]
    public ChangeSceneOnPressButton? RemotePlayButton{get; set;}
    [Export]
    public ChangeSceneOnPressButton? HostServerButton{get; set;}

    public override void _Ready()
    {
        if(LocalPlayButton is not null)
        {
            LocalPlayButton.ChangeSceneRequested += (string path) =>
            {
                GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
            };
        }

        if(RemotePlayButton is not null)
        {
            RemotePlayButton.ChangeSceneRequested += (string path) =>
            {
                GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
            };
        }

        if(HostServerButton is not null)
        {
            HostServerButton.ChangeSceneRequested += (string path) =>
            {
                GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
            };
        }
    }
}
