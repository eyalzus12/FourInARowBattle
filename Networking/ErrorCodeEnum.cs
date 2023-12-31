namespace FourInARowBattle;

public enum ErrorCodeEnum : byte
{
    //unknown error
    UNKNOWN,
    //generic error
    GENERIC,
    //cannot create lobby - already in lobby
    CANNOT_CREATE_WHILE_IN_LOBBY,
    //cannot join lobby - current in lobby
    CANNOT_JOIN_WHILE_IN_LOBBY,
    //lobby does not exist
    LOBBY_DOES_NOT_EXIST,
    //can't approve - no request sent
    CANNOT_APPROVE_NO_REQUEST,
    //can't reject - no request sent
    CANNOT_REJECT_NO_REQUEST,
    //can't cancel - no request sent
    CANNOT_CANCEL_NO_REQUEST,
    //can't start game - no other player
    CANNOT_START_NO_OTHER_PLAYER,
    //can't start game - current in game
    CANNOT_START_MID_GAME,
    //can't start game - not in lobby
    CANNOT_START_NO_LOBBY,
    //can't place - not your turn
    CANNOT_PLACE_NOT_YOUR_TURN,
    //can't refill - not your turn
    CANNOT_REFILL_NOT_YOUR_TURN,
    //can't place - not enough of type
    CANNOT_PLACE_NOT_ENOUGH_TOKENS,
    //can't place - invalid column
    CANNOT_PLACE_INVALID_COLUMN,
    //can't place - full column
    CANNOT_PLACE_FULL_COLUMN,
    //can't refill - all are filled
    CANNOT_REFILL_ALL_FILLED,
    //can't refill - two turns in a row
    CANNOT_REFILL_TWO_TURN_STREAK,
    //can't place - invalid token type
    CANNOT_PLACE_INVALID_TOKEN,
}
