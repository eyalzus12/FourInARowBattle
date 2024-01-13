using Godot;

namespace FourInARowBattle;

/// <summary>
/// This is a simple class used to store the state of players in a lobby when connecting to it.
/// </summary>
public partial class LobbyPlayerData : RefCounted
{
    /// <summary>
    /// The name of the player
    /// </summary>
    public string Name{get; private set;}
    /// <summary>
    /// Whether the player is busy (in the middle of a game)
    /// </summary>
    public bool Busy{get; private set;}

    /// <summary>
    /// LobbyPlayerData constructor
    /// </summary>
    /// <param name="name">Player name</param>
    /// <param name="busy">Player busy state</param>
    public LobbyPlayerData(string name, bool busy)
    {
        Name = name;
        Busy = busy;
    }
}