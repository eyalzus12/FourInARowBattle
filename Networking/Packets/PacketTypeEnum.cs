namespace FourInARowBattle;

/// <summary>
/// An enum for packet types. The first byte in a packet.
/// </summary>
public enum PacketTypeEnum : byte
{
    //similar packet types have similar numberings
    //the goal of the non-uniform numbering is to make garbage packets easier to detect
    //00X       packets with special meaning
    //01X-07X   lobby management
    //08X-09X   unused
    //10X-13X   connection related
    //14X-19X   unused
    //20X-24X   game related
    //250-255   unused

    //dummy message. used for testing.
    //data: none
    DUMMY = 001,
    //invalid packet type. for internal use only.
    //data: given packet type
    INVALID_PACKET = 002,
    //server tells client they sent bad packet
    //data: given packet type
    INVALID_PACKET_INFORM = 003,
    //request to create a lobby
    //data: name length(8b) + name(var)
    CREATE_LOBBY_REQUEST = 010,
    //server approves lobby creation
    //data: lobby id(32b)
    CREATE_LOBBY_OK = 011,
    //server failed to create a lobby
    //data: error code(8b)
    CREATE_LOBBY_FAIL = 012,
    //request to connect to a lobby
    //data: lobby id(32b), name length(8b) + name(var)
    CONNECT_LOBBY_REQUEST = 020,
    //server approves lobby connection
    //data: connecter index(32b) + player count(8b) + names(var array of name length(8b) + name(var)) + busy states(var bit array of busy(1b))
    CONNECT_LOBBY_OK = 021,
    //failed to connect to lobby
    //data: error code(8b)
    CONNECT_LOBBY_FAIL = 022,
    //request to start a game
    //data: request target(32b)
    NEW_GAME_REQUEST = 030,
    //request to start a game succeded
    //data: request target(32b)
    NEW_GAME_REQUEST_OK = 031,
    //request to start a game failed
    //data: error code(8b), request target(32b)
    NEW_GAME_REQUEST_FAIL = 032,
    //player created a game request
    //data: request source(32b)
    NEW_GAME_REQUESTED = 033,
    //approve other players' game start request
    //data: request source(32b)
    NEW_GAME_ACCEPT = 040,
    //accepting game start request succeded
    //data: request source(32b)
    NEW_GAME_ACCEPT_OK = 041,
    //accepting game start request failed
    //data: error code(8b), request source(32b)
    NEW_GAME_ACCEPT_FAIL = 042,
    //player accepted a game request
    //data: request target(32b)
    //game will now start
    NEW_GAME_ACCEPTED = 043,
    //reject other players' game start request
    //data: request source(32b)
    NEW_GAME_REJECT = 050,
    //rejecting game start request succeeded
    //data: request source(32b)
    NEW_GAME_REJECT_OK = 051,
    //rejecting game start request failed
    //data: error code(8b), request source(32b)
    NEW_GAME_REJECT_FAIL = 052,
    //player rejected a game request
    //data: request target(32b)
    NEW_GAME_REJECTED = 053,
    //cancel new game request
    //data: request target(32b)
    NEW_GAME_CANCEL = 060,
    //canceling new game request succeeded
    //data: request target(32b)
    NEW_GAME_CANCEL_OK = 061,
    //canceling new game request failed
    //data: error code(8b), request target(32b)
    NEW_GAME_CANCEL_FAIL = 062,
    //player canceled a game request
    //data: request source(32b)
    NEW_GAME_CANCELED = 063,
    //player became busy
    //data: player index(32b)
    LOBBY_PLAYER_BUSY_TRUE = 070,
    //player stopped being busy
    //data: player index(32b)
    LOBBY_PLAYER_BUSY_FALSE = 071,
    //new player joined lobby
    //data: name length(8b) + name(var) of new player
    LOBBY_NEW_PLAYER = 100,
    //disconnect from the lobby
    //data: reason(8b)
    LOBBY_DISCONNECT = 110,
    //other player disconnected from lobby
    //data: reason(8b), player index(32b)
    LOBBY_DISCONNECT_OTHER = 111,
    //unused. lobby will timeout soon
    //data: seconds remaining(32b)
    LOBBY_TIMEOUT_WARNING = 120,
    //unused. lobby timed out.
    //data: none
    LOBBY_TIMEOUT = 121,
    //server is closing
    //data: none
    SERVER_CLOSING = 130,
    //new game is starting
    //data: player color(8b), opponent index(32b)
    NEW_GAME_STARTING = 200,
    //place a token
    //data: column(8b), scene path length(32b) + scene path(var)
    GAME_ACTION_PLACE = 210,
    //placing a token is ok
    //data: none
    GAME_ACTION_PLACE_OK = 211,
    //placing a token failed
    //data: error code(8b)
    GAME_ACTION_PLACE_FAIL = 212,
    //other player is placing a token
    //data: column(8b), scene path length(32b) + scene path(var)
    GAME_ACTION_PLACE_OTHER = 213,
    //refill
    //data: none
    GAME_ACTION_REFILL = 220,
    //refilling ok
    //data: none
    GAME_ACTION_REFILL_OK = 221,
    //refilling failed
    //data: error code(8b)
    GAME_ACTION_REFILL_FAIL = 222,
    //other player is refilling
    //date: none
    GAME_ACTION_REFILL_OTHER = 223,
    //quit the current game
    //data: none
    GAME_QUIT = 230,
    //quitting ok
    //data: none
    GAME_QUIT_OK = 231,
    //failed to quit game
    //data: error code(8b)
    GAME_QUIT_FAIL = 232,
    //other player quit game
    //data: none
    GAME_QUIT_OTHER = 233,
    //unused. game finished.
    //data: result(8b), player 1 points(32b), player 2 points(32b)
    GAME_FINISHED = 240,
}
