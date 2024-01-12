using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FourInARowBattle;

public static class ErrorCodeUtils
{
    private static readonly ReadOnlyDictionary<ErrorCodeEnum, string> _descriptionDict = new Dictionary<ErrorCodeEnum, string>()
    {
        {ErrorCodeEnum.UNKNOWN, "UNKNOWN ERROR"},
        {ErrorCodeEnum.GENERIC, "Generic error. This error type should not be used beyond testing."},

        {ErrorCodeEnum.CANNOT_CREATE_WHILE_IN_LOBBY, "Cannot create a new lobby while inside a lobby."},

        {ErrorCodeEnum.CANNOT_JOIN_WHILE_IN_LOBBY, "Cannot join a lobby while already inside a lobby."},
        {ErrorCodeEnum.CANNOT_JOIN_LOBBY_DOES_NOT_EXIST, "Cannot join lobby as it does not exist."},

        {ErrorCodeEnum.CANNOT_REQUEST_START_YOURSELF, "Cannot challenge yourself."},
        {ErrorCodeEnum.CANNOT_REQUEST_START_INVALID_PLAYER, "Challenged player is invalid."},
        {ErrorCodeEnum.CANNOT_REQUEST_START_MID_GAME, "Cannot start a new game in the middle of a game."},
        {ErrorCodeEnum.CANNOT_REQUEST_START_MID_GAME_OTHER, "Cannot start a new game with a player in the middle of a game."},
        {ErrorCodeEnum.CANNOT_REQUEST_START_NO_LOBBY, "Cannot start a new game without being in a lobby."},
        {ErrorCodeEnum.CANNOT_REQUEST_START_ALREADY_DID, "Game start request was already sent. Please wait."},
        {ErrorCodeEnum.CANNOT_REQUEST_START_OTHER_DID, "Other player already sent a game start request. Accept or reject it."},

        {ErrorCodeEnum.CANNOT_APPROVE_NO_REQUEST, "Cannot approve a game request that does not exist."},
        {ErrorCodeEnum.CANNOT_APPROVE_NOT_IN_LOBBY, "Cannot approve a game request while not in a lobby."},

        {ErrorCodeEnum.CANNOT_REJECT_NO_REQUEST, "Cannot reject a game request that does not exist."},
        {ErrorCodeEnum.CANNOT_REJECT_NOT_IN_LOBBY, "Cannot reject a game request while not in a lobby."},

        {ErrorCodeEnum.CANNOT_CANCEL_NO_REQUEST, "Cannot cancel a game request that does not exist."},
        {ErrorCodeEnum.CANNOT_CANCEL_NOT_IN_LOBBY, "Cannot cancel a game request while not in a lobby."},

        {ErrorCodeEnum.CANNOT_PLACE_NOT_IN_GAME, "Cannot place a token while not in a game."},
        {ErrorCodeEnum.CANNOT_PLACE_NOT_YOUR_TURN, "Cannot place the token as it is not your turn."},
        {ErrorCodeEnum.CANNOT_PLACE_NOT_ENOUGH_TOKENS, "Cannot place the token as you do not have enough."},
        {ErrorCodeEnum.CANNOT_PLACE_INVALID_COLUMN, "Cannot place the token as the column is invalid."},
        {ErrorCodeEnum.CANNOT_PLACE_FULL_COLUMN, "Cannot place the token as the column is full."},
        {ErrorCodeEnum.CANNOT_PLACE_INVALID_TOKEN, "Cannot place the token as it is invalid."},

        {ErrorCodeEnum.CANNOT_REFILL_NOT_IN_GAME, "Cannot refill while not in a game."},
        {ErrorCodeEnum.CANNOT_REFILL_NOT_YOUR_TURN, "Cannot refill as it is not your turn."},
        {ErrorCodeEnum.CANNOT_REFILL_ALL_FILLED, "Cannot refill as all token counters are filled."},
        {ErrorCodeEnum.CANNOT_REFILL_TWO_TURN_STREAK, "Cannot refill for two turns in a row."},
    }.AsReadOnly();

    public static string Humanize(ErrorCodeEnum error) =>  _descriptionDict.GetValueOrDefault(error, $"Invalid error code {error}");
}