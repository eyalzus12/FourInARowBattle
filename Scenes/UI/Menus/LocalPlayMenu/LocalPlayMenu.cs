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
    private ChangeSceneOnPressButton _createNewGameButton = null!;
    [Export]
    private ChangeSceneAndLoadGameButton _loadGameButton = null!;
    [Export]
    private GoBackButton _goBackButton = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_createNewGameButton);
        ArgumentNullException.ThrowIfNull(_loadGameButton);
        ArgumentNullException.ThrowIfNull(_goBackButton);
    }

    private void ConnectSignals()
    {
        _createNewGameButton.ChangeSceneRequested += OnCreateNewGameButtonChangeSceneRequested;
        _loadGameButton.ChangeSceneRequested += OnLoadGameButtonChangeSceneRequested;
        _goBackButton.ChangeSceneRequested += OnGoBackButtonChangeSceneRequested;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnCreateNewGameButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.CreateNewGameRequested, path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    private void OnLoadGameButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.LoadGameRequested, path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }

    private void OnGoBackButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.GoBackRequested, path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }
}
