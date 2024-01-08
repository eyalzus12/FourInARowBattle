using Godot;

namespace FourInARowBattle;

public partial class ChangeSceneOnPressButton : BaseButton
{
    [Signal]
    public delegate void ChangeSceneRequestedEventHandler(string path);

    //to avoid a cyclic reference, we store the file path

    [Export(PropertyHint.File, "*.tscn,*.scn")]
    public string ChangeTo{get; set;} = "";

    public override void _Pressed()
    {
        if(ChangeTo != "")
            EmitSignal(SignalName.ChangeSceneRequested, ChangeTo);
    }
}
