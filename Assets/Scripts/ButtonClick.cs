using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonClick : MonoBehaviour
{
    public string functionName;
    public string parameter;
    private Button btn;

    void Start()
    {
        btn = GetComponent<Button>();

        if (!string.IsNullOrEmpty(functionName) && !string.IsNullOrEmpty(parameter))
        {
            btn.onClick.AddListener(() => CanvasManager.Instance.actDict[functionName](parameter));
        }
    }
}
