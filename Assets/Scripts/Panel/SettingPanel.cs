using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    public GameObject img;
    public GameObject homeButton;
    public GameObject logoutButton;

    private void Start()
    {
        CanvasManager.Instance.panelDict.Add("SettingPanel", gameObject);
        
        gameObject.SetActive(false);
    }

    public void BackHome()
    {
        _ = GameManager.Instance.Client.LeaveMatch();
        CanvasManager.Instance.ClosePanel(CanvasManager.Instance.currentPanel);
        CanvasManager.Instance.CloseCanvas("GameCanvas");
        CanvasManager.Instance.OpenCanvas("ForeCanvas");
        img.SetActive(true);
        homeButton.SetActive(false);
    }

    public void Logout()
    {
        if (GameManager.Instance.Client.IsInMatch)
            _ = GameManager.Instance.Client.LeaveMatch();
        _ = GameManager.Instance.Client.Logout();
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("SettingPanel");
    }
}
