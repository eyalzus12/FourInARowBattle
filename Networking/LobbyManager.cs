using Godot;
using System.Collections.Generic;

namespace FourInARowBattle;

public class LobbyManager
{
    public sealed class Player
    {
        public int Id{get; set;}
        public string Name{get; set;} = null!;
        public uint? Lobby{get; set;} = null;
    }

    public sealed class Lobby
    {
        public uint Id{get; set;}
        public bool ActiveRequest{get; set;} = false;
        public bool InGame{get; set;} = false;
        public int? Player1{get; set;} = null;
        public int? Player2{get; set;} = null;
    }

    private Dictionary<int, Player> _players{get; set;} = new();
    private Dictionary<uint, Lobby> _lobbies{get; set;} = new();

    public LobbyManagerErrorEnum RegisterNewPlayer(int id, string name)
    {
        if(_players.ContainsKey(id))
        {
            //GD.PushError($"Cannot register new player with id {id} as it already exists");
            return LobbyManagerErrorEnum.PLAYER_ALREADY_EXISTS;
        }
        _players[id] = new()
        {
            Id = id,
            Name = name,
            Lobby = null
        };
        return LobbyManagerErrorEnum.NONE;
    }

    public LobbyManagerErrorEnum RemovePlayer(int id)
    {
        if(!_players.ContainsKey(id))
        {
            //GD.PushError($"Cannot remove player with id {id} as it does not exist");
            return LobbyManagerErrorEnum.PLAYER_DOES_NOT_EXIST;
        }
        _players.Remove(id);
        return LobbyManagerErrorEnum.NONE;
    }

    public uint GetAvailableLobbyId()
    {
        uint id;
        do {id = GD.Randi();} while(_lobbies.ContainsKey(id));
        return id;
    }

    public LobbyManagerErrorEnum CreateNewLobby(uint id)
    {
        if(_lobbies.ContainsKey(id))
        {
            //GD.PushError($"Cannot create lobby with id {id} as it already exists");
            return LobbyManagerErrorEnum.LOBBY_ALREADY_EXISTS;
        }
        _lobbies[id] = new()
        {
            Id = id,
            ActiveRequest = false,
            InGame = false,
            Player1 = null,
            Player2 = null
        };
        return LobbyManagerErrorEnum.NONE;
    }

    public LobbyManagerErrorEnum RemoveLobby(uint id, out int? player1, out int? player2)
    {
        player1 = null;
        player2 = null;
        if(!_lobbies.TryGetValue(id, out Lobby? lobby))
        {
            //GD.PushError($"Cannot remove lobby with id {id} as it does not exist");
            return LobbyManagerErrorEnum.LOBBY_DOES_NOT_EXIST;
        }
        if(lobby.Player1 is not null)
        {
            if(_players.TryGetValue(lobby.Player1??0, out Player? p1))
                p1.Lobby = null;
            if(_players.TryGetValue(lobby.Player2??0, out Player? p2))
                p2.Lobby = null;
        }
        player1 = lobby.Player1;
        player2 = lobby.Player2;
        _lobbies.Remove(id);
        return LobbyManagerErrorEnum.NONE;
    }

    public LobbyManagerErrorEnum MakeRequest(int playerId, out int? otherPlayer)
    {
        otherPlayer = null;
        if(!_players.TryGetValue(playerId, out Player? player))
            return LobbyManagerErrorEnum.PLAYER_DOES_NOT_EXIST;
        uint? lobbyId = player.Lobby;
        if(lobbyId is null)
            return LobbyManagerErrorEnum.PLAYER_NOT_IN_LOBBY;
        Lobby lobby = _lobbies[lobbyId??0];
        if(lobby.ActiveRequest)
            return LobbyManagerErrorEnum.REQUEST_ALREADY_EXISTS;
        if(lobby.InGame)
            return LobbyManagerErrorEnum.MID_GAME;
        if(lobby.Player2 is null)
            return LobbyManagerErrorEnum.NO_OTHER_PLAYER;
        lobby.ActiveRequest = true;
        otherPlayer = (lobby.Player1 == playerId) ? lobby.Player2 : lobby.Player1;
        return LobbyManagerErrorEnum.NONE;
    }

