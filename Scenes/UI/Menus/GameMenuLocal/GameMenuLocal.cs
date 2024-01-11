using Godot;

namespace FourInARowBattle;

public partial class GameMenuLocal : GameMenu
{
    private void ConnectSignals()
    {
        TokenPlaceAttempted += OnTokenPlaceAttempted;
        RefillAttempted += OnRefillAttempted;
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