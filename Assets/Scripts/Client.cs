using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

    public async Task<bool> SendPlayerInfo(PlayerInfo playerInfo)
    {
        try
        {
            string json = JsonUtility.ToJson(playerInfo);
            
            return await SendMessage($"PLAYER_INFO:{json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send player info: {e.Message}");
            return false;
        }
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

        if (message.StartsWith("GAME_MOVE:") || message.StartsWith("AI_MOVE:"))
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

                Debug.Log($"Received player info: {playerInfo.playerName}");
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

                Debug.Log($"Received opponent info: {opponentInfo.playerName}");
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
            }
            string roomId = message.Substring("MATCH_FOUND:".Length);
            PlayWithAI();
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
                OnGameStarted?.Invoke(gameStart.roomId, gameStart.currentPlayer);
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

    private void PlayWithAI()
    {
        PlayerInfo aiPlayer = new PlayerInfo
        {
            playerId = "AI_01",
            playerName = "AI_Opponent",
            playerLevel = 1,
            playerElo = 1000
        };
        OpponentInfo = aiPlayer;
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

public static class MainThreadDispatcher
{
    private static readonly ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
    private static volatile bool hasActions = false;

    public static void Enqueue(Action action)
    {
        actions.Enqueue(action);
        hasActions = true;
    }

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
        var go = new GameObject("MainThreadDispatcher");
        go.AddComponent<MainThreadDispatcherBehaviour>();
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    private class MainThreadDispatcherBehaviour : MonoBehaviour
    {
        private void Update()
        {
            if (hasActions)
            {
                while (actions.TryDequeue(out Action action))
                {
                    action?.Invoke();
                }
                hasActions = false;
            }
        }
    }
}
