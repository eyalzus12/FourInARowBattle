using Godot;

namespace FourInARowBattle;

public partial class Startup : Node
{
    public const string GAME_SAVE_FOLDER_BASE = "user://SaveData/";

    public override void _Ready()
    {
        if(!DirAccess.DirExistsAbsolute(GAME_SAVE_FOLDER_BASE))
        {
            Error err = DirAccess.MakeDirAbsolute(GAME_SAVE_FOLDER_BASE);
            if(err != Error.Ok)
                GD.PushError($"Error {err} while trying to create save data folder");
        }

        #pragma warning disable CS0162 // unreachable code warning
        if(4 * Globals.NAME_LENGTH_LIMIT > byte.MaxValue)
        {
            GD.PushWarning("Name length limit is too high. Remember that Utf8 can go up to 4 bytes per character.");
        }
        #pragma warning restore CS0162
    }
}
