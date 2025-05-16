using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

[System.Serializable]
public class SwitchState
{
    public Dictionary<int, string> vlanInterfaces;
    public Dictionary<int, int> portSpeedLimit;
    public Dictionary<int, int> mirroredPorts;
    public float cpuLoad;
    public Dictionary<int, float> portTrafficLoad;
    public List<string> log;
}

[System.Serializable]
public class PortConfig
{
    public int portNumber;
    public int vlanId;
    public string macAddress;
}

public class SwitchSimulator : MonoBehaviour
{
    public List<PortConfig> portConfigs = new List<PortConfig>();
    private Dictionary<string, int> ipToPortMap = new(); // Новый словарь IP -> порт
    private Dictionary<string, string> portMacTable = new();
    public Dictionary<int, List<string>> vlans = new();
    private Dictionary<int, float> portTrafficLoad = new();
    private float cpuLoad = 0.0f;
private Dictionary<int, int> portPacketCounter = new(); // счётчик пакетов по портам

    private Dictionary<int, int> mirroredPorts = new();
    private Dictionary<int, int> portToVlan = new();
    public Dictionary<int, int> portSpeedLimit = new();
    public Dictionary<int, string> vlanInterfaces = new();
private float speedUpdateInterval = 1f;  // интервал в секундах
private float lastSpeedUpdateTime = 0f;

    private Dictionary<int, float> portLastCountTime = new();
    private Dictionary<int, int> portSpeeds = new();

    private Logger logger;
    private StringBuilder log = new();
    private Dictionary<int, List<int>> vlanPortMap = new();

    public string[] Logs => log.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
public void ForceAddIP(int port, string ip)
{
    if (!ipToPortMap.ContainsKey(ip))
        ipToPortMap[ip] = port;
}

    void Start()
    {
        if (logger == null)
            logger = FindObjectOfType<Logger>();
    }
    public bool TryGetVlanForPort(int port, out int vlan)
{
    vlan = GetVlanForPort(port);
    return vlan != -1;
}

private void UpdatePortSpeeds()
{
    foreach (var port in portPacketCounter.Keys.ToList())
    {
        float speed = GetPortSpeed(port); // получаем скорость
        Debug.Log($"[Speed Update] Порт {port}: {speed:F2} пакетов/сек (сохранено в portSpeeds)");
    }
}

private HashSet<int> trunkPorts = new();
private Dictionary<int, List<int>> segmentationRules = new();
public bool CanCommunicate(int portA, int portB)
{
    foreach (var entry in vlanPortMap)
    {
        var ports = entry.Value;
        if (ports.Contains(portA) && ports.Contains(portB))
            return true;
    }
    return false;
}





public void SetTrafficSegmentation(int port, List<int> forwardList)
{
    segmentationRules[port] = new List<int>(forwardList);
    LogAction($"Сегментация трафика: порт {port} может передавать только на {string.Join(", ", forwardList)}");
}

public void SetTrunkPort(int port, bool enabled)
{
    if (enabled) trunkPorts.Add(port);
    else trunkPorts.Remove(port);
    LogAction($"Порт {port} {(enabled ? "добавлен в TRUNK" : "удален из TRUNK")}");
}

public string SetTaggedStatus(int vlanId, int port, bool isTagged)
{
    CreateVlan(vlanId);
    string tagType = isTagged ? "TAGGED" : "UNTAGGED";
    if (!vlans[vlanId].Contains($"Port{port}"))
        vlans[vlanId].Add($"Port{port}");

    LogAction($"VLAN {vlanId}: Порт {port} настроен как {tagType}");
    return $"VLAN {vlanId}: порт {port} добавлен как {tagType}";
}

public void RemovePortFromAllVlans(int port)
{
    foreach (var vlan in vlans)
        vlan.Value.Remove($"Port{port}");
    portToVlan.Remove(port);
    LogAction($"Порт {port} удален из всех VLAN");
}

public List<string> GetRegisteredClientIPs()
{
    return ipToPortMap.Keys.ToList();
}


void Update()
{
    if (Time.time - lastSpeedUpdateTime >= speedUpdateInterval)
    {
        UpdatePortSpeeds(); // вычисление скоростей
        lastSpeedUpdateTime = Time.time;
    }
}

    public bool AssignVlanToPort(int portNumber, int vlanId)
    {
        Debug.Log($"Назначаю VLAN {vlanId} порту {portNumber}");
        portToVlan[portNumber] = vlanId;
        return true;
    }
public int FindPortByIP(string ip)
{
    foreach (var switchSim in FindObjectsOfType<SwitchSimulator>())
    {
        if (switchSim.ipToPortMap.TryGetValue(ip, out int port))
            return port;
    }
    return -1; // не найден
}
private Dictionary<int, string> portToIpMap = new();

public void RegisterClientIP(int port, string ip)
{
    // Стандартная логика
    if (!portToIpMap.ContainsKey(port))
        portToIpMap[port] = ip;

    // Критично важно:
    if (!ipToPortMap.ContainsKey(ip))
        ipToPortMap[ip] = port;

    Debug.Log($"[SwitchSimulator] IP {ip} зарегистрирован за портом {port}");
}






    public void HandleIncomingPacket(int port, string sourceMac, string destMac)
    {
        Logger.Instance.Log($"Пакет от {sourceMac} к {destMac} пришёл на порт {port}", Logger.LogLevel.Traffic);
        AddMacToPort(port.ToString(), sourceMac);

        if (!portPacketCounter.ContainsKey(port))
            portPacketCounter[port] = 0;

        portPacketCounter[port]++;
    }
    public Dictionary<string, string> GetVlanInterfaces()
{
    return vlanInterfaces.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
}

public Dictionary<string, int> GetPortSpeeds()
{
    return portSpeedLimit.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
}

public Dictionary<string, string> GetMirroredPorts()
{
    return mirroredPorts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());
}

public Dictionary<string, float> GetPortLoads()
{
    return portTrafficLoad.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
}

public List<string> GetLog()
{
    return new List<string>(Logs);
}

public float GetCpuLoad() => cpuLoad;

