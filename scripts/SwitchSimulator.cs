using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SwitchSimulator : MonoBehaviour
{
    private Dictionary<string, string> portMacTable = new Dictionary<string, string>();
    public Dictionary<int, List<string>> vlans = new Dictionary<int, List<string>>();
    private Dictionary<int, float> portTrafficLoad = new Dictionary<int, float>();
    private float cpuLoad = 0.0f;

    public Logger logger; // Подключение внешнего логгера
private Dictionary<int, int> mirroredPorts = new Dictionary<int, int>();
    private Dictionary<int, int> portToVlan = new Dictionary<int, int>();
    public Dictionary<int, int> portSpeedLimit = new Dictionary<int, int>();
    public Dictionary<int, string> vlanInterfaces = new Dictionary<int, string>();

    private StringBuilder log = new StringBuilder();
    public string[] Logs => log.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    void Start()
    {
        if (logger == null)
        {
            logger = FindObjectOfType<Logger>();
        }
    }

    private void SendPacketToPort(NetworkPacket packet, int port)
{
    // Здесь логика доставки пакета, например:
Logger.Instance.Log($"[Forward] Отправка пакета на порт {port}: {packet}", Logger.LogLevel.Info);

    // Если хочешь симулировать отправку — можно добавить вызов метода на порте или объекте:
    // Псевдо-логика: передаём пакет в объект-приёмник (если так реализовано)
    // connectedDevices[port]?.ReceivePacket(packet);
}

public void ForwardPacket(NetworkPacket packet, int sourcePort, int destPort)
{
    // 1. Отправка пакета на основной порт
    SendPacketToPort(packet, destPort);

    // 2. Проверка, настроено ли зеркалирование
    if (mirroredPorts.ContainsKey(sourcePort))
    {
        int mirrorPort = mirroredPorts[sourcePort];

        // 3. Если зеркальный порт не совпадает с основным — дублируем
        if (mirrorPort != destPort)
        {
            SendPacketToPort(packet, mirrorPort);
Logger.Instance.Log($"[Mirror] Пакет с порта {sourcePort} продублирован на порт {mirrorPort}", Logger.LogLevel.Info);
        }
    }
}

public void MirrorPort(int sourcePort, int mirrorPort)
{
    if (sourcePort == mirrorPort)
    {
Logger.Instance.Log($"[Mirror] Ошибка: нельзя зеркалировать порт сам на себя.", Logger.LogLevel.Error);
        return;
    }

    mirroredPorts[sourcePort] = mirrorPort;
Logger.Instance.Log($"[Mirror] Трафик с порта {sourcePort} теперь зеркалируется на порт {mirrorPort}", Logger.LogLevel.Info);
}

    public string ShowRunningConfig()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("--- Таблица коммутации ---");
        foreach (var entry in portMacTable)
            sb.AppendLine($"Порт {entry.Key}: {entry.Value}");

        sb.AppendLine("\n--- VLAN ---");
        foreach (var vlan in vlans)
            sb.AppendLine($"VLAN {vlan.Key}: {string.Join(", ", vlan.Value)}");

        sb.AppendLine("\n--- Интерфейсы VLAN (SVI) ---");
        foreach (var svi in vlanInterfaces)
            sb.AppendLine($"VLAN {svi.Key}: IP {svi.Value}");

        sb.AppendLine("\n--- Нагрузка по портам ---");
        foreach (var port in portTrafficLoad)
            sb.AppendLine($"Порт {port.Key}: {port.Value:F2} Мбит/с");

        sb.AppendLine($"\n--- Нагрузка CPU: {cpuLoad:F2}%");

        sb.AppendLine("\n--- Ограничения скорости ---");
        foreach (var entry in portSpeedLimit)
            sb.AppendLine($"Порт {entry.Key}: ограничение {entry.Value} Мбит/с");

        sb.AppendLine("\n--- Зеркалирование портов ---");
        foreach (var entry in mirroredPorts)
            sb.AppendLine($"Источник: Порт {entry.Key} → Зеркало: Порт {entry.Value}");

        return sb.ToString();
    }

    public bool TryGetVlanForPort(int port, out int vlanId)
    {
        vlanId = -1;
        if (portToVlan.ContainsKey(port))
        {
            vlanId = portToVlan[port];
            return true;
        }
        return false;
    }

    public void AddMacToPort(string port, string macAddress)
    {
        portMacTable[port] = macAddress;
        LogAction($"Добавлен MAC {macAddress} на порт {port}");
    }

   public string CreateVlan(int vlanId)
{
    if (!vlans.ContainsKey(vlanId))
    {
        vlans[vlanId] = new List<string>();
        LogAction($"Создан VLAN {vlanId}");
        return $"VLAN {vlanId} создан.";
    }
    else
    {
        return $"VLAN {vlanId} уже существует.";
    }
}

