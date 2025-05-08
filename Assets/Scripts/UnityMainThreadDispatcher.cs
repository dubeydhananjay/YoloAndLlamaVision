using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;

    private readonly Queue<Action> _executionQueue = new Queue<Action>();

    /// <summary>
    /// Gets the singleton instance of the dispatcher.
    /// </summary>
    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            // Create a new GameObject to hold the dispatcher
            var dispatcherObject = new GameObject("UnityMainThreadDispatcher");
            _instance = dispatcherObject.AddComponent<UnityMainThreadDispatcher>();

            // Prevent the dispatcher from being destroyed across scenes
            DontDestroyOnLoad(dispatcherObject);
        }
        return _instance;
    }

    /// <summary>
    /// Enqueues an action to be executed on the main thread.
    /// </summary>
    public void Enqueue(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Executes actions from the queue on the main thread.
    /// </summary>
    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                var action = _executionQueue.Dequeue();
                action?.Invoke();
            }
        }
    }
}
