using UnityEngine;
using System;

public class WaitingPanel : MonoBehaviour
{
    private Action onDisconnectedHandler;
    private Action onConnectedHandler;

    void Start()
    {
        CanvasManager.Instance.panelDict.Add("WaitingPanel", gameObject);
        onConnectedHandler = () => { gameObject.SetActive(false); };
        onDisconnectedHandler = () => { gameObject.SetActive(true); };
        GameManager.Instance.Client.OnConnected += onConnectedHandler;
        GameManager.Instance.Client.OnDisconnected += onDisconnectedHandler;


        if (GameManager.Instance.Client.IsConnected) 
            gameObject.SetActive(false);
        else 
            gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("WaitingPanel");
        GameManager.Instance.Client.OnDisconnected -= onDisconnectedHandler;
        GameManager.Instance.Client.OnConnected -= onConnectedHandler;
    }
}
