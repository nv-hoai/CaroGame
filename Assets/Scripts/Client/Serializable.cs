using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MoveData
{
    public int row;
    public int col;
}

[System.Serializable]
public class GameStartData
{
    public string roomId;
    public string currentPlayer;
}

[System.Serializable]
public class ChatMessageData
{
    public string Sender;
    public string Message;
    public Time Timestamp;
}

[System.Serializable]
public class TurnChangeData
{
    public string currentPlayer;
}

[System.Serializable]
public class GameEndData
{
    public string reason;
    public string winner;
}

[System.Serializable]
public class PlayerInfo
{
    public string PlayerId;
    public string PlayerName;
    public int PlayerLevel;
    public int PlayerElo;
    public string AvatarUrl;
}

[System.Serializable]
public class LoginRequest
{
    public string Username;
    public string Password;
}

[System.Serializable]
public class RegisterRequest
{
    public string Username;
    public string Password;
    public string Email;
    public string PlayerName;
}

[System.Serializable]
public class UpdatePlayerNameRequest
{
    public int ProfileId;
    public string PlayerName;
}

[System.Serializable]
public class UpdateAvatarRequest
{
    public int ProfileId;
    public string AvatarUrl;
}

[System.Serializable]
public class UpdatePlayerNameRespond
{
    public int ProfileId;
    public string NewPlayerName;
    public string Message;
}

[System.Serializable]
public class UpdateAvatarRespond
{
    public int ProfileId;
    public string NewAvatarUrl;
    public string Message;
}

[System.Serializable]
public class LoginResponse
{
    public int UserId;
    public string Username;
    public int ProfileId;
    public string PlayerName;
    public int Elo;
    public int Level;
    public int Wins;
    public int Losses;
    public int TotalGames;
    public string AvatarUrl;
}

[System.Serializable]
public class ProfileData
{
    public int ProfileId;
    public string PlayerName;
    public int Elo;
    public int Level;
    public int TotalGames;
    public int Wins;
    public int Losses;
    public int Draws;
    public double WinRate;
    public string Bio;
    public string AvatarUrl;
    public int Rank;
    public bool IsOnline;
}

[System.Serializable]
public class LeaderboardEntry
{
    public int Rank;
    public int ProfileId;
    public string PlayerName;
    public int Elo;
    public int Level;
    public int Wins;
    public int TotalGames;
    public double WinRate;
    public string AvatarUrl;
}

[System.Serializable]
public class GameHistoryEntry
{
    public int GameId;
    public string Player1Name;
    public string Player2Name;
    public bool IsAIGame;
    public string GameResult;
    public string WinnerName;
    public int TotalMoves;
    public double GameDuration;
    public int EloChange;
    public string PlayedAt;
}

[System.Serializable]
public class FriendData
{
    public int ProfileId;
    public string PlayerName;
    public int Elo;
    public int Level;
    public int Wins;
    public int TotalGames;
    public string AvatarUrl;
    public string Status;
}

[System.Serializable]
public class FriendRequestData
{
    public int FriendshipId;
    public int ProfileId;
    public string PlayerName;
    public string AvatarUrl;
    public string RequestedAt;
}

[System.Serializable]
public class PlayerStatsData
{
    public string PlayerName;
    public int Elo;
    public int Level;
    public int TotalGames;
    public int Wins;
    public int Losses;
    public int Draws;
    public double WinRate;
    public int Rank;
}

[System.Serializable]
public class LeaderboardWrapper
{
    public List<LeaderboardEntry> entries;
}

[System.Serializable]
public class GameHistoryWrapper
{
    public List<GameHistoryEntry> entries;
}

[System.Serializable]
public class FriendsWrapper
{
    public List<FriendData> friends;
}

[System.Serializable]
public class FriendRequestsWrapper
{
    public List<FriendRequestData> requests;
}

[System.Serializable]
public class ProfileDataWrapper
{
    public List<ProfileData> profiles;
}