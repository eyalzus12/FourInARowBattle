namespace FourInARowBattle;

public enum ErrorCodeEnum : byte
{
    UNKNOWN,
    GENERIC,

    CANNOT_CREATE_WHILE_IN_LOBBY,

    CANNOT_JOIN_WHILE_IN_LOBBY,
    CANNOT_JOIN_LOBBY_DOES_NOT_EXIST,
    CANNOT_JOIN_LOBBY_FULL,

    CANNOT_REQUEST_START_NO_OTHER_PLAYER,
    CANNOT_REQUEST_START_MID_GAME,
    CANNOT_REQUEST_START_NO_LOBBY,
    CANNOT_REQUEST_START_ALREADY_DID,

    CANNOT_APPROVE_NO_REQUEST,
    CANNOT_APPROVE_NOT_IN_LOBBY,
    CANNOT_APPROVE_YOUR_REQUEST,

    CANNOT_REJECT_NO_REQUEST,
    CANNOT_REJECT_NOT_IN_LOBBY,
    CANNOT_REJECT_YOUR_REQUEST,

    CANNOT_CANCEL_NO_REQUEST,
    CANNOT_CANCEL_NOT_IN_LOBBY,
    CANNOT_CANCEL_NOT_YOUR_REQUEST,

    CANNOT_PLACE_NOT_IN_GAME,
    CANNOT_PLACE_NOT_YOUR_TURN,
    CANNOT_PLACE_NOT_ENOUGH_TOKENS,
    CANNOT_PLACE_INVALID_COLUMN,
    CANNOT_PLACE_FULL_COLUMN,
    CANNOT_PLACE_INVALID_TOKEN,

    CANNOT_REFILL_NOT_IN_GAME,
    CANNOT_REFILL_NOT_YOUR_TURN,
    CANNOT_REFILL_ALL_FILLED,
    CANNOT_REFILL_TWO_TURN_STREAK,
}
