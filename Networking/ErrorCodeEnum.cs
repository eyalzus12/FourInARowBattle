namespace FourInARowBattle;

public enum ErrorCodeEnum : byte
{
    //similar error codes have similar numberings
    //the goal of the non-uniform numbering is to make garbage packets easier to detect
    //00X       errors that should be reserved for testing
    //01X-06X   lobby related errors
    //07X-09X   unused
    //10X-11X   game related errors
    //12X-255   unused

    UNKNOWN = 001,
    GENERIC = 002,

    CANNOT_CREATE_WHILE_IN_LOBBY = 010,

    CANNOT_JOIN_WHILE_IN_LOBBY = 020,
    CANNOT_JOIN_LOBBY_DOES_NOT_EXIST = 021,
    CANNOT_JOIN_LOBBY_FULL = 022,

    CANNOT_REQUEST_START_YOURSELF = 030,
    CANNOT_REQUEST_START_INVALID_PLAYER = 031,
    CANNOT_REQUEST_START_MID_GAME = 032,
    CANNOT_REQUEST_START_MID_GAME_OTHER = 033,
    CANNOT_REQUEST_START_NO_LOBBY = 034,
    CANNOT_REQUEST_START_ALREADY_DID = 035,
    CANNOT_REQUEST_START_OTHER_DID = 036,

    CANNOT_APPROVE_NO_REQUEST = 040,
    CANNOT_APPROVE_NOT_IN_LOBBY = 041,


    CANNOT_REJECT_NO_REQUEST = 050,
    CANNOT_REJECT_NOT_IN_LOBBY = 051,

    CANNOT_CANCEL_NO_REQUEST = 060,
    CANNOT_CANCEL_NOT_IN_LOBBY = 061,

    CANNOT_PLACE_NOT_IN_GAME = 100,
    CANNOT_PLACE_NOT_YOUR_TURN = 101,
    CANNOT_PLACE_NOT_ENOUGH_TOKENS = 102,
    CANNOT_PLACE_INVALID_COLUMN = 103,
    CANNOT_PLACE_FULL_COLUMN = 104,
    CANNOT_PLACE_INVALID_TOKEN = 105,

    CANNOT_REFILL_NOT_IN_GAME = 110,
    CANNOT_REFILL_NOT_YOUR_TURN = 111,
    CANNOT_REFILL_ALL_FILLED = 112,
    CANNOT_REFILL_TWO_TURN_STREAK = 113,
}
