using Godot;
using System;

public partial class ChangeSceneAndLoadGameButton : ChangeSceneOnPressButton
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
        
        GetWindow().SizeChanged += _Pressed;
    }

    public override void _Pressed()
    {
        var decorations = GetWindow().GetSizeOfDecorations();
        FileSelectDialog.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
