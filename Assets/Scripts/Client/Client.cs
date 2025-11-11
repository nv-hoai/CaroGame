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
    public string serverIP = "192.168.195.126";
    public string serverIPFallback = "192.168.195.69";
    public int serverPort = 5000;

    private IPAddress[] serverAddresses;

    [Header("Auto-Reconnect Settings")]
    public bool enableAutoReconnect = true;
    public float reconnectDelay = 5f;
    public float maxReconnectDelay = 60f;
    public int maxReconnectAttempts = -1;

    private TcpClient tcpClient;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;
    private bool isManuallyDisconnected = false;
    private CancellationTokenSource cancellationToken;
    private Coroutine reconnectCoroutine;
    private float currentReconnectDelay;
    private int reconnectAttempts;
    private byte[] sessionKey;
    private bool isEncryptionEnabled = false;


    public bool IsReady { get; private set; } = false;
    public bool BothReady { get; private set; } = false;
    public bool IsInMatch { get; private set; } = false;
    public bool IsMyTurn { get; private set; } = false;
    public string MyPlayerSymbol { get; private set; } = "";
    public string CurrentRoomId { get; private set; } = "";


    public PlayerInfo MyPlayerInfo { get; set; }
    public PlayerInfo OpponentInfo { get; set; }


    public bool IsAuthenticated { get; private set; } = false;
    public LoginResponse CurrentUser { get; private set; }
    public ProfileData CurrentProfile { get; private set; }


    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnError;


    public event Action<string> OnMatchFound;
    public event Action<string, string> OnGameStarted;
    public event Action<string> OnTurnChanged;
    public event Action<string, string> OnGameEnded;
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
    public event Action<List<FriendRequestData>> OnFriendRequestsReceived;
    public event Action<string> OnFriendRequestSent;
    public event Action<string> OnFriendRequestAccepted;
    public event Action<PlayerStatsData> OnPlayerStatsReceived;
    public event Action<ChatMessageData> OnChatMessageReceived;
    public event Action<MoveData> OnMoveReceived;
    public event Action OnLogoutSuccess;
    public event Action<List<ProfileData>> OnPlayerSearchResultsReceived;

    private void Start()
    {
        cancellationToken = new CancellationTokenSource();
        currentReconnectDelay = reconnectDelay;
        reconnectAttempts = 0;

        serverAddresses = new IPAddress[2];
        if (IPAddress.TryParse(serverIP, out IPAddress primaryAddress))
        {
            serverAddresses[0] = primaryAddress;
        }
        else
        {
            Debug.LogError($"Invalid primary server IP: {serverIP}");
        }

        if (IPAddress.TryParse(serverIPFallback, out IPAddress fallbackAddress))
        {
            serverAddresses[1] = fallbackAddress;
        }
        else
        {
            Debug.LogError($"Invalid fallback server IP: {serverIPFallback}");
        }
    }

    public async Task<bool> ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(serverAddresses, serverPort);
            stream = tcpClient.GetStream();
            isConnected = true;
            isManuallyDisconnected = false;
            reconnectAttempts = 0;
            currentReconnectDelay = reconnectDelay;

            await InitializeEncryption();

            if (reconnectCoroutine != null)
            {
                StopCoroutine(reconnectCoroutine);
                reconnectCoroutine = null;
            }


            receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log($"Connected to server at {serverIP} | {serverIPFallback}:{serverPort}");
            OnConnected?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to connect to server: {e.Message}");
            OnError?.Invoke($"Connection failed: {e.Message}");


            if (enableAutoReconnect && !isManuallyDisconnected)
            {
                reconnectAttempts++;
                if (reconnectCoroutine != null)
                {
                    StopCoroutine(reconnectCoroutine);
                }
                reconnectCoroutine = StartCoroutine(AttemptReconnect());
            }

            return false;
        }
    }

    private async Task InitializeEncryption()
    {
        try
        {
            // Step 1: Request public key
            await SendRawMessage("GET_PUBLIC_KEY");

            // Step 2: Receive public key (wait for response)
            string response = await ReceiveRawMessage();

            if (response.StartsWith("PUBLIC_KEY:"))
            {
                string json = response.Substring("PUBLIC_KEY:".Length);
                var data = JsonUtility.FromJson<PublicKeyResponse>(json);

                // Step 3: Generate session key (32 bytes for AES-256)
                sessionKey = CryptoUtil.GenerateRandomBytes(32);

                // Step 4: Encrypt session key with RSA public key
                byte[] encryptedSessionKey = CryptoUtil.RsaEncrypt(sessionKey, data.PublicKey);
                string encryptedBase64 = CryptoUtil.ToBase64(encryptedSessionKey);

                // Step 5: Send encrypted session key to server
                await SendRawMessage($"SET_SESSION_KEY:{encryptedBase64}");

                // Step 6: Wait for acknowledgment
                string ack = await ReceiveRawMessage();

                if (ack.StartsWith("SESSION_KEY_ACK:"))
                {
                    isEncryptionEnabled = true;
                    Debug.Log("Encryption enabled successfully!");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Encryption initialization failed: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        try
        {
            isManuallyDisconnected = true;
            isConnected = false;
            cancellationToken?.Cancel();


            if (reconnectCoroutine != null)
            {
                StopCoroutine(reconnectCoroutine);
                reconnectCoroutine = null;
            }

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

    public async Task Logout()
    {
        IsAuthenticated = false;
        CurrentUser = null;
        CurrentProfile = null;
        MyPlayerInfo = null;
        OpponentInfo = null;
        IsReady = false;
        BothReady = false;
        IsInMatch = false;
        IsMyTurn = false;
        MyPlayerSymbol = "";
        CurrentRoomId = "";
        ResetEvents();
        await SendMessage("LOGOUT");
    }

    private void ResetEvents()
    {
        OnMessageReceived = null;
        OnConnected = null;
        OnDisconnected = null;
        OnError = null;


        OnMatchFound = null;
        OnGameStarted = null;
        OnTurnChanged = null;
        OnGameEnded = null;
        OnOpponentLeft = null;
        OnWaitingForOpponent = null;

        OnUpdatePlayerNameSuccess = null;
        OnUpdateAvatarSucess = null;
        OnOpponentInfoReceived = null;
        OnPlayerInfoReceived = null;
        OnLoginSuccess = null;
        OnLoginFailed = null;
        OnRegisterSuccess = null;
        OnRegisterFailed = null;
        OnProfileDataReceived = null;
        OnLeaderboardReceived = null;
        OnGameHistoryReceived = null;
        OnFriendsListReceived = null;
        OnFriendRequestsReceived = null;
        OnFriendRequestSent = null;
        OnFriendRequestAccepted = null;
        OnPlayerStatsReceived = null;
        OnChatMessageReceived = null;
        OnMoveReceived = null;
        OnPlayerSearchResultsReceived = null;
    }

    // Send unencrypted message (for handshake only)
    private async Task SendRawMessage(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        await stream.WriteAsync(data, 0, data.Length);
        await stream.FlushAsync();
        Debug.Log($"Sent: {message}");
    }

    // Receive unencrypted message (for handshake only)
    private async Task<string> ReceiveRawMessage()
    {
        byte[] buffer = new byte[4096];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
        Debug.Log($"Received: {message}");
        return message;
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
            string messageToSend = message;

            if (isEncryptionEnabled && sessionKey != null)
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(message);
                byte[] encryptedBytes = CryptoUtil.AesEncrypt(plainBytes, sessionKey);
                string encryptedBase64 = CryptoUtil.ToBase64(encryptedBytes);
                messageToSend = $"ENC:{encryptedBase64}";
                Debug.Log($"Sending encrypted: {message}");
            }

            byte[] data = Encoding.UTF8.GetBytes(messageToSend + "\n");
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
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

    public async Task<bool> SendChat(string message)
    {
        try
        {
            return await SendMessage($"CHAT:{message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send chat messaege: {e.Message}");
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

    public async Task<bool> GetFriendRequests()
    {
        return await SendMessage("GET_FRIEND_REQUESTS");
    }

    public async Task<bool> SendFriendRequest(int profileId)
    {
        return await SendMessage($"SEND_FRIEND_REQUEST:{profileId}");
    }

    public async Task<bool> AcceptFriendRequest(int friendshipId)
    {
        return await SendMessage($"ACCEPT_FRIEND_REQUEST:{friendshipId}");
    }

    public async Task<bool> RejectFriendRequest(int friendshipId)
    {
        return await SendMessage($"REJECT_FRIEND_REQUEST:{friendshipId}");
    }
    
    public async Task<bool> SearchPlayerByName(string playerName)
    {
        return await SendMessage($"SEARCH_PLAYER:{playerName}");
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
                                // Decrypt message if encrypted
                                string decryptedMessage = DecryptMessageIfNeeded(message);

                                // Process on main thread
                                MainThreadDispatcher.Enqueue(() => ProcessMessage(decryptedMessage));
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
                        Debug.LogWarning("Server closed the connection");
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

        // Auto-reconnect logic
        if (isConnected && !isManuallyDisconnected && enableAutoReconnect)
        {
            Debug.LogWarning("Connection lost. Attempting to reconnect...");
            MainThreadDispatcher.Enqueue(() =>
            {
                isConnected = false;
                OnDisconnected?.Invoke();
                reconnectAttempts++;
                if (reconnectCoroutine != null)
                {
                    StopCoroutine(reconnectCoroutine);
                }
                reconnectCoroutine = StartCoroutine(AttemptReconnect());
            });
        }
    }

    // Helper method to decrypt messages
    private string DecryptMessageIfNeeded(string message)
    {
        try
        {
            // Skip decryption for handshake messages
            if (!isEncryptionEnabled || sessionKey == null)
            {
                return message;
            }

            // Check if message is encrypted
            if (message.StartsWith("ENC:"))
            {
                string encryptedData = message.Substring("ENC:".Length);
                byte[] encryptedBytes = CryptoUtil.FromBase64(encryptedData);
                byte[] decryptedBytes = CryptoUtil.AesDecrypt(encryptedBytes, sessionKey);
                string decryptedMessage = Encoding.UTF8.GetString(decryptedBytes);

                Debug.Log($"[Decrypted] {decryptedMessage}");
                return decryptedMessage;
            }

            // Message is not encrypted (during handshake)
            return message;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Decryption failed: {ex.Message}");
            return message; // Return original on error
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
        else if (message.StartsWith("LOGOUT_SUCCESS:"))
        {
            OnLogoutSuccess?.Invoke();
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
        else if (message.StartsWith("FRIEND_REQUESTS_DATA:"))
        {
            try
            {
                string json = message.Substring("FRIEND_REQUESTS_DATA:".Length);
                var wrapper = JsonUtility.FromJson<FriendRequestsWrapper>("{\"requests\":" + json + "}");
                Debug.Log($"Friend requests received with {wrapper.requests.Count} requests");
                OnFriendRequestsReceived?.Invoke(wrapper.requests);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse friend requests: {e.Message}");
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
                OnMoveReceived?.Invoke(moveData);
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
        else if (message.StartsWith("CHAT:"))
        {
            string json = message.Substring("CHAT:".Length);
            var chat = JsonUtility.FromJson<ChatMessageData>(json);
            OnChatMessageReceived?.Invoke(chat);
            Debug.Log($"Chat message received from {chat.Sender}: {chat.Message}");
        }
        else if (message.StartsWith("ERROR:"))
        {
            string errorMsg = message.Substring("ERROR:".Length);

            Debug.LogError($"Server error: {errorMsg}");
            OnError?.Invoke(errorMsg);
        }
        else if (message.StartsWith("SEARCH_RESULTS:"))
        {
            try
            {
                string json = message.Substring("SEARCH_RESULTS:".Length);
                var wrapper = JsonUtility.FromJson<ProfileDataWrapper>("{\"profiles\":" + json + "}");
                Debug.Log($"Player search results received with {wrapper.profiles.Count} players");
                OnPlayerSearchResultsReceived?.Invoke(wrapper.profiles);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse player search results: {e.Message}");
            }
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
        yield return null;
        yield return null;
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

    public bool IsConnected => isConnected;

    private IEnumerator AttemptReconnect()
    {
        while (!isManuallyDisconnected && enableAutoReconnect)
        {

            if (maxReconnectAttempts > 0 && reconnectAttempts > maxReconnectAttempts)
            {
                Debug.LogError($"Max reconnection attempts ({maxReconnectAttempts}) reached. Giving up.");
                OnError?.Invoke("Max reconnection attempts reached");
                yield break;
            }

            Debug.Log($"Attempting to reconnect... (Attempt {reconnectAttempts}, waiting {currentReconnectDelay}s)");


            yield return new WaitForSeconds(currentReconnectDelay);

            var task = ConnectToServer();
            while (!task.IsCompleted)
            {
                yield return null;
            }

            bool connected = task.Result;

            if (connected)
            {
                Debug.Log("Reconnected successfully!");
                reconnectCoroutine = null;
                yield break;
            }


            currentReconnectDelay = Mathf.Min(currentReconnectDelay * 2f, maxReconnectDelay);
        }

        reconnectCoroutine = null;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == gameObject.GetComponent<GameManager>()) Disconnect();
        cancellationToken?.Dispose();
    }
}




