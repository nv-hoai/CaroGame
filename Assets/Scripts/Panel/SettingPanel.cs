using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    public GameObject img;
    public GameObject homeButton;

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

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("SettingPanel");
    }
}
