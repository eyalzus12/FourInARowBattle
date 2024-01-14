using Godot;
using System.Collections.Generic;

namespace FourInARowBattle;

/// <summary>
/// Class for code to run in startup
/// </summary>
public partial class Startup : Node
{
    public const string GAME_SAVE_FOLDER_BASE = "user://SaveData/";

    public Dictionary<string, string> UserCmdlineArgs{get; private set;} = new();

    public override void _Ready()
    {
        Autoloads.Startup = this;

        //create save folder
        if(!DirAccess.DirExistsAbsolute(GAME_SAVE_FOLDER_BASE))
        {
            Error err = DirAccess.MakeDirAbsolute(GAME_SAVE_FOLDER_BASE);
            if(err != Error.Ok)
                GD.PushError($"Error {err} while trying to create save data folder");
        }
        
        //store user commandline arguments
        foreach(var argument in OS.GetCmdlineUserArgs())
        {
            if(argument.Find("=") > -1)
            {
                string[] keyValue = argument.Split("=");
                UserCmdlineArgs[keyValue[0].TrimPrefix("--")] = keyValue[1];
            }
            else
            {
                UserCmdlineArgs[argument.TrimPrefix("--")] = "";
            }
        }

        #pragma warning disable CS0162 // unreachable code warning
        if(4 * Globals.NAME_LENGTH_LIMIT > byte.MaxValue)
        {
            GD.PushWarning("Name length limit is too high. Remember that Utf8 can go up to 4 bytes per character.");
        }
        #pragma warning restore CS0162
    }
}
