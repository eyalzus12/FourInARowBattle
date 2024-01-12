using Godot;
using System;

namespace FourInARowBattle;

public partial class ChangeSceneAndLoadGameButton : ChangeSceneOnPressButton
{
    [ExportCategory("Nodes")]
    [Export]
    private FileDialog _fileSelectDialog = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_fileSelectDialog);
    }

    private void ConnectSignals()
    {
        _fileSelectDialog.FileSelected += OnFileSelectDialogFileSelected;
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
        if(_fileSelectDialog.Visible)
            _Pressed();
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        _fileSelectDialog.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