    public LobbyManagerErrorEnum ConsumeRequest(int playerId, out int? otherPlayer)
    {
        otherPlayer = null;
        if(!_players.TryGetValue(playerId, out Player? player))
            return LobbyManagerErrorEnum.PLAYER_DOES_NOT_EXIST;
        uint? lobbyId = player.Lobby;
        if(lobbyId is null)
            return LobbyManagerErrorEnum.PLAYER_NOT_IN_LOBBY;
        Lobby lobby = _lobbies[lobbyId??0];
        if(!lobby.ActiveRequest)
            return LobbyManagerErrorEnum.REQUEST_DOES_NOT_EXIST;
        lobby.ActiveRequest = false;
        otherPlayer = (lobby.Player1 == playerId) ? lobby.Player2 : lobby.Player1;
        return LobbyManagerErrorEnum.NONE;
    }

    public LobbyManagerErrorEnum GetPlayerOutOfLobby(int playerId, out int? other)
    {
        other = null;
        if(!_players.TryGetValue(playerId, out Player? player))
        {
            //GD.PushError($"Cannot remove player {playerId} from lobby as that player does not exist");
            return LobbyManagerErrorEnum.PLAYER_DOES_NOT_EXIST;
        }
        if(player.Lobby is null)
        {
            //GD.PushError($"Cannot remove player {playerId} from lobby as that player is not in a lobby");
            return LobbyManagerErrorEnum.PLAYER_NOT_IN_LOBBY;
        }
        uint lobbyId = player.Lobby??0;
        if(_lobbies.TryGetValue(lobbyId, out Lobby? lobby))
        {
            if(lobby.Player1 == playerId)
            {
                other = lobby.Player2;
                lobby.Player1 = lobby.Player2;
                lobby.Player2 = null;
                player.Lobby = null;
            }
            else if(lobby.Player2 == playerId)
            {
                other = lobby.Player1;
                lobby.Player2 = null;
                player.Lobby = null;
            }

            lobby.ActiveRequest = false;
            lobby.InGame = false;

            if(lobby.Player1 is null && lobby.Player2 is null)
                RemoveLobby(lobby.Id, out int? _1, out int? _2);
        }
        return LobbyManagerErrorEnum.NONE;
    }

    public LobbyManagerErrorEnum AddPlayerToLobby(int playerId, uint lobbyId, out string? other)
    {
        other = null;
        if(!_players.TryGetValue(playerId, out Player? player))
        {
            //GD.PushError($"Cannot add player {playerId} to lobby {lobbyId} as that player does not exist");
            return LobbyManagerErrorEnum.PLAYER_DOES_NOT_EXIST;
        }
        if(player.Lobby is not null)
        {
            //GD.PushError($"Cannot add player {playerId} to lobby {lobbyId} as that player is already in a lobby");
            return LobbyManagerErrorEnum.PLAYER_ALREADY_IN_LOBBY;
        }
        if(!_lobbies.TryGetValue(lobbyId, out Lobby? lobby))
        {
            //GD.PushError($"Cannot add player {playerId} to lobby {lobbyId} as that lobby does not exist");
            return LobbyManagerErrorEnum.LOBBY_DOES_NOT_EXIST;
        }
        if(lobby.Player1 is not null && lobby.Player2 is not null)
        {
            //GD.PushError($"Cannot add player {playerId} to lobby {lobbyId} as that lobby is full");
            return LobbyManagerErrorEnum.LOBBY_IS_FULL;
        }
        if(lobby.Player1 == playerId || lobby.Player2 == playerId)
        {
            //GD.PushError($"Cannot add player {playerId} to lobby {lobbyId} as that player is alreay in the lobby");
            return LobbyManagerErrorEnum.PLAYER_ALREADY_IN_THAT_LOBBY;
        }
        if(lobby.Player1 is null)
        {
            lobby.Player1 = playerId;
            player.Lobby = lobbyId;
            other = null;
        }
        else if(lobby.Player2 is null)
        {
            lobby.Player2 = playerId;
            player.Lobby = lobbyId;
            other = _players[lobby.Player1??0].Name;
        }
        return LobbyManagerErrorEnum.NONE;
    }

    public uint? GetPlayerLobby(int playerId)
    {
        if(!_players.TryGetValue(playerId, out Player? player))
        {
            //GD.PushError($"Cannot get lobby for player {playerId} as that player does not exist");
            return null;
        }
        return player.Lobby;
    }
}