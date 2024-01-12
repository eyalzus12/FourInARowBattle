using Godot;

namespace FourInARowBattle;

public partial class ChangeSceneOnPressButton : BaseButton
{
    [Signal]
    public delegate void ChangeSceneRequestedEventHandler(string path);

    //to avoid a cyclic reference, we store the file path

    [Export(PropertyHint.File, "*.tscn,*.scn")]
    private string _changeTo = "";

    public override void _Pressed()
    {
        if(_changeTo != "")
            EmitSignal(SignalName.ChangeSceneRequested, _changeTo);
    }
}
