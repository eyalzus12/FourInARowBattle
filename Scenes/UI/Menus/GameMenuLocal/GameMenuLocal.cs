using Godot;

namespace FourInARowBattle;

public partial class GameMenuLocal : GameMenu
{
    private void ConnectSignals()
    {
        TokenPlaceAttempted += OnTokenPlaceAttempted;
        RefillAttempted += OnRefillAttempted;
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
}