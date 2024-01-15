using System;
using Godot;

namespace FourInARowBattle;

/// <summary>
/// A local version of GameMenu that does not rely on server verification.
/// Instead, the signals are directly connected to local methods.
/// </summary>
public partial class GameMenuLocal : GameMenu
{
    private void ConnectSignals()
    {
        TokenPlaceAttempted += OnTokenPlaceAttempted;
        RefillAttempted += OnRefillAttempted;
        GameQuitRequested += OnGameQuitRequested;
    }

    public override void _Ready()
    {
        base._Ready();
        ConnectSignals();
    }

    private void OnTokenPlaceAttempted(int column, PackedScene scene)
    {
        PlaceToken(column, scene);
    }

    private void OnRefillAttempted()
    {
        Refill();
    }

    private void OnGameQuitRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }
}