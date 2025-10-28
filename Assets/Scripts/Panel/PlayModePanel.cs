using System.Threading.Tasks;
using UnityEngine;

public class PlayModePanel : MonoBehaviour
{
    private void Start()
    {
        CanvasManager.Instance.panelDict.Add("PlayModePanel", gameObject);
        
        gameObject.SetActive(false);
    }

    public async void PlayPVP()
    {
        CanvasManager.Instance.OpenPanel("FindMatchPanel");
        await GameManager.Instance.Client.SendMessage("FIND_MATCH");
    }
    
    public async void PlayPVE()
    {
        //Play with AI
        CanvasManager.Instance.OpenPanel("FindMatchPanel");
        await GameManager.Instance.Client.SendMessage("PLAY_WITH_AI");
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("PlayModePanel");
    }
}
