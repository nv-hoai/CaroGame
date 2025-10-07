using UnityEngine;
using UnityEngine.EventSystems;

public class ClosePanel : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        int currentPanelIndex = CanvasManager.Instance.currentPanelIndex;

        if (currentPanelIndex == 8)
        {
            _ = GameManager.Instance.Client.LeaveMatch();
        }

        CanvasManager.Instance.CloseCurrentPanel();
    }
}
