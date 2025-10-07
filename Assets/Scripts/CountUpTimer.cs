using TMPro;
using UnityEngine;

public class CountUpTimer : MonoBehaviour
{
    private TextMeshProUGUI timerText; // optional UI text to display time

    private float elapsedTime = 0f; // time elapsed since start
    private bool isRunning = true; // flag to control timer state

    private void Start()
    {
        timerText = GetComponent<TextMeshProUGUI>();
        DisplayTime(elapsedTime); // Initialize display
    }

    void Update()
    {
        if (!isRunning)
            return; // Exit if timer is not running

        elapsedTime += Time.deltaTime; // Increment elapsed time
        DisplayTime(elapsedTime); // Update display
    }

    public void ResetTimer()
    {
        elapsedTime = 0f; // Reset elapsed time
        DisplayTime(elapsedTime); // Update display
    }

    public void StopTimer()
    {
        isRunning = false; // Stop the timer
    }

    public void StartTimer()
    {
        isRunning = true; // Start the timer
    }

    void DisplayTime(float timeToDisplay)
    {
        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);
        if (timerText != null)
            timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }
}
