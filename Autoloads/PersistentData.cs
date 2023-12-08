using Godot;

namespace FourInARowBattle;

//this class is used to transfer data between scenes when switching between them
public partial class PersistentData : Node
{
    public GameData? ContinueFromState{get; set;}
}
