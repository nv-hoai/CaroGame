using System.Threading.Tasks;
using UnityEngine;

public class PlayModePanel : MonoBehaviour
{
    private void Start()
    {
        gameObject.SetActive(false);
    }

    public async void PlayPVP()
    {
        CanvasManager.Instance.OpenPanel(8);
        await GameManager.Instance.Client.SendMessage("FIND_MATCH");
    }

    public async void PlayPVE()
    {
        //Play with AI
        CanvasManager.Instance.OpenPanel(8);
        await GameManager.Instance.Client.SendMessage("PLAY_WITH_AI");
    }
}
