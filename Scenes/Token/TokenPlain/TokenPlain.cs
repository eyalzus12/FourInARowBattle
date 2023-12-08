using Godot;

namespace FourInARowBattle;

public partial class TokenPlain : TokenOnDropFinish
{
    public override void OnDropFinish(Board board, int row, int col)
    {
        AudioStreamPlayer player = Autoloads.AudioManager.PlayersPool.GetObject();
        AudioStream stream = Autoloads.GlobalResources.TEST_LAND;
        player.Play(stream);
    }
}
