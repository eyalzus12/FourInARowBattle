using Godot;
using System;

namespace FourInARowBattle;

/// <summary>
/// This class represents a player slot inside the lobby menu.
/// </summary>
public partial class PlayerSlot : Control
{
    #region Signals

    /// <summary>
    /// Challenge button was pressed
    /// </summary>
    [Signal]
    public delegate void ChallengeSentEventHandler();
    /// <summary>
    /// Challenge cancel button was pressed
    /// </summary>
    [Signal]
    public delegate void ChallengeCanceledEventHandler();
    /// <summary>
    /// Challenge accept button was pressed
    /// </summary>
    [Signal]
    public delegate void ChallengeAcceptedEventHandler();
    /// <summary>
    /// Challenge reject button was pressed
    /// </summary>
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
    /// <summary>
    /// When true, highlights the player name
    /// </summary>
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

    /// <summary>
    /// Event: Challenge button pressed
    /// </summary>
    private void OnSendChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeSent);
    }

    /// <summary>
    /// Event: Cancel challenge button pressed
    /// </summary>
    private void OnCancelChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeCanceled);
    }

    /// <summary>
    /// Event: Accept challenge button pressed
    /// </summary>
    private void OnAcceptChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeAccepted);
    }

    /// <summary>
    /// Event: Reject challenge button pressed
    /// </summary>
    private void OnRejectChallengeButtonPressed()
    {
        EmitSignal(SignalName.ChallengeRejected);
    }

    #endregion

    /// <summary>
    /// Set current challenge state
    /// </summary>
    /// <param name="state">The state to change to</param>
    public void SetState(ChallengeStateEnum state)
    {
        switch(state)
        {
            //No challenge. Show challenge button.
            case ChallengeStateEnum.NONE:
                _sendChallengeButton.Visible = true;
                _cancelChallengeButton.Visible = false;
                _acceptChallengeButton.Visible = false;
                _rejectChallengeButton.Visible = false;
                break;
            //Cannot challenge. Show nothing.
            case ChallengeStateEnum.CANNOT:
                _sendChallengeButton.Visible = false;
                _cancelChallengeButton.Visible = false;
                _acceptChallengeButton.Visible = false;
                _rejectChallengeButton.Visible = false;
                break;
            //Sent a challenge. Show cancel button.
            case ChallengeStateEnum.SENT:
                _sendChallengeButton.Visible = false;
                _cancelChallengeButton.Visible = true;
                _acceptChallengeButton.Visible = false;
                _rejectChallengeButton.Visible = false;
                break;
            //Got a challenge. Show accept and reject buttons.
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