public string AssignPortToVlan(int port, int vlanId)
{
    CreateVlan(vlanId); // или string msg = CreateVlan(vlanId); — если хочешь использовать текст создания

    if (!vlans[vlanId].Contains($"Port{port}"))
    {
        vlans[vlanId].Add($"Port{port}");
        portToVlan[port] = vlanId;
        LogAction($"Порт {port} назначен VLAN {vlanId}");
        return $"Порт {port} добавлен в VLAN {vlanId}";
    }
    return $"Порт {port} уже в VLAN {vlanId}";
}


    public string ConfigureVlanInterface(int vlanId, string ipAddress)
{
    vlanInterfaces[vlanId] = ipAddress;
    LogAction($"Назначен IP {ipAddress} для интерфейса VLAN {vlanId}");
    return $"Назначен IP {ipAddress} для интерфейса VLAN {vlanId}";
}


    public int? GetPortVlan(int port)
    {
        if (portToVlan.ContainsKey(port))
            return portToVlan[port];
        return null;
    }

    public void SetCPULoad(float load)
    {
        cpuLoad = Mathf.Clamp(load, 0, 100);
    }

    public void SetTrafficLoad(int port, float loadMbps)
    {
        portTrafficLoad[port] = loadMbps;
    }

    public void SetPortSpeedLimit(int port, int limitMbps)
    {
        portSpeedLimit[port] = limitMbps;
        LogAction($"Порт {port}: ограничение скорости установлено {limitMbps} Мбит/с");
    }


    public void LogAction(string action)
    {
        string timeStamp = DateTime.Now.ToString("HH:mm:ss");
        string entry = $"[{timeStamp}] {action}";
        log.AppendLine(entry);

        if (logger != null)
        {
            logger.Log(action);
        }
    }

    public string ShowLog()
    {
        return log.ToString();
    }

    public void ClearLog()
    {
        log.Clear();
        if (logger != null)
        {
            logger.Clear();
        }
    }

    public int GetVlanForPort(int port)
    {
        return portToVlan.ContainsKey(port) ? portToVlan[port] : -1;
    }

    public string GetStatusReport()
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("=== Текущие настройки коммутатора ===");

        report.AppendLine("\n--- VLAN ---");
        foreach (var vlan in vlans)
            report.AppendLine($"VLAN {vlan.Key}: Порты: {string.Join(", ", vlan.Value)}");

        report.AppendLine("\n--- Интерфейсы VLAN (SVI) ---");
        foreach (var svi in vlanInterfaces)
            report.AppendLine($"VLAN {svi.Key}: IP {svi.Value}");

        report.AppendLine("\n--- Ограничения по скорости ---");
        foreach (var entry in portSpeedLimit)
            report.AppendLine($"Порт {entry.Key}: {entry.Value} Мбит/с");

        report.AppendLine("\n--- Зеркалирование портов ---");
        foreach (var entry in mirroredPorts)
            report.AppendLine($"Источник: Порт {entry.Key} → Зеркало: Порт {entry.Value}");

        report.AppendLine($"\n--- Нагрузка CPU: {cpuLoad:F2}%");

        report.AppendLine("\n--- Нагрузка по портам ---");
        foreach (var port in portTrafficLoad)
            report.AppendLine($"Порт {port.Key}: {port.Value:F2} Мбит/с");

        return report.ToString();
    }
}
