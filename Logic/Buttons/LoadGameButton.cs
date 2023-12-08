using Godot;

namespace FourInARowBattle;

public partial class LoadGameButton : Button
{
    [Export]
    public Game GameToLoadTo{get; set;} = null!;
    [Export]
    public FileDialog LoadGamePopup{get; set;} = null!;

    public override void _Ready()
    {
        LoadGamePopup.FileSelected += (string path) =>
        {
            GameData saveData = ResourceLoader.Load<GameData>(path, cacheMode: ResourceLoader.CacheMode.Replace);
            GameToLoadTo.DeserializeFrom(saveData);
        };
        
        GetWindow().SizeChanged += _Pressed;
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        LoadGamePopup.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
