using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance { get; private set; }

    public Transform bgImage;
    public List<Transform> canvases;
    public List<Transform> panels;
    public TMP_Dropdown graphicQualityDropdown;

    public int currentCanvasIndex, currentPanelIndex;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentCanvasIndex = 1;
        currentPanelIndex = -1;
    }

    public void OpenPanel(int index)
    {
        CloseCurrentPanel();

        if (index >= 0 && index < panels.Count)
        {
            currentPanelIndex = index;
            panels[0].gameObject.SetActive(true);
            panels[index].gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Invalid panel index: " + index);
        }

        AudioManager.Instance.PlayClickSound();
    }

    public void CloseCurrentPanel()
    {
        if (currentPanelIndex > -1)
        {
            panels[currentPanelIndex].gameObject.SetActive(false);
            panels[0].gameObject.SetActive(false);
            currentPanelIndex = -1;
        }
    }

    public void DisableClose()
    {
        if (currentPanelIndex > -1)
        {
            panels[0].gameObject.SetActive(false);
        }
    }

    public void SwitchCanvas(int index)
    {
        if (index >= 0 && index < canvases.Count)
        {
            canvases[currentCanvasIndex].gameObject.SetActive(false);
            if (index == 1)
            {
                bgImage.gameObject.SetActive(true);
            }
            else
            {
                bgImage.gameObject.SetActive(false);
            }
            currentCanvasIndex = index;
            canvases[currentCanvasIndex].gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Invalid canvas index: " + index);
        }
    }

    public void ChangeGraphicQuality()
    {
        QualitySettings.SetQualityLevel(graphicQualityDropdown.value);
    }
}
