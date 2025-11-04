using UnityEngine;

public class WaitingPanel : MonoBehaviour
{
    void Start()
    {
        CanvasManager.Instance.panelDict.Add("WaitingPanel", gameObject);

        GameManager.Instance.Client.OnConnected += () => { gameObject.SetActive(false); };
        GameManager.Instance.Client.OnDisconnected += () => 
        {
            if (gameObject)
                gameObject.SetActive(true); 
        };

        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("WaitingPanel");
    }
}
