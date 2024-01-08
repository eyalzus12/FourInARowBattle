using Godot;
using System;

namespace FourInARowBattle;

public partial class LocalPlayMenu : Control
{
    [Signal]
    public delegate void CreateNewGameRequestedEventHandler(string path);
    [Signal]
    public delegate void LoadGameRequestedEventHandler(string path);
    [Signal]
    public delegate void GoBackRequestedEventHandler(string path);

    [Export]
    public ChangeSceneOnPressButton? CreateNewGame{get; set;}
    [Export]
    public ChangeSceneAndLoadGameButton? LoadGame{get; set;}
    [Export]
    public GoBackButton? GoBack{get; set;}

    public override void _Ready()
    {
        if(CreateNewGame is not null)
        {
            CreateNewGame.ChangeSceneRequested += (string path) =>
            {
                EmitSignal(SignalName.CreateNewGameRequested, path);
                GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
            };
        }

        if(LoadGame is not null)
        {
            LoadGame.ChangeSceneRequested += (string path) =>
            {
                EmitSignal(SignalName.LoadGameRequested, path);
                GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
            };
        }

        if(GoBack is not null)
        {
            GoBack.ChangeSceneRequested += (string path) =>
            {
                EmitSignal(SignalName.GoBackRequested, path);
                GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
            };
        }
    }
}
