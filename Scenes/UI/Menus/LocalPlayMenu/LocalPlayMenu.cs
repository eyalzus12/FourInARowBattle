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

    [ExportCategory("Nodes")]
    [Export]
    private ChangeSceneOnPressButton CreateNewGame = null!;
    [Export]
    private ChangeSceneAndLoadGameButton LoadGame = null!;
    [Export]
    private GoBackButton GoBack = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(CreateNewGame);
        ArgumentNullException.ThrowIfNull(LoadGame);
        ArgumentNullException.ThrowIfNull(GoBack);
    }

    private void ConnectSignals()
    {
        CreateNewGame.ChangeSceneRequested += OnCreateNewGameChangeSceneRequested;
        LoadGame.ChangeSceneRequested += OnLoadGameChangeSceneRequested;
        GoBack.ChangeSceneRequested += OnGoBackChangeSceneRequested;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnCreateNewGameChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.CreateNewGameRequested, path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    private void OnLoadGameChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.LoadGameRequested, path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    private void OnGoBackChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.GoBackRequested, path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }
}
