using Godot;

namespace FourInARowBattle;

public partial class ChangeSceneAndLoadGameButton : ChangeSceneOnPressButton
{
    [Export]
    public FileDialog FileSelectDialog{get; set;} = null!;

    public override void _Ready()
    {
        FileSelectDialog.FileSelected += (string path) =>
        {
            Autoloads.PersistentData.ContinueFromState = ResourceLoader.Load<GameData>(path, cacheMode: ResourceLoader.CacheMode.Replace);
            base._Pressed();
        };
        
        GetWindow().SizeChanged += _Pressed;
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        FileSelectDialog.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
