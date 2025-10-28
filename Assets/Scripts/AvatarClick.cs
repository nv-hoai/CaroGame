using UnityEngine;
using UnityEngine.EventSystems;

public class AvatarClick : MonoBehaviour, IPointerClickHandler
{
    public string functionName;
    public string parameter;

    public void OnPointerClick(PointerEventData eventData)
    {
        CanvasManager.Instance.actDict[functionName](parameter);
    }
}
