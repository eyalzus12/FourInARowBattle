using Godot;
using System;

namespace FourInARowBattle;

public partial class PlayerSlot : Control
{
    #region Signals
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
    private Label _playerName = null!;
    [Export]
    private Button _sendChallengeButton = null!;
    [Export]
    private Button _cancelChallengeButton = null!;
    [Export]
    private Button _acceptChallengeButton = null!;
    [Export]
    private Button _rejectChallengeButton = null!;

    private Color? _oldModulate;

    private bool _marked = false;
    public bool Marked
    {
        get => _marked;
        set
        {
            _marked = value;
            if(_marked)
            {
                _oldModulate ??= _playerName.Modulate;
                _playerName.Modulate = Colors.Cyan;
            }
            else
            {
                if(_oldModulate is not null)
                    _playerName.Modulate = (Color)_oldModulate;
                _oldModulate = null;
            }
        }
    }

    public string PlayerName
    {
        get => _playerName.Text;
        set => _playerName.Text = value;
    }

    public ChallengeStateEnum State{get; set;} = ChallengeStateEnum.NONE;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_playerName);
        ArgumentNullException.ThrowIfNull(_sendChallengeButton);
        ArgumentNullException.ThrowIfNull(_cancelChallengeButton);
        ArgumentNullException.ThrowIfNull(_acceptChallengeButton);
        ArgumentNullException.ThrowIfNull(_rejectChallengeButton);
    }

    private void ConnectSignals()
    {
        _sendChallengeButton.Pressed += OnSendChallengeButtonPressed;
        _cancelChallengeButton.Pressed += OnCancelChallengeButtonPressed;
        _acceptChallengeButton.Pressed += OnAcceptChallengeButtonPressed;
        _rejectChallengeButton.Pressed += OnRejectChallengeButtonPressed;
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    #region Signal Handling
    private void OnSendChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeSent);
    }

    private void OnCancelChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeCanceled);
    }

    private void OnAcceptChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeAccepted);
    }

    private void OnRejectChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeRejected);
    }
    #endregion

    public void SetState(ChallengeStateEnum state)
    {
        switch(state)
        {
            case ChallengeStateEnum.NONE:
                _sendChallengeButton.Visible = true;
                _cancelChallengeButton.Visible = false;
                _acceptChallengeButton.Visible = false;
                _rejectChallengeButton.Visible = false;
                break;
            case ChallengeStateEnum.CANNOT:
                _sendChallengeButton.Visible = false;
                _cancelChallengeButton.Visible = false;
                _acceptChallengeButton.Visible = false;
                _rejectChallengeButton.Visible = false;
                break;
            case ChallengeStateEnum.SENT:
                _sendChallengeButton.Visible = false;
                _cancelChallengeButton.Visible = true;
                _acceptChallengeButton.Visible = false;
                _rejectChallengeButton.Visible = false;
                break;
            case ChallengeStateEnum.GOT:
                _sendChallengeButton.Visible = false;
                _cancelChallengeButton.Visible = false;
                _acceptChallengeButton.Visible = true;
                _rejectChallengeButton.Visible = true;
                break;
        }
        State = state;
    }
}
