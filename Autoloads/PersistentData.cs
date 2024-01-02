using Godot;

namespace FourInARowBattle;

//this class is used to transfer data between scenes when switching between them
public partial class PersistentData : Node
{
    public GameData? ContinueFromState{get; set;}
    public bool HeadlessMode{get; set;} = false;

    public override void _Ready()
    {
        Autoloads.PersistentData = this;
    }
}
