using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FourInARowBattle;

/// <summary>
/// This is the UI class for the lobby menu.
/// </summary>
public partial class LobbyMenu : Control
{
    #region Signals
    /// <summary>
    /// Exit lobby was pressed and confirmed
    /// </summary>
    /// <param name="path">The path to the remote play menu scene</param>
    [Signal]
    public delegate void ExitLobbyRequestedEventHandler(string path);
    /// <summary>
    /// Challenge button was pressed
    /// </summary>
    /// <param name="index">Which player it was pressed for</param>
    [Signal]
    public delegate void ChallengeSentEventHandler(int index);
    /// <summary>
    /// Challenge cancel button was pressed
    /// </summary>
    /// <param name="index">Which player it was pressed for</param>
    [Signal]
    public delegate void ChallengeCanceledEventHandler(int index);
    /// <summary>
    /// Challenge accept button was pressed
    /// </summary>
    /// <param name="index">Which player it was pressed for</param>
    [Signal]
    public delegate void ChallengeAcceptedEventHandler(int index);
    /// <summary>
    /// Challenge reject button was pressed
    /// </summary>
    /// <param name="index">Which player it was pressed for</param>
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
    }

    /// <summary>
    /// Connect a slot signals to the functions
    /// </summary>
    /// <param name="slot">The slot to connect</param>
    private void ConnectSlotSignals(PlayerSlot slot)
    {
        slot.ChallengeSent += () => OnPlayerSlotChallengeSent(slot);
        slot.ChallengeCanceled += () => OnPlayerSlotChallengeCanceled(slot);
        slot.ChallengeAccepted += () => OnPlayerSlotChallengeAccepted(slot);
        slot.ChallengeRejected += () => OnPlayerSlotChallengeRejected(slot);
    }

    public override void _Ready()
    {
        VerifyExports();
        ConnectSignals();
    }

    #region Signal Handling

    /// <summary>
    /// Event: Go back was confirmed
    /// </summary>
    private void OnGoBackConfirmationDialogConfirmed()
    {
        if(_goBackRequestPath is null) return;
        EmitSignal(SignalName.ExitLobbyRequested, _goBackRequestPath);
    }

    /// <summary>
    /// Event: Window size changed. Resize popup.
    /// </summary>
    private void OnWindowSizeChanged()
    {
        if(_goBackConfirmationDialog.Visible)
            OnGoBackButtonChangeSceneRequested(_goBackRequestPath!);
    }

    /// <summary>
    /// Event: Go back button pressed. Show popup.
    /// </summary>
    /// <param name="path">The path to the remote play menu scene</param>
    private void OnGoBackButtonChangeSceneRequested(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _goBackRequestPath = path;
        _goBackConfirmationDialog.PopupCentered();
    }

    /// <summary>
    /// Challenge button pressed
    /// </summary>
    /// <param name="which">What slot it was pressed on</param>
    private void OnPlayerSlotChallengeSent(PlayerSlot which)
    {
        int index = _slots.FindIndex(s => s == which);
        EmitSignal(SignalName.ChallengeSent, index);
    }

    /// <summary>
    /// Challenge cancel button pressed
    /// </summary>
    /// <param name="which">What slot it was pressed on</param>
    private void OnPlayerSlotChallengeCanceled(PlayerSlot which)
    {
        int index = _slots.FindIndex(s => s == which);
        EmitSignal(SignalName.ChallengeCanceled, index);
    }

    /// <summary>
    /// Challenge accept button pressed
    /// </summary>
    /// <param name="which">What slot it was pressed on</param>
    private void OnPlayerSlotChallengeAccepted(PlayerSlot which)
    {
        int index = _slots.FindIndex(s => s == which);
        EmitSignal(SignalName.ChallengeAccepted, index);
    }

    /// <summary>
    /// Challenge reject button pressed
    /// </summary>
    /// <param name="which">What slot it was pressed on</param>
    private void OnPlayerSlotChallengeRejected(PlayerSlot which)
    {
        int index = _slots.FindIndex(s => s == which);
        EmitSignal(SignalName.ChallengeRejected, index);
    }

    #endregion

    /// <summary>
    /// Set the displayed loby id
    /// </summary>
    /// <param name="id">The id</param>
    public void SetLobbyId(uint id)
    {
        _lobbyIdLabel.Text = id.ToString();
    }

    /// <summary>
    /// Set what player slot is marked as us
    /// </summary>
    /// <param name="index">The slot to mark</param>
    public void SetMark(int index)
    {
        for(int i = 0; i < _slots.Count; ++i)
        {
            _slots[i].Marked = i == index;
        }
    }

    /// <summary>
    /// Dispose of all player slots
    /// </summary>
    public void ClearPlayers()
    {
        foreach(PlayerSlot slot in _slots)
        {
            Autoloads.ScenePool.ReturnScene(slot);
        }
        _slots.Clear();
    }

    /// <summary>
    /// Add a player slot
    /// </summary>
    /// <param name="name">The player name</param>
    public void AddPlayer(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        PlayerSlot slot = Autoloads.ScenePool.GetScene<PlayerSlot>(_playerSlotScene);
        _playerSlotsBase.AddChild(slot);
        ConnectSlotSignals(slot);
        slot.SetState(ChallengeStateEnum.NONE);
        slot.PlayerName = name;
        slot.Marked = false;
        _slots.Add(slot);
    }

    /// <summary>
    /// Remove a player slot
    /// </summary>
    /// <param name="index">The index to remove at</param>
    public void RemovePlayer(int index)
    {
        Autoloads.ScenePool.ReturnScene(_slots[index]);
        _slots.RemoveAt(index);
    }

    /// <summary>
    /// Set the names of the players in the lobby, adding new slots if needed
    /// </summary>
    /// <param name="names">The list of names</param>
    public void SetPlayerNames(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        for(int i = 0; i < names.Length; ++i)
        {
            if(i >= _slots.Count) AddPlayer(names[i]);
            _slots[i].PlayerName = names[i];
        }
    }

    /// <summary>
    /// Set the challenge state of a player slot
    /// </summary>
    /// <param name="state">The state to set to</param>
    /// <param name="index">The index of the player</param>
    public void SetChallengeState(ChallengeStateEnum state, int index)
    {
        _slots[index].SetState(state);
    }

    /// <summary>
    /// Set all slots to the same challenge state
    /// </summary>
    /// <param name="state">The state to set to</param>
    public void SetAllChallengeStates(ChallengeStateEnum state)
    {
        foreach(PlayerSlot slot in _slots)
            slot.SetState(state);
    }

    /// <summary>
    /// Set all slots to the same challenge state, except the marked slot
    /// </summary>
    /// <param name="state">The state to set to</param>
    public void SetAllChallengeStatesExceptMark(ChallengeStateEnum state)
    {
        foreach(PlayerSlot slot in _slots)
            if(!slot.Marked)
                slot.SetState(state);
    }

    /// <summary>
    /// Get the name of a player
    /// </summary>
    /// <param name="index">The player index</param>
    /// <returns>The name of a player</returns>
    public string GetPlayerName(int index)
    {
        return _slots[index].PlayerName;
    }

    /// <summary>
    /// Get the name of the marked slot. Errors if none or more than 1 exist.
    /// </summary>
    /// <returns>The name of the marked slot</returns>
    public string GetMarkedName()
    {
        return _slots.Where(s => s.Marked).Single().PlayerName;
    }
}
