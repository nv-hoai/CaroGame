using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class Client : MonoBehaviour
{
    [Header("Connection Settings")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5000;

    private TcpClient tcpClient;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;
    private CancellationTokenSource cancellationToken;

    // Game state
    public bool IsReady { get; private set; } = false;
    public bool BothReady { get; private set; } = false;
    public bool IsInMatch { get; private set; } = false;
    public bool IsMyTurn { get; private set; } = false;
    public string MyPlayerSymbol { get; private set; } = "";
    public string CurrentRoomId { get; private set; } = "";

    // Player info
    public PlayerInfo MyPlayerInfo { get; set; }
    public PlayerInfo OpponentInfo { get; set; }

    // Authentication state
    public bool IsAuthenticated { get; private set; } = false;
    public LoginResponse CurrentUser { get; private set; }
    public ProfileData CurrentProfile { get; private set; }

    // Events for message handling
    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnError;

    // Game-specific events
    public event Action<string> OnMatchFound; //roomId
    public event Action<string, string> OnGameStarted; // roomId, currentPlayer
    public event Action<string> OnTurnChanged; // currentPlayer
    public event Action<string, string> OnGameEnded; // reason, winner
    public event Action OnOpponentLeft;
    public event Action OnWaitingForOpponent;

    public event Action<string> OnUpdatePlayerNameSuccess;
    public event Action<string> OnUpdateAvatarSucess;
    public event Action<PlayerInfo> OnOpponentInfoReceived;
    public event Action<PlayerInfo> OnPlayerInfoReceived;
    public event Action<LoginResponse> OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action<string> OnRegisterSuccess;
    public event Action<string> OnRegisterFailed;
    public event Action<ProfileData> OnProfileDataReceived;
    public event Action<List<LeaderboardEntry>> OnLeaderboardReceived;
    public event Action<List<GameHistoryEntry>> OnGameHistoryReceived;
    public event Action<List<FriendData>> OnFriendsListReceived;
    public event Action<string> OnFriendRequestSent;
    public event Action<string> OnFriendRequestAccepted;
    public event Action<PlayerStatsData> OnPlayerStatsReceived;

    private void Start()
    {
        cancellationToken = new CancellationTokenSource();
    }

    public async Task<bool> ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(serverIP, serverPort);
            stream = tcpClient.GetStream();
            isConnected = true;

            // Start receiving messages on background thread
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log($"Connected to server at {serverIP}:{serverPort}");
            OnConnected?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to server: {e.Message}");
            OnError?.Invoke($"Connection failed: {e.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        try
        {
            isConnected = false;
            cancellationToken?.Cancel();

            receiveThread?.Join(1000);
            stream?.Close();
            tcpClient?.Close();

            Debug.Log("Disconnected from server");
            OnDisconnected?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during disconnect: {e.Message}");
        }
    }

    public new async Task<bool> SendMessage(string message)
    {
        if (!isConnected || stream == null)
        {
            Debug.LogWarning("Cannot send message: not connected to server");
            return false;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            Debug.Log($"Sent message: {message}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send message: {e.Message}");
            OnError?.Invoke($"Send failed: {e.Message}");
            return false;
        }
    }

    public async Task<bool> Login(string username, string password)
    {
        try
        {
            var loginRequest = new LoginRequest
            {
                Username = username,
                Password = password
            };
            string json = JsonUtility.ToJson(loginRequest);
            return await SendMessage($"LOGIN:{json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send login request: {e.Message}");
            return false;
        }
    }

    public async Task<bool> Register(string username, string password, string email, string playerName)
    {
        try
        {
            var registerRequest = new RegisterRequest
            {
                Username = username,
                Password = password,
                Email = email,
                PlayerName = playerName
            };
            string json = JsonUtility.ToJson(registerRequest);
            return await SendMessage($"REGISTER:{json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send register request: {e.Message}");
            return false;
        }
    }

    public async Task<bool> UpdatePlayerName(string playerName)
    {
        try
        {
            var nameUpdate = new UpdatePlayerNameRequest
            {
                ProfileId = CurrentUser.ProfileId,
                PlayerName = playerName
            };
            string json = JsonUtility.ToJson(nameUpdate);
            return await SendMessage($"UPDATE_PLAYER_NAME:{json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send player name update request: {e.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateAvatar(string avatarUrl)
    {
        try
        {
            var avatarUpdate = new UpdateAvatarRequest
            {
                ProfileId = CurrentUser.ProfileId,
                AvatarUrl = avatarUrl
            };
            string json = JsonUtility.ToJson(avatarUpdate);
            return await SendMessage($"UPDATE_AVATAR_URL:{json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send avatar update request: {e.Message}");
            return false;
        }
    }

    public async Task<bool> GetProfile()
    {
        return await SendMessage("GET_PROFILE");
    }

    public async Task<bool> GetLeaderboard()
    {
        return await SendMessage("GET_LEADERBOARD");
    }

    public async Task<bool> GetGameHistory()
    {
        return await SendMessage("GET_GAME_HISTORY");
    }

    public async Task<bool> GetPlayerStats(string playerName)
    {
        return await SendMessage($"GET_PLAYER_STATS:{playerName}");
    }

    public async Task<bool> GetFriends()
    {
        return await SendMessage("GET_FRIENDS");
    }

    public async Task<bool> SendFriendRequest(string playerName)
    {
        return await SendMessage($"SEND_FRIEND_REQUEST:{playerName}");
    }

    public async Task<bool> AcceptFriendRequest(int friendshipId)
    {
        return await SendMessage($"ACCEPT_FRIEND_REQUEST:{friendshipId}");
    }

    public async Task<bool> SendGameMove(int row, int col)
    {
        try
        {
            var moveData = new MoveData { row = row, col = col };
            string json = JsonUtility.ToJson(moveData);

            return await SendMessage($"GAME_MOVE:{json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send game move: {e.Message}");
            return false;
        }
    }

    public async Task<bool> FindMatch()
    {
        try
        {
            return await SendMessage("FIND_MATCH");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to request match: {e.Message}");
            return false;
        }
    }

    public async Task<bool> LeaveMatch()
    {
        try
        {
            return await SendMessage("LEAVE_MATCH");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to leave match: {e.Message}");
            return false;
        }
    }

    private void ReceiveMessages()
    {
        byte[] buffer = new byte[4096];
        StringBuilder messageBuilder = new StringBuilder();

        while (isConnected && !cancellationToken.Token.IsCancellationRequested)
        {
            try
            {
                if (stream != null && stream.CanRead)
                {
                    //Waits for data instead of polling
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        messageBuilder.Append(data);

                        string messages = messageBuilder.ToString();
                        string[] lines = messages.Split('\n');

                        for (int i = 0; i < lines.Length - 1; i++)
                        {
                            string message = lines[i].Trim();
                            if (!string.IsNullOrEmpty(message))
                            {
                                //Use thread-safe queue
                                MainThreadDispatcher.Enqueue(() => ProcessMessage(message));
                            }
                        }

                        messageBuilder.Clear();
                        if (lines.Length > 0)
                        {
                            messageBuilder.Append(lines[lines.Length - 1]);
                        }
                    }
                    else
                    {
                        // Connection closed
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                if (isConnected)
                {
                    Debug.LogError($"Error receiving message: {e.Message}");
                    MainThreadDispatcher.Enqueue(() => OnError?.Invoke($"Receive error: {e.Message}"));
                }
                break;
            }
        }
    }

    private void ProcessMessage(string message)
    {
        Debug.Log($"Received message: {message}");

        if (message.StartsWith("LOGIN_SUCCESS:"))
        {
            try
            {
                string json = message.Substring("LOGIN_SUCCESS:".Length);
                var loginResponse = JsonUtility.FromJson<LoginResponse>(json);

                CurrentUser = loginResponse;
                IsAuthenticated = true;

                // Update player info from login data
                MyPlayerInfo = new PlayerInfo
                {
                    PlayerId = loginResponse.ProfileId.ToString(),
                    PlayerName = loginResponse.PlayerName,
                    PlayerLevel = loginResponse.Level,
                    PlayerElo = loginResponse.Elo,
                    AvatarUrl = loginResponse.AvatarUrl
                };

                Debug.Log($"Login successful! Welcome {loginResponse.PlayerName}");
                OnLoginSuccess?.Invoke(loginResponse);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse login response: {e.Message}");
            }
        }
        else if (message.StartsWith("LOGIN_FAILED:"))
        {
            string errorMsg = message.Substring("LOGIN_FAILED:".Length);
            IsAuthenticated = false;

            Debug.LogWarning($"Login failed: {errorMsg}");
            OnLoginFailed?.Invoke(errorMsg);
        }
        else if (message.StartsWith("REGISTER_SUCCESS:"))
        {
            string successMsg = message.Substring("REGISTER_SUCCESS:".Length);

            Debug.Log($"Registration successful: {successMsg}");
            OnRegisterSuccess?.Invoke(successMsg);
        }
        else if (message.StartsWith("REGISTER_FAILED:"))
        {
            string errorMsg = message.Substring("REGISTER_FAILED:".Length);

            Debug.LogWarning($"Registration failed: {errorMsg}");
            OnRegisterFailed?.Invoke(errorMsg);
        }
        else if (message.StartsWith("PROFILE_DATA:"))
        {
            try
            {
                string json = message.Substring("PROFILE_DATA:".Length);
                var profileData = JsonUtility.FromJson<ProfileData>(json);

                MyPlayerInfo = new PlayerInfo
                {
                    PlayerId = profileData.ProfileId.ToString(),
                    PlayerName = profileData.PlayerName,
                    PlayerLevel = profileData.Level,
                    PlayerElo = profileData.Elo,
                    AvatarUrl = profileData.AvatarUrl
                };

                CurrentProfile = profileData;

                Debug.Log($"Profile data received for {profileData.PlayerName}");
                OnProfileDataReceived?.Invoke(profileData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse profile data: {e.Message}");
            }
        }
        else if (message.StartsWith("LEADERBOARD_DATA:"))
        {
            try
            {
                string json = message.Substring("LEADERBOARD_DATA:".Length);
                // Parse array of leaderboard entries
                var wrapper = JsonUtility.FromJson<LeaderboardWrapper>("{\"entries\":" + json + "}");

                Debug.Log($"Leaderboard received with {wrapper.entries.Count} players");
                OnLeaderboardReceived?.Invoke(wrapper.entries);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse leaderboard: {e.Message}");
            }
        }
        else if (message.StartsWith("UPDATE_PLAYER_NAME_SUCCESS:"))
        {
            string successMsg = message.Substring("UPDATE_PLAYER_NAME_SUCCESS:".Length);
            var respond = JsonUtility.FromJson<UpdatePlayerNameRespond>(successMsg);
            Debug.Log($"Player name update successful: {respond.Message}");
            OnUpdatePlayerNameSuccess?.Invoke(respond.NewPlayerName);
        }
        else if (message.StartsWith("UPDATE_AVATAR_URL_SUCCESS:"))
        {
            string successMsg = message.Substring("UPDATE_AVATAR_URL_SUCCESS:".Length);
            var respond = JsonUtility.FromJson<UpdateAvatarRespond>(successMsg);
            Debug.Log($"Avatar update successful: {respond.Message}");
            OnUpdateAvatarSucess?.Invoke(respond.NewAvatarUrl);
        }
        else if (message.StartsWith("GAME_HISTORY_DATA:"))
        {
            try
            {
                string json = message.Substring("GAME_HISTORY_DATA:".Length);
                var wrapper = JsonUtility.FromJson<GameHistoryWrapper>("{\"entries\":" + json + "}");

                Debug.Log($"Game history received with {wrapper.entries.Count} games");
                OnGameHistoryReceived?.Invoke(wrapper.entries);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse game history: {e.Message}");
            }
        }
        else if (message.StartsWith("PLAYER_STATS_DATA:"))
        {
            try
            {
                string json = message.Substring("PLAYER_STATS_DATA:".Length);
                var statsData = JsonUtility.FromJson<PlayerStatsData>(json);

                Debug.Log($"Player stats received for {statsData.PlayerName}");
                OnPlayerStatsReceived?.Invoke(statsData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse player stats: {e.Message}");
            }
        }
        else if (message.StartsWith("FRIENDS_DATA:"))
        {
            try
            {
                string json = message.Substring("FRIENDS_DATA:".Length);
                var wrapper = JsonUtility.FromJson<FriendsWrapper>("{\"friends\":" + json + "}");

                Debug.Log($"Friends list received with {wrapper.friends.Count} friends");
                OnFriendsListReceived?.Invoke(wrapper.friends);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse friends list: {e.Message}");
            }
        }
        else if (message.StartsWith("FRIEND_REQUEST_SENT:"))
        {
            string successMsg = message.Substring("FRIEND_REQUEST_SENT:".Length);

            Debug.Log($"Friend request sent: {successMsg}");
            OnFriendRequestSent?.Invoke(successMsg);
        }
        else if (message.StartsWith("FRIEND_REQUEST_ACCEPTED:"))
        {
            string successMsg = message.Substring("FRIEND_REQUEST_ACCEPTED:".Length);

            Debug.Log($"Friend request accepted: {successMsg}");
            OnFriendRequestAccepted?.Invoke(successMsg);
        }
        else if (message.StartsWith("GAME_MOVE:") || message.StartsWith("AI_MOVE:"))
        {
            try
            {
                if (message.StartsWith("AI_MOVE"))
                {
                    message = message.Replace("AI_MOVE:", "GAME_MOVE:");
                }
                string json = message.Substring("GAME_MOVE:".Length);
                var moveData = JsonUtility.FromJson<MoveData>(json);
                GameManager.Instance.GameMove(moveData.row, moveData.col);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse game move: {e.Message}");
            }
        }
        else if (message.StartsWith("PLAYER_INFO:"))
        {
            try
            {
                string json = message.Substring("PLAYER_INFO:".Length);
                var playerInfo = JsonUtility.FromJson<PlayerInfo>(json);

                MyPlayerInfo = playerInfo;

                Debug.Log($"Received player info: {playerInfo.PlayerName}");
                OnPlayerInfoReceived?.Invoke(playerInfo);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse player info: {e.Message}");
            }
        }
        else if (message.StartsWith("OPPONENT_INFO:"))
        {
            try
            {
                string json = message.Substring("OPPONENT_INFO:".Length);
                var opponentInfo = JsonUtility.FromJson<PlayerInfo>(json);

                OpponentInfo = opponentInfo;

                Debug.Log($"Received opponent info: {opponentInfo.PlayerName}");
                OnOpponentInfoReceived?.Invoke(opponentInfo);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse opponent info: {e.Message}");
            }
        }
        else if (message.StartsWith("MATCH_LEFT:"))
        {
            string roomId = message.Substring("MATCH_LEFT:".Length);
            CurrentRoomId = roomId;

            if (IsInMatch)
            {
                //Logic to handle leaving match
                IsInMatch = false;
            }

            Debug.Log($"Match left! Room ID: {roomId}");
        }
        else if (message.StartsWith("JOIN_ROOM:"))
        {
            string roomId = message.Substring("JOIN_ROOM:".Length);
            CurrentRoomId = roomId;

            Debug.Log($"Joined room! Room ID: {roomId}");
        }
        else if (message.StartsWith("MATCH_FOUND:") || message.StartsWith("AI_MATCH_FOUND:"))
        {
            if (message.StartsWith("AI_MATCH_FOUND:"))
            {
                message = message.Replace("AI_ROOM_CREATED:", "MATCH_FOUND:");
                PlayWithAI();
            }
            string roomId = message.Substring("MATCH_FOUND:".Length);
            
            OnMatchFound?.Invoke(roomId);

            Debug.Log($"Room is full! Getting ready to start!");
        }
        else if (message.StartsWith("READY_ACK:"))
        {
            string mes = message.Substring("READY_ACK:".Length);
            IsReady = true;

            Debug.Log($"{mes}");
        }
        else if (message.StartsWith("BOTH_READY:"))
        {
            string mes = message.Substring("BOTH_READY:".Length);
            BothReady = true;

            Debug.Log($"{mes}");
        }
        else if (message.StartsWith("PLAYER_SYMBOL:"))
        {
            string symbol = message.Substring("PLAYER_SYMBOL:".Length);

            SetPlayerSymbol(symbol);

            Debug.Log($"Server assigned me symbol: {symbol}");
        }
        else if (message.StartsWith("GAME_START:"))
        {
            try
            {
                string json = message.Substring("GAME_START:".Length);
                var gameStart = JsonUtility.FromJson<GameStartData>(json);

                IsInMatch = true;
                CurrentRoomId = gameStart.roomId;
                IsMyTurn = (gameStart.currentPlayer == MyPlayerSymbol);

                Debug.Log($"Game started! Room: {gameStart.roomId}, Current player: {gameStart.currentPlayer}");
                StartCoroutine(InvokeAfterFrame(gameStart));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse game start: {e.Message}");
            }
        }
        else if (message.StartsWith("TURN_CHANGE:"))
        {
            try
            {
                string json = message.Substring("TURN_CHANGE:".Length);
                var turnChange = JsonUtility.FromJson<TurnChangeData>(json);

                IsMyTurn = (turnChange.currentPlayer == MyPlayerSymbol);

                Debug.Log($"Turn changed to: {turnChange.currentPlayer} (My turn: {IsMyTurn})");
                OnTurnChanged?.Invoke(turnChange.currentPlayer);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse turn change: {e.Message}");
            }
        }
        else if (message.StartsWith("GAME_END:"))
        {
            try
            {
                string json = message.Substring("GAME_END:".Length);
                var gameEnd = JsonUtility.FromJson<GameEndData>(json);

                IsInMatch = false;
                IsMyTurn = false;

                Debug.Log($"Game ended! Reason: {gameEnd.reason}, Winner: {gameEnd.winner}");
                OnGameEnded?.Invoke(gameEnd.reason, gameEnd.winner);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse game end: {e.Message}");
            }
        }
        else if (message == "OPPONENT_LEFT:Your opponent left the game")
        {
            IsInMatch = false;
            IsMyTurn = false;

            Debug.Log("Opponent left the game");
            OnOpponentLeft?.Invoke();
        }
        else if (message.StartsWith("WAITING_FOR_OPPONENT:"))
        {
            Debug.Log("Waiting for opponent to join...");
            OnWaitingForOpponent?.Invoke();
        }
        else if (message == "PLAYER_INFO_ACK:Player info received")
        {
            Debug.Log("Server acknowledged player info");
        }
        else if (message == "MATCH_LEFT:You left the match")
        {
            IsInMatch = false;
            IsMyTurn = false;
            CurrentRoomId = "";

            Debug.Log("Successfully left the match");
        }
        else if (message.StartsWith("ERROR:"))
        {
            string errorMsg = message.Substring("ERROR:".Length);

            Debug.LogError($"Server error: {errorMsg}");
            OnError?.Invoke(errorMsg);
        }

        OnMessageReceived?.Invoke(message);
    }

    public void SetPlayerSymbol(string symbol)
    {
        MyPlayerSymbol = symbol;

        Debug.Log($"My player symbol set to: {symbol}");
    }

    

    IEnumerator InvokeAfterFrame(GameStartData gameStart)
    {
        yield return null; // Wait for the next frame
        yield return null; // Ensure all UI updates are done
        OnGameStarted?.Invoke(gameStart.roomId, gameStart.currentPlayer);
    }

    private void PlayWithAI()
    {
        PlayerInfo aiPlayer = new PlayerInfo
        {
            PlayerId = "AI_01",
            PlayerName = "AI",
            PlayerLevel = 1,
            PlayerElo = 1000,
            AvatarUrl = "cat"
        };

        OpponentInfo = aiPlayer;
        OnOpponentInfoReceived?.Invoke(aiPlayer);
    }

    public async Task ServerDiscovery()
    {
        int udpPort = 5001;
        var udp = new UdpClient() { EnableBroadcast = true };

        var msg = Encoding.UTF8.GetBytes("DISCOVER");
        await udp.SendAsync(msg, msg.Length, new IPEndPoint(IPAddress.Broadcast, udpPort));
        Console.WriteLine("Broadcast sent.");

        var result = await udp.ReceiveAsync();  // chờ phản hồi
        string reply = Encoding.UTF8.GetString(result.Buffer);
        udp.Close(); // tắt discovery ngay sau khi nhận được

        var parts = reply.Split(':');
        IPAddress ip = IPAddress.Parse(parts[0]);
        int port = int.Parse(parts[1]);

        serverIP = ip.ToString();
        serverPort = port;

        Debug.Log($"Found server at {ip} : {port}");

        await ConnectToServer();
    }

    public bool IsConnected => isConnected;

    private void OnDestroy()
    {
        Disconnect();
        cancellationToken?.Dispose();
    }
}