public void SyncToServerFromState()
{
    var state = new
    {
        vlanInterfaces = GetVlanInterfaces(),
        portSpeedLimit = GetPortSpeeds(),
        mirroredPorts = GetMirroredPorts(),
        cpuLoad = GetCpuLoad(),
        portTrafficLoad = GetPortLoads(),
        log = GetLog()
    };

    string json = JsonConvert.SerializeObject(state);

    StartCoroutine(PostToServer(json));
}

    public void SyncToServer()
    {
        SwitchState state = new()
        {
            vlanInterfaces = vlanInterfaces,
            portSpeedLimit = portSpeedLimit,
            mirroredPorts = mirroredPorts,
            cpuLoad = cpuLoad,
            portTrafficLoad = portTrafficLoad,
            log = new List<string>(Logs)
        };

        string json = JsonConvert.SerializeObject(state);

        StartCoroutine(PostToServer(json));
    }

    private IEnumerator PostToServer(string json)
    {
        UnityWebRequest request = new("http://127.0.0.1:5000/api/sync", "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError("Ошибка синхронизации с сервером: " + request.error);
        else
            Debug.Log("Состояние успешно отправлено на сервер.");
    }

public float GetPortSpeed(int port)
{
    if (!portPacketCounter.ContainsKey(port)) return 0f;

    float lastUpdate = portLastCountTime.ContainsKey(port) ? portLastCountTime[port] : Time.time;
    float timeDelta = Time.time - lastUpdate;
    if (timeDelta <= 0) timeDelta = 0.0001f;

    int packetCount = portPacketCounter[port];
    float speed = packetCount / timeDelta;

    // обновляем скорость
    portSpeeds[port] = Mathf.RoundToInt(speed);

    // сбрасываем счётчик
    portPacketCounter[port] = 0;
    portLastCountTime[port] = Time.time;
Debug.Log($"[Speed] Port {port} — {portSpeeds[port]} pkt/s");

    return speed;
}

    public void MirrorPort(int sourcePort, int mirrorPort)
    {
        if (sourcePort == mirrorPort)
        {
            Logger.Instance.Log("[Mirror] Ошибка: нельзя зеркалировать порт сам на себя.", Logger.LogLevel.Error);
            return;
        }

        mirroredPorts[sourcePort] = mirrorPort;
        Logger.Instance.Log($"[Mirror] Трафик с порта {sourcePort} теперь зеркалируется на порт {mirrorPort}", Logger.LogLevel.Info);
    }

   public string ConfigureVlanInterface(int vlanId, string ipAddress)
{
    vlanInterfaces[vlanId] = ipAddress;
    LogAction($"Назначен IP {ipAddress} для интерфейса VLAN {vlanId}");

    SyncToServer(); // ✅ добавлено

    return $"Назначен IP {ipAddress} для интерфейса VLAN {vlanId}";
}


    public string CreateVlan(int vlanId)
    {
        if (!vlans.ContainsKey(vlanId))
        {
            vlans[vlanId] = new();
            LogAction($"Создан VLAN {vlanId}");
            return $"VLAN {vlanId} создан.";
        }
        return $"VLAN {vlanId} уже существует.";
    }

    public string AssignPortToVlan(int port, int vlanId)
{
    CreateVlan(vlanId); // добавляем VLAN в словарь, если не существует
    if (!vlans[vlanId].Contains($"Port{port}"))
    {
        vlans[vlanId].Add($"Port{port}");
        portToVlan[port] = vlanId; // <=== ВАЖНО
        if (!vlanPortMap.ContainsKey(vlanId))
    vlanPortMap[vlanId] = new List<int>();
if (!vlanPortMap[vlanId].Contains(port))
    vlanPortMap[vlanId].Add(port);
        SyncToServer();
        LogAction($"Порт {port} назначен VLAN {vlanId}");
        return $"Порт {port} добавлен в VLAN {vlanId}";
    }
    return $"Порт {port} уже в VLAN {vlanId}";
}


    public void ForwardPacket(NetworkPacket packet, int sourcePort, int destPort)
    {
        int sourceVlan = GetVlanForPort(sourcePort);
        int destVlan = GetVlanForPort(destPort);

        if (sourceVlan != destVlan)
        {
            Logger.Instance.Log($"[VLAN BLOCK] Пакет от порта {sourcePort} (VLAN {sourceVlan}) к порту {destPort} (VLAN {destVlan}) заблокирован", Logger.LogLevel.Warning);
            return;
        }

        SendPacketToPort(packet, destPort);

        if (mirroredPorts.TryGetValue(sourcePort, out int mirrorPort) && mirrorPort != destPort)
        {
            SendPacketToPort(packet, mirrorPort);
            Logger.Instance.Log($"[Mirror] Пакет с порта {sourcePort} продублирован на порт {mirrorPort}", Logger.LogLevel.Info);
        }
    }

    private void SendPacketToPort(NetworkPacket packet, int port)
    {
        Logger.Instance.Log($"[Forward] Отправка пакета на порт {port}: {packet}", Logger.LogLevel.Info);
    }
public int GetVlanForPort(int port)
{
    if (portToVlan.TryGetValue(port, out int vlan))
        return vlan;

    return -1;
}



public string GetMacForPort(int port)
{
    var config = portConfigs.Find(p => p.portNumber == port);
    return config != null ? config.macAddress : "02-00-00-00-00-00";
}

  

    public Dictionary<int, int> GetAllPortSpeeds() => portSpeeds;

    public void SetCPULoad(float load) => cpuLoad = Mathf.Clamp(load, 0, 100);

    public void SetTrafficLoad(int port, float loadMbps) => portTrafficLoad[port] = loadMbps;

    public void SetPortSpeedLimit(int port, int limitMbps)
    {
        portSpeedLimit[port] = limitMbps;
        LogAction($"Порт {port}: ограничение скорости установлено {limitMbps} Мбит/с");
    }

    public string ShowRunningConfig()
    {
        StringBuilder sb = new();
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

    public void AddMacToPort(string port, string macAddress)
    {
        portMacTable[port] = macAddress;
        LogAction($"Добавлен MAC {macAddress} на порт {port}");
    }

    public void LogAction(string action)
    {
        string entry = $"[{DateTime.Now:HH:mm:ss}] {action}";
        log.AppendLine(entry);
        logger?.Log(action);
    }

    public string ShowLog() => log.ToString();
    public void ClearLog()
    {
        log.Clear();
        logger?.Clear();
    }
    public string GetFirstSVI() => vlanInterfaces.Values.FirstOrDefault();
}