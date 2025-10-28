using System;
using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance { get; private set; }

    public Dictionary<string, GameObject> panelDict;
    public Dictionary<string, GameObject> canvasDict;
    public Dictionary<string, Action<string>> actDict;

    public string currentPanel;
    public string currentCanvas;

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
        panelDict = new Dictionary<string, GameObject>();
        canvasDict = new Dictionary<string, GameObject>();
        actDict = new Dictionary<string, Action<string>>()
        {
            {"OpenPanel", (panel) => OpenPanel(panel)},
            {"ClosePanel", (panel) => ClosePanel(panel) },
            {"OpenCanvas", (panel) => OpenCanvas(panel) },
            {"CloseCanvas", (panel) => CloseCanvas(panel) }
        };

        currentPanel = string.Empty;
        currentCanvas = string.Empty;
    }

    public void OpenPanel(string panel)
    {
        ClosePanel(currentPanel);

        if (panelDict.ContainsKey(panel))
        {
            GameObject panelToOpen = panelDict[panel];
            panelToOpen.SetActive(true);
            currentPanel = panel;
        }
    }

    public void ClosePanel(string panel)
    {
        if (panelDict.ContainsKey(panel))
        {
            GameObject gameObject = panelDict[panel];
            gameObject.SetActive(false);
            currentPanel = string.Empty;
        }
    }

    public void OpenCanvas(string canvas)
    {
        if (canvasDict.ContainsKey(canvas))
        {
            GameObject canvasToOpen = canvasDict[canvas];
            canvasToOpen.SetActive(true);
            currentCanvas = canvas;
        }
    }

    public void CloseCanvas(string canvas)
    {
        if (canvasDict.ContainsKey(canvas))
        {
            GameObject gameObject = canvasDict[canvas];
            gameObject.SetActive(false);
            currentCanvas = string.Empty;
        }
    }
}
