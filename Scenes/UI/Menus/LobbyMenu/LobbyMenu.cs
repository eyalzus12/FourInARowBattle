using Godot;
using System;

namespace FourInARowBattle;

public partial class LobbyMenu : Control
{
    #region Signals
    [Signal]
    public delegate void ExitLobbyRequestedEventHandler(string path);
    [Signal]
    public delegate void ChallengeSentEventHandler();
    [Signal]
    public delegate void ChallengeCanceledEventHandler();
    [Signal]
    public delegate void ChallengeAcceptedEventHandler();
    [Signal]
    public delegate void ChallengeRejectedEventHandler();
    #endregion

    [ExportCategory("Nodes")]
    [Export]
    private GoBackButton _goBackButton = null!;
    [Export]
    private ConfirmationDialog _goBackConfirmationDialog = null!;
    [Export]
    private Label _lobbyIdLabel = null!;
    [Export]
    private Label _player1NameLabel = null!;
    [Export]
    private Label _player2NameLabel = null!;
    [Export]
    private LobbyGameChallengeMenu _gameChallengeSubMenu = null!;

    private string? _goBackRequestPath;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_goBackButton);
        ArgumentNullException.ThrowIfNull(_goBackConfirmationDialog);
        ArgumentNullException.ThrowIfNull(_lobbyIdLabel);
        ArgumentNullException.ThrowIfNull(_player1NameLabel);
        ArgumentNullException.ThrowIfNull(_player2NameLabel);
        ArgumentNullException.ThrowIfNull(_gameChallengeSubMenu);
    }

    private void ConnectSignals()
    {
        _goBackConfirmationDialog.Confirmed += OnGoBackConfirmationDialogConfirmed;
        GetWindow().SizeChanged += OnWindowSizeChanged;
        _goBackButton.ChangeSceneRequested += OnGoBackButtonChangeSceneRequested;
        _gameChallengeSubMenu.ChallengeSent += OnChallengeSubMenuChallengeSent;
        _gameChallengeSubMenu.ChallengeCanceled += OnChallengeSubMenuChallengeCanceled;
        _gameChallengeSubMenu.ChallengeAccepted += OnChallengeSubMenuChallengeAccepted;
        _gameChallengeSubMenu.ChallengeRejected += OnChallengeSubMenuChallengeRejected;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    public bool LobbyFull() => _player1NameLabel.Text != "" && _player2NameLabel.Text != "";

    #region Signal Handling
    private void OnGoBackConfirmationDialogConfirmed()
    {
        if(_goBackRequestPath is null) return;
        EmitSignal(SignalName.ExitLobbyRequested, _goBackRequestPath);
    }

    private void OnWindowSizeChanged()
    {
        if(_goBackConfirmationDialog.Visible)
            OnGoBackButtonChangeSceneRequested(_goBackRequestPath!);
    }

    private void OnGoBackButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _goBackRequestPath = path;

        //Vector2I decorations = GetWindow().GetSizeOfDecorations();
        _goBackConfirmationDialog.PopupCentered(/*GetWindow().GetVisibleSize() - new Vector2I(0,decorations.Y)*/);
    }

    private void OnChallengeSubMenuChallengeSent()
    {
        EmitSignal(SignalName.ChallengeSent);
    }

    private void OnChallengeSubMenuChallengeCanceled()
    {
        EmitSignal(SignalName.ChallengeCanceled);
    }

    private void OnChallengeSubMenuChallengeAccepted()
    {
        EmitSignal(SignalName.ChallengeAccepted);
    }

    private void OnChallengeSubMenuChallengeRejected()
    {
        EmitSignal(SignalName.ChallengeRejected);
    }

    #endregion

    public void SetLobbyId(uint id)
    {
        _lobbyIdLabel.Text = id.ToString();
    }

    private Color? _player1OldModulate = null;
    private Color? _player2OldModulate = null;

    public void SetPlayer1Marked()
    {
        if(_player2OldModulate is not null)
        {
            _player2NameLabel.Modulate = (Color)_player2OldModulate;
            _player2OldModulate = null;
        }

        _player1OldModulate ??= _player1NameLabel.Modulate;
        _player1NameLabel.Modulate = Colors.Blue;
    }

    public void SetPlayer2Marked()
    {
        if(_player1OldModulate is not null)
        {
            _player1NameLabel.Modulate = (Color)_player1OldModulate;
            _player1OldModulate = null;
        }

        _player2OldModulate ??= _player2NameLabel.Modulate;
        _player2NameLabel.Modulate = Colors.Blue;
    }

    public void ClearMark()
    {
        if(_player1OldModulate is not null)
        {
            _player1NameLabel.Modulate = (Color)_player1OldModulate;
            _player1OldModulate = null;
        }

        if(_player2OldModulate is not null)
        {
            _player2NameLabel.Modulate = (Color)_player2OldModulate;
            _player2OldModulate = null;
        }
    }

    public void SetPlayer1Name(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if(name.Length > Globals.NAME_LENGTH_LIMIT) name = name[..Globals.NAME_LENGTH_LIMIT];
        _player1NameLabel.Text = name;
    }

    public void SetPlayer2Name(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if(name.Length > Globals.NAME_LENGTH_LIMIT) name = name[..Globals.NAME_LENGTH_LIMIT];
        _player2NameLabel.Text = name;
    }

    public void SetChallengeState_NoChallenge()
    {
        _gameChallengeSubMenu.SetState_NoChallenge();
    }

    public void SetChallengeState_SentChallenge()
    {
        _gameChallengeSubMenu.SetState_SentChallenge();
    }

    public void SetChallengeState_GotChallenge()
    {
        _gameChallengeSubMenu.SetState_GotChallenge();
    }

    public void SetChallengeState_CannotChallenge()
    {
        _gameChallengeSubMenu.SetState_CannotChallenge();
    }
}
