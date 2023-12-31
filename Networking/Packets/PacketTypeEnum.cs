namespace FourInARowBattle;

public enum PacketTypeEnum : byte
{
    //dummy message
    //data: none
    DUMMY,
    //invalid packet type
    //data: given packet type
    INVALID_PACKET,
    //server tells client they sent bad packet
    //data: given packet type
    INVALID_PACKET_INFORM,
    //request to create a lobby
    //data: name length(8b) + name(var)
    CREATE_LOBBY_REQUEST,
    //server approves lobby creation
    //data: lobby id(32b)
    CREATE_LOBBY_OK,
    //server failed to create a lobby
    //data: error code(8b)
    CREATE_LOBBY_FAIL,
    //request to connect to a lobby
    //data: lobby id(32b), name length(8b) + name(var)
    CONNECT_LOBBY_REQUEST,
    //server approves lobby connection
    //data: name length(8b) + name(var) of other player in lobby
    CONNECT_LOBBY_OK,
    //failed to connect to lobby
    //data: error code(8b)
    CONNECT_LOBBY_FAIL,
    //new player joined lobby
    //data: name length(8b) + name(var) of new player
    LOBBY_NEW_PLAYER,
    //request to start a game
    //data: none
    NEW_GAME_REQUEST,
    //request to start a game ok by server
    //data: none
    NEW_GAME_REQUEST_OK,
    //request to start a game rejected by server
    //data: error code(8b)
    NEW_GAME_REQUEST_FAIL,
    //other player requested to start a game
    //data: none
    NEW_GAME_REQUESTED,
    //approve other players' game start request
    //data: none
    NEW_GAME_ACCEPT,
    //accepting game start request ok
    //data: none
    NEW_GAME_ACCEPT_OK,
    //accepting game start request failed
    //data: error code(8b)
    NEW_GAME_ACCEPT_FAIL,
    //other player accepted game start request
    //data: none
    //game will now start
    NEW_GAME_ACCEPTED,
    //reject other players' game start request
    //data: none
    NEW_GAME_REJECT,
    //rejecting game start request ok
    //data: none
    NEW_GAME_REJECT_OK,
    //rejecting game start request failed
    //data: error code(8b)
    NEW_GAME_REJECT_FAIL,
    //other player rejected game start request
    //data: none
    NEW_GAME_REJECTED,
    //cancel new game request
    //data: none
    NEW_GAME_CANCEL,
    //canceling new game request ok
    //data: none
    NEW_GAME_CANCEL_OK,
    //canceling new game request failed
    //data: error code(8b)
    NEW_GAME_CANCEL_FAIL,
    //other player cancelled the game start request
    //data: none
    NEW_GAME_CANCELED,
    //disconnect from the lobby
    //data: none
    LOBBY_DISCONNECT,
    //other player disconnected from lobby
    //data: none
    LOBBY_DISCONNECT_OTHER,
    //lobby will timeout soon
    //data: seconds remaining(32b)
    LOBBY_TIMEOUT_WARNING,
    //lobby timed out 
    //data: none
    LOBBY_TIMEOUT,
    //new game is starting
    //data: player color(8b)
    NEW_GAME_STARTING,
    //place a token
    //data: column(8b), scene path length(32b) + scene path(var)
    GAME_ACTION_PLACE,
    //placing a token is ok
    //data: none
    GAME_ACTION_PLACE_OK,
    //placing a token failed
    //data: error code(8b)
    GAME_ACTION_PLACE_FAIL,
    //other player is placing a token
    //data: column(8b), scene path length(32b) + scene path(var)
    GAME_ACTION_PLACE_OTHER,
    //refill
    //data: none
    GAME_ACTION_REFILL,
    //refilling ok
    //data: none
    GAME_ACTION_REFILL_OK,
    //refilling failed
    //data: error code(8b)
    GAME_ACTION_REFILL_FAIL,
    //other player is refilling
    //date: none
    GAME_ACTION_REFILL_OTHER,
    //game finished
    //data: result(8b), player 1 points(32b), player 2 points(32b)
    GAME_FINISHED,
}
