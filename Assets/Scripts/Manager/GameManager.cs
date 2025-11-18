using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public Client Client { get; private set; }
    public TMP_Dropdown graphicQualityDropdown;
    public Board board;

    public Dictionary<string, Sprite> avatarSprites = new();
    private int[,] resolutions = new int[4, 2]
    {
        { 1280, 720 },
        { 1366, 768 },
        { 1600, 900 },
        { 1920, 1080 }
    };

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
        Application.runInBackground = true;
        Client = GetComponent<Client>();

        if (Client != null)
        {
            try
            {
                await Client.ConnectToServer();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during connection or data exchange: {ex.Message}");
            }

            Client.OnLogoutSuccess += () =>
            {
                SceneManager.LoadScene("LoginScene");
            };
        }
        else
        {
            Debug.LogError("Client component not found on GameManager.");
        }

        Addressables.LoadAssetsAsync<Sprite>("avatarIcon", (sprite) =>
        {
            avatarSprites[sprite.name] = sprite;
        }).Completed += handle =>
        {
            Debug.Log("All avatars ready!");
        };
    }

    public void GameMove(int row, int col)
    {
        if (!board) return;
        board.MakeMove(row, col);
    }

    public void SetTMPDropdown(TMP_Dropdown dropdown)
    {
        graphicQualityDropdown = dropdown;
        graphicQualityDropdown.onValueChanged.AddListener(delegate { ChangeResolution(); });
    }

    public void ChangeResolution()
    {
        bool isFullScreen = graphicQualityDropdown.value == 3;
        Screen.SetResolution(resolutions[graphicQualityDropdown.value, 0], 
                resolutions[graphicQualityDropdown.value, 1], isFullScreen);
    }

}
