using Godot;

namespace FourInARowBattle;

public partial class ChangeSceneOnPressButton : BaseButton
{
    //to avoid a cyclic reference, we store the file path

    [Export(PropertyHint.File, "*.tscn,*.scn")]
    public string ChangeTo{get; set;} = "";

    public override void _Pressed()
    {
        Error err = GetTree().ChangeSceneToFile(ChangeTo);
        if(err != Error.Ok)
            GD.Print($"Error while attempting to change scene: {err}");
    }
}
