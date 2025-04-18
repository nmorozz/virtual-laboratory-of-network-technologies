using System;
using System.Collections.Generic;
using UnityEngine;

public class Logger : MonoBehaviour
{
    public static Logger Instance { get; private set; }

    private List<string> logs = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        string timeStamp = DateTime.Now.ToString("HH:mm:ss");
        string entry = $"[{timeStamp}] [{level}] {message}";

        logs.Add(entry);

        switch (level)
        {
            case LogLevel.Info:
                Debug.Log(entry);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(entry);
                break;
            case LogLevel.Error:
                Debug.LogError(entry);
                break;
        }
    }

    public void Clear()
    {
        logs.Clear();
        Debug.Log("[LOG] Лог очищен.");
    }

    public string[] GetLogs()
    {
        return logs.ToArray();
    }

    public string GetFormattedLogs()
    {
        return logs.Count > 0 ? string.Join("\n", logs) : "Лог пуст.";
    }
}
