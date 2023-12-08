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
    }
}
