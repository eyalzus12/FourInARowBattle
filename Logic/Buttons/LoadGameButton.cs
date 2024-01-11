using Godot;
using System;

namespace FourInARowBattle;

public partial class LoadGameButton : Button
{
    [Signal]
    public delegate void GameLoadRequestedEventHandler(string path);
    
    [ExportCategory("Nodes")]
    [Export]
    private FileDialog LoadGamePopup = null!;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(LoadGamePopup);
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
        ArgumentNullException.ThrowIfNull(path);
        EmitSignal(SignalName.GameLoadRequested, path);
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
