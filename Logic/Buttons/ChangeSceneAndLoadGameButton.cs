using Godot;
using System;

namespace FourInARowBattle;

public partial class ChangeSceneAndLoadGameButton : ChangeSceneOnPressButton
{
    [ExportCategory("Nodes")]
    [Export]
    private FileDialog FileSelectDialog = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(FileSelectDialog);
    }

    private void ConnectSignals()
    {
        FileSelectDialog.FileSelected += OnFileSelectDialogFileSelected;
        GetWindow().SizeChanged += OnWindowSizeChanged;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnFileSelectDialogFileSelected(string path)
    {
        Autoloads.PersistentData.ContinueFromState = ResourceLoader.Load<GameData>(path, cacheMode: ResourceLoader.CacheMode.Replace);
        base._Pressed();
    }

    private void OnWindowSizeChanged()
    {
        if(FileSelectDialog.Visible)
            _Pressed();
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        FileSelectDialog.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
