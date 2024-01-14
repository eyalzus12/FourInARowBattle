using Godot;
using System;

namespace FourInARowBattle;

/// <summary>
/// A button that on press opens a menu to select a game state to load into.
/// </summary>
public partial class LoadGameButton : Button
{
    [Signal]
    public delegate void GameLoadRequestedEventHandler(string path);
    
    [ExportCategory("Nodes")]
    [Export]
    private FileDialog _loadGamePopup = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_loadGamePopup);
    }

    private void ConnectSignals()
    {
        _loadGamePopup.FileSelected += OnLoadGamePopupFileSelected;
        GetWindow().SizeChanged += OnWindowSizeChanged;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    private void OnLoadGamePopupFileSelected(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.GameLoadRequested, path);
    }

    private void OnWindowSizeChanged()
    {
        if(_loadGamePopup.Visible)
            _Pressed();
    }

    public override void _Pressed()
    {
        Vector2I decorations = GetWindow().GetSizeOfDecorations();
        _loadGamePopup.PopupCentered(GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y));
    }
}
