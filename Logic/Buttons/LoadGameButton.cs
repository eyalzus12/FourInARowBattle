using Godot;

namespace FourInARowBattle;

public partial class LoadGameButton : Button
{
    [Export]
    public Game GameToLoadTo{get; set;} = null!;
    [Export]
    public FileDialog LoadGamePopup{get; set;} = null!;

    private void VerifyExports()
    {
        if(GameToLoadTo is null) { GD.PushError($"No {nameof(GameToLoadTo)} set"); return; }
        if(LoadGamePopup is null) { GD.PushError($"No {nameof(LoadGamePopup)} set"); return; }
    }

    private void ConnectSignals()
    {
        LoadGamePopup.FileSelected += OnLoadGamePopupFileSelected;
        GetWindow().SizeChanged += OnWindowSizeChanged;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnLoadGamePopupFileSelected(string path)
    {
        GameData saveData = ResourceLoader.Load<GameData>(path, cacheMode: ResourceLoader.CacheMode.Replace);
        GameToLoadTo.DeserializeFrom(saveData);
    }

    private void OnWindowSizeChanged()
    {
        if(LoadGamePopup.Visible)
            _Pressed();
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        LoadGamePopup.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
