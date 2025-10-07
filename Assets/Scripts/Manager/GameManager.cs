using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public Client Client { get; private set; }
    public Board board;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        Client = GetComponent<Client>();

        if (Client != null)
        {
            try
            {
                await Client.ConnectToServer();

                //Sample data, later to send account info and receive player data
                Client.MyPlayerInfo = new PlayerInfo
                {
                    playerId = "VN01",
                    playerName = "PlayerOne",
                    playerLevel = 1,
                    playerElo = 1000
                };

                await Client.SendPlayerInfo(Client.MyPlayerInfo);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during connection or data exchange: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("Client component not found on GameManager.");
        }
    }

    public void GameMove(int row, int col)
    {
        board.GetComponent<Board>().MakeMove(row, col);
    }

}
