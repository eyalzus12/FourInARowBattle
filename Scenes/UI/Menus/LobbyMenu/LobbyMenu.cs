using Godot;
using System;
using System.Collections.Generic;

namespace FourInARowBattle;

public partial class LobbyMenu : Control
{
    #region Signals
    [Signal]
    public delegate void ExitLobbyRequestedEventHandler(string path);
    [Signal]
    public delegate void ChallengeSentEventHandler(int index);
    [Signal]
    public delegate void ChallengeCanceledEventHandler(int index);
    [Signal]
    public delegate void ChallengeAcceptedEventHandler(int index);
    [Signal]
    public delegate void ChallengeRejectedEventHandler(int index);
    #endregion

    [ExportCategory("Nodes")]
    [Export]
    private GoBackButton _goBackButton = null!;
    [Export]
    private ConfirmationDialog _goBackConfirmationDialog = null!;
    [Export]
    private Label _lobbyIdLabel = null!;
    [Export]
    private Control _playerSlotsBase = null!;
    [ExportCategory("")]
    [Export]
    private PackedScene _playerSlotScene = null!;

    private readonly List<PlayerSlot> _slots = new();

    public int PlayerCount => _slots.Count;

    private string? _goBackRequestPath;

    private void VerifyExports()
    {
        ArgumentNullException.ThrowIfNull(_goBackButton);
        ArgumentNullException.ThrowIfNull(_goBackConfirmationDialog);
        ArgumentNullException.ThrowIfNull(_lobbyIdLabel);
        ArgumentNullException.ThrowIfNull(_playerSlotsBase);
        ArgumentNullException.ThrowIfNull(_playerSlotScene);
    }

    private void ConnectSignals()
    {
        GetWindow().SizeChanged += OnWindowSizeChanged;
        _goBackConfirmationDialog.Confirmed += OnGoBackConfirmationDialogConfirmed;
        _goBackButton.ChangeSceneRequested += OnGoBackButtonChangeSceneRequested;
        for(int i = 0; i < _slots.Count; ++i)
        {
            PlayerSlot slotBind = _slots[i];
            slotBind.ChallengeSent += () => OnPlayerSlotChallengeSent(slotBind);
            slotBind.ChallengeCanceled += () => OnPlayerSlotChallengeCanceled(slotBind);
            slotBind.ChallengeAccepted += () => OnPlayerSlotChallengeAccepted(slotBind);
            slotBind.ChallengeRejected += () => OnPlayerSlotChallengeRejected(slotBind);
        }
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

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
        _goBackConfirmationDialog.PopupCentered();
    }

    private void OnPlayerSlotChallengeSent(PlayerSlot which)
    {
        int index = _slots.FindIndex(s => s == which);
        EmitSignal(SignalName.ChallengeSent, index);
    }

    private void OnPlayerSlotChallengeCanceled(PlayerSlot which)
    {
        int index = _slots.FindIndex(s => s == which);
        EmitSignal(SignalName.ChallengeCanceled, index);
    }

    private void OnPlayerSlotChallengeAccepted(PlayerSlot which)
    {
        int index = _slots.FindIndex(s => s == which);
        EmitSignal(SignalName.ChallengeAccepted, index);
    }

    private void OnPlayerSlotChallengeRejected(PlayerSlot which)
    {
        int index = _slots.FindIndex(s => s == which);
        EmitSignal(SignalName.ChallengeRejected, index);
    }

    #endregion

    public void SetLobbyId(uint id)
    {
        _lobbyIdLabel.Text = id.ToString();
    }

    public void SetMark(int index)
    {
        for(int i = 0; i < _slots.Count; ++i)
        {
            _slots[i].Marked = i == index;
        }
    }

    public void ClearMark()
    {
        SetMark(-1);
    }

    public void AddPlayer(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        PlayerSlot slot = Autoloads.ScenePool.GetScene<PlayerSlot>(_playerSlotScene);
        _playerSlotsBase.AddChild(slot);
        slot.SetState(ChallengeStateEnum.NONE);
        slot.PlayerName = name;
        _slots.Add(slot);
    }

    public void RemovePlayer(int index)
    {
        Autoloads.ScenePool.ReturnScene(_slots[index]);
        _slots.RemoveAt(index);
    }

    public void SetPlayerNames(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        for(int i = 0; i < names.Length; ++i)
        {
            if(i >= _slots.Count) AddPlayer(names[i]);
            _slots[i].PlayerName = names[i];
        }
    }

    public void SetChallengeState(ChallengeStateEnum state, int index)
    {
        _slots[index].SetState(state);
    }
}
