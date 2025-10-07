using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class CountdownTimer : MonoBehaviour
{
    public float timeDown = 15.0f;
    public float timeRemaining; // set countdown time in seconds
    
    private TextMeshProUGUI timerText; // optional UI text to display time
    private bool isRunning = true; // flag to control timer state

    public UnityEvent onTimerEnd; // Event to trigger when timer ends   

    private void Start()
    {
        timerText = GetComponent<TextMeshProUGUI>();
        timeRemaining = timeDown;
    }

    void Update()
    {
        if (!isRunning)
            return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            ResetTimer();
        }

        DisplayTime(timeRemaining);
    }

    public void StopTimer()
    {
        isRunning = false; // Stop the timer
    }

    public void StartTimer()
    {
        isRunning = true; // Start the timer
    }

    public void ResetTimer()
    {
        timeRemaining = timeDown; // Reset to initial countdown time
    }

    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay = Mathf.Max(0, timeToDisplay);
        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);
        if (timerText != null)
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
