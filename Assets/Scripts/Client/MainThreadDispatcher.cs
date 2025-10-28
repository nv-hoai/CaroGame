using System;
using System.Collections.Concurrent;
using UnityEngine;

public static class MainThreadDispatcher
{
    private static readonly ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
    private static volatile bool hasActions = false;

    public static void Enqueue(Action action)
    {
        actions.Enqueue(action);
        hasActions = true;
    }

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
        var go = new GameObject("MainThreadDispatcher");
        go.AddComponent<MainThreadDispatcherBehaviour>();
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    private class MainThreadDispatcherBehaviour : MonoBehaviour
    {
        private void Update()
        {
            if (hasActions)
            {
                while (actions.TryDequeue(out Action action))
                {
                    action?.Invoke();
                }
                hasActions = false;
            }
        }
    }
}
