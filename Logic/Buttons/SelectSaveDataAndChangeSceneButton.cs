using Godot;
using System;

public partial class SelectSaveDataAndChangeSceneButton : ChangeSceneOnPressButton
{
    [Export]
    public FileDialog FileSelectDialog{get; set;} = null!;

    private PersistentData _persistentData = null!;

    public override void _Ready()
    {
        _persistentData = GetTree().Root.GetNode<PersistentData>(nameof(PersistentData));
        FileSelectDialog.FileSelected += (string path) =>
        {
            _persistentData.ContinueFromState = ResourceLoader.Load<GameData>(path);
            base._Pressed();
        };
    }

    public override void _Pressed()
    {
        FileSelectDialog.PopupCentered(GetWindow().Size);
    }
}
