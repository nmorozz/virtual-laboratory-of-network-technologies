using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

public class RouterSimulator : MonoBehaviour
{
    private List<string> routingTable = new();
    private Dictionary<int, string> portStatus = new(); // up/down
    private float cpuLoad = 0;
    private Logger logger;
    private StringBuilder log = new();

    void Start() {
        logger = Logger.Instance;
    }
public void AddRoute(string route)
{
    if (!routingTable.Contains(route))
    {
        routingTable.Add(route);
        LogAction($"Добавлен маршрут: {route}");
    }
}

public void SetPortStatus(int port, string status)
{
    portStatus[port] = status;
    LogAction($"Порт {port} установлен в состояние {status}");
}

    public string GetRoutingTable() {
        return string.Join("\n", routingTable);
    }

    public string ShowRunningConfig() {
        return $"--- Таблица маршрутизации ---\n{GetRoutingTable()}\n\n" +
               $"--- Порты ---\n" + string.Join("\n", portStatus.Select(p => $"Port {p.Key}: {p.Value}")) +
               $"\n\n--- CPU: {cpuLoad}%";
    }

    public void StartDHCPServer() {
        logger?.Log("DHCP-сервер маршрутизатора запущен.");
    }

    public void SetCpuLoad(float load) {
        cpuLoad = Mathf.Clamp(load, 0, 100);
    }

    public float GetCpuLoad() => cpuLoad;

    public Dictionary<int, string> GetPortStatus() => portStatus;

    public void LogAction(string text) {
        log.AppendLine(text);
        logger?.Log(text);
    }

    public string ShowLog() => log.ToString();
}
