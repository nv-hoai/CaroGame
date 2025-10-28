using UnityEngine;
using UnityEngine.EventSystems;

public class ClosePanel : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        string currentPanel = CanvasManager.Instance.currentPanel;

        if (currentPanel == "FindMatchPanel")
            _ = GameManager.Instance.Client.LeaveMatch();

        CanvasManager.Instance.ClosePanel(currentPanel);
    }
}
