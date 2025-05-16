using System;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Globalization;


public class Terminal : MonoBehaviour
{
    public DHCPClient dhcpClient;
    public DHCPHandler dhcpHandler;
    public SwitchSimulator switchSimulator;

    public InputField inputField;
    public Text outputText;
    public Button processButton;
    public PingHandler pingHandler;
 public RouterController routerController;

    private Logger logger;

 private void Start()
{
    logger = Logger.Instance;

    if (routerController == null)
        routerController = FindObjectOfType<RouterController>();

    if (dhcpHandler == null)
    {
        dhcpHandler = FindObjectOfType<DHCPHandler>();
        if (dhcpHandler == null)
        {
            Debug.LogError("DHCPHandler не найден в сцене!");
        }
    }

    if (dhcpHandler != null && routerController != null)
    {
        dhcpHandler.AssignRouterController(routerController);
    }

    if (switchSimulator == null)
    {
        switchSimulator = FindObjectOfType<SwitchSimulator>();
        if (switchSimulator == null)
        {
            Debug.LogError("SwitchSimulator не найден в сцене!");
        }
    }

    inputField.ActivateInputField();
    processButton.onClick.AddListener(ProcessInput);
}

private List<int> ParsePortRange(string range)
{
    List<int> ports = new List<int>();


    if (range.Contains('-'))
    {
        string[] parts = range.Split('-');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int start) &&
            int.TryParse(parts[1], out int end))
        {
            for (int i = start; i <= end; i++)
                ports.Add(i);
        }
    }
    else if (range.Contains(','))
    {
        foreach (var part in range.Split(','))
        {
            if (int.TryParse(part, out int p))
                ports.Add(p);
        }
    }
    else if (int.TryParse(range, out int single))
    {
        ports.Add(single);
    }

    return ports;
}
public int? FindPortByIP(string ip)
{
    // ⏺️ Пытаемся искать в маршрутизаторе (в первую очередь)
    int? routerPort = routerController?.FindPortByIP(ip);
    if (routerPort != null) return routerPort;

    // ⏺️ Ищем в switch (если подключен)
    return switchSimulator?.FindPortByIP(ip);
}

    public void ProcessInput()
    {
          Debug.Log("ProcessInput вызван");
        outputText.text = "";
        string input = inputField.text;
        string[] commandParts = input.Split(' ');
        string command = commandParts[0];
        string output = "";

        switch (command)
        {
            case "ping":
    if (commandParts.Length > 1)
    {
        string address = commandParts[1];

        int? sourcePort = dhcpClient?.portNumber;
        int? destPort = FindPortByIP(address);


        if (sourcePort != null && destPort != null)
        {
            bool allowed = false;

            if (switchSimulator != null && switchSimulator.isActiveAndEnabled)
                allowed = switchSimulator.CanCommunicate(sourcePort.Value, destPort.Value);
            else if (routerController != null && routerController.isActiveAndEnabled)
                allowed = routerController.CanCommunicate(sourcePort.Value, destPort.Value);

            if (allowed)
            {
                output = $"Ответ от {address}";
                logger?.Log($"Ping: порт {sourcePort} → {destPort} (разрешено)");
            }
            else
            {
                output = $"Пинг до {address} невозможен — блокировка";
                logger?.Log($"Ping: порт {sourcePort} → {destPort} (заблокировано)");
            }
        }
        else
        {
            output = "Ошибка: невозможно определить порты источника или назначения.";
        }
    }
    else output = "Ошибка: Не указан адрес для пинга.";
    break;

                case "pathping":
    if (commandParts.Length > 1)
    {
        string address = commandParts[1];
        pingHandler.Pathping(address);
        output = $"Выполняется pathping до {address}...";
    }
    else output = "Ошибка: Не указан адрес.";
    break;

                case "set":
    if (commandParts.Length == 4 && commandParts[1] == "ip")
    {
        if (int.TryParse(commandParts[2], out int port) && IPAddress.TryParse(commandParts[3], out IPAddress ip))
        {
            dhcpClient.SetAssignedIP(commandParts[3]);
            switchSimulator.RegisterClientIP(port, commandParts[3]);
            switchSimulator.ForceAddIP(port, commandParts[3]);

            logger.Log($"Ручное назначение IP {commandParts[3]} на порт {port}");
            output = $"IP {commandParts[3]} назначен вручную порту {port}";
        }
        else
        {
            output = "Ошибка: неверный формат порта или IP-адреса.";
        }
    }
    else
    {
        output = "Использование: set ip <порт> <ip-адрес>";
    }
    break;

    case "diagnostics":
    string diag = "";
    diag += ShowIPAddressInfo();
    diag += "\n--- VLAN подключения ---\n";

    if (switchSimulator.TryGetVlanForPort(dhcpClient.portNumber, out int vlanIdDiag))
        diag += $"Порт {dhcpClient.portNumber} подключён к VLAN {vlanIdDiag}\n";
    else
        diag += $"Порт {dhcpClient.portNumber}: VLAN не назначен\n";

    diag += "\n--- DHCP-сервер ---\n";
    diag += dhcpHandler != null ? "DHCP-сервер активен.\n" : "DHCP-сервер не инициализирован.\n";

    diag += "\n--- Конфигурация коммутатора ---\n";
    diag += switchSimulator != null ? switchSimulator.ShowRunningConfig() : "SwitchSimulator не найден.";

    output = diag;
    break;


            case "clear":
    if (commandParts.Length > 1 && commandParts[1] == "log")
    {
        if (logger != null)
        {
            logger.Clear();
            output = "Лог успешно очищен.";
        }
        else
        {
            output = "Ошибка: Логгер недоступен.";
        }
    }
    else
    {
        output = "Использование: clear log";
    }
    break;


    case "show":
    if (commandParts.Length > 1)
    {
        switch (commandParts[1])
        {
            case "run":
                if (switchSimulator != null && switchSimulator.isActiveAndEnabled)
    output = switchSimulator.ShowRunningConfig();
else if (routerController != null && routerController.isActiveAndEnabled)
    output = routerController.ShowRunningConfig();

                else
                    output = "Ошибка: не найдено активное устройство.";
                break;

            case "log":
                if (routerController != null && routerController.isActiveAndEnabled)
                    output = routerController.ShowLog();
                else if (logger != null)
                    output = logger.GetFormattedLogs();
                else
                    output = "Логгер недоступен.";
                break;

            case "ip":
                if (commandParts.Length >= 3 && commandParts[2] == "route")
                {
                    output = routerController != null ? routerController.GetRoutingTable() : "Маршрутизатор не найден!";
                }
                else
                {
                    output = "Ошибка: неизвестная подкоманда IP.";
                }
                break;

            default:
                output = "Неизвестный подкомандный параметр.";
                break;
        }
    }
    else
    {
        output = "Ошибка: Не указана подкоманда для show.";
    }
    break;
            case "mirror":
                if (commandParts.Length == 4 && commandParts[1] == "port")
                {
                    int source = int.Parse(commandParts[2]);
                    int mirror = int.Parse(commandParts[3]);
                    switchSimulator.MirrorPort(source, mirror);
                    logger.Log($"Зеркалирование: порт {source} -> {mirror}");
                    output = $"Трафик с порта {source} будет зеркалироваться на порт {mirror}";
                }
                else output = "Использование: mirror port <source> <destination>";
                break;

            case "limit":
    if (commandParts.Length == 4 && commandParts[1] == "port")
    {
        if (int.TryParse(commandParts[2], out int port) &&
            float.TryParse(commandParts[3], out float rawLimit))
        {
            int limit = Mathf.RoundToInt(rawLimit);
            switchSimulator.SetPortSpeedLimit(port, limit);

            logger.Log($"Ограничение скорости: порт {port} -> {limit} Мбит/с");
            output = $"Ограничение для порта {port}: {limit} Мбит/с";
        }
        else
        {
            output = "Ошибка: неверный формат порта или значения ограничения.";
        }
    }
    else
    {
        output = "Использование: limit port <port> <mbps>";
    }
    break;

case "port-speed":
    if (switchSimulator != null)
    {
        if (commandParts.Length == 3 && int.TryParse(commandParts[2], out int port))
        {
            // Показать скорость конкретного порта
            float speed = switchSimulator.GetPortSpeed(port);
output = $"Скорость порта {port}: {speed:F2} Мбит/с";

        }
        else
        {
            // Показать скорости всех портов
            Dictionary<int, int> portSpeeds = switchSimulator.GetAllPortSpeeds();
            if (portSpeeds.Count > 0)
            {
                output = "Скорости всех портов:\n";
                foreach (var kvp in portSpeeds)
                {
                    output += $"Порт {kvp.Key}: {kvp.Value} Мбит/с\n";
                }
            }
            else
            {
                output = "Нет информации о скоростях портов.";
            }
        }
    }
    else
    {
        output = "Коммутатор не найден!";
    }
    break;
case "trunk":
    if (commandParts.Length >= 3)
    {
        int port = int.Parse(commandParts[2]);
        bool enable = commandParts[1] != "disable";
        switchSimulator.SetTrunkPort(port, enable);
        output = enable
            ? $"Порт {port} настроен как TRUNK"
            : $"TRUNK отключен на порту {port}";
    }
    break; 
case "segmentation":
    if (commandParts.Length >= 4 && commandParts[2] == "forward")
    {
        var sourcePorts = ParsePortRange(commandParts[1]);
        var forwardPorts = ParsePortRange(commandParts[3]);
        foreach (var src in sourcePorts)
        {
            switchSimulator.SetTrafficSegmentation(src, forwardPorts);
        }
        output = $"Сегментация настроена: {string.Join(", ", sourcePorts)} → {string.Join(", ", forwardPorts)}";
    }
    break;

           case "vlan":
    if (commandParts.Length >= 2)
    {
        if (commandParts[1] == "add")
        {
            if (commandParts.Length >= 3 && int.TryParse(commandParts[2], out int vlanId))
            {
                string ip = commandParts.Length >= 4 ? commandParts[3] : null;
                if (ip != null)
                {
                    output = switchSimulator.ConfigureVlanInterface(vlanId, ip);
                }
                else
                {
                    output = switchSimulator.CreateVlan(vlanId);
                }
            }
            else output = "Использование: vlan add <id> [ip]";
        }
        else if (commandParts[1] == "assign")
{
    if (commandParts.Length >= 4 &&
        int.TryParse(commandParts[3], out int vlanId))
    {
        var ports = ParsePortRange(commandParts[2]);
        StringBuilder sb = new StringBuilder();
       foreach (var port in ports)
{
    if (switchSimulator != null && switchSimulator.isActiveAndEnabled)
        sb.AppendLine(switchSimulator.AssignPortToVlan(port, vlanId));

    if (routerController != null && routerController.isActiveAndEnabled)
    {
        routerController.AssignPortToVlan(port, vlanId);
        sb.AppendLine($"Порт {port} добавлен в VLAN {vlanId} (на маршрутизаторе)");
    }
}

        output = sb.ToString();
    }
    else output = "Использование: vlan assign <port или диапазон> <vlan>";
}

        else if (commandParts[1] == "delete")
        {
            if (commandParts.Length >= 3)
            {
                var ports = ParsePortRange(commandParts[2]);
                foreach (var port in ports)
                    switchSimulator.RemovePortFromAllVlans(port);
                output = $"Порты {string.Join(", ", ports)} удалены из всех VLAN.";
            }
            else output = "Использование: vlan delete <порт или диапазон>";
        }
        else if (commandParts[1] == "tag")
        {
            if (commandParts.Length >= 5 &&
                int.TryParse(commandParts[2], out int vlanId) &&
                (commandParts[3] == "tagged" || commandParts[3] == "untagged"))
            {
                var ports = ParsePortRange(commandParts[4]);
                bool isTagged = commandParts[3] == "tagged";
                foreach (int port in ports)
                {
                    output += switchSimulator.SetTaggedStatus(vlanId, port, isTagged) + "\n";
                }
            }
            else output = "Использование: vlan tag <vlanId> <tagged|untagged> <порт или диапазон>";
        }
        else
        {
            output = "Неизвестная подкоманда VLAN.";
        }
    }
    else output = "Использование: vlan add/assign/delete/tag ...";
    break;


case "cpu":
if (commandParts.Length >= 3 && commandParts[1] == "load" && float.TryParse(commandParts[2], out float cpuLoad))
    {
        switchSimulator.SetCPULoad(cpuLoad);
        output = $"Установлена загрузка CPU: {cpuLoad}%";
    }
    else output = "Использование: cpu load <значение>";
    break;

case "port":
    if (commandParts.Length >= 4 && 
        commandParts[1] == "load" && 
        int.TryParse(commandParts[2], out int portNum) && 
        float.TryParse(commandParts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out float portLoad))
    {
        if (switchSimulator != null && switchSimulator.isActiveAndEnabled)
            switchSimulator.SetTrafficLoad(portNum, portLoad);
        else if (routerController != null && routerController.isActiveAndEnabled)
            routerController.SetTrafficLoad(portNum, portLoad);

        output = $"Установлена нагрузка на порт {portNum}: {portLoad} Мбит/с";
    }
    else output = "Использование: port load <порт> <нагрузка>";
    break;



            
            case "tracert":
                if (commandParts.Length > 1)
                {
                    string address = commandParts[1];
                    pingHandler.Tracert(address, 30);
                    output = $"Трассировка маршрута до {address}...";
                }
                else output = "Ошибка: Не указан адрес.";
                break;

            case "ipconfig":
    if (commandParts.Length > 1)
    {
        if (commandParts[1] == "set" && commandParts.Length >= 3)
        {
            string ip = commandParts[2];
            int vlan = switchSimulator.GetVlanForPort(dhcpClient.portNumber);
            if (!ip.StartsWith($"192.168.{vlan}."))
            {
                output = $"Ошибка: IP {ip} не соответствует VLAN {vlan}.";
                break;
            }

            dhcpClient.SetAssignedIP(ip);
            logger.Log($"Пользователь вручную назначил IP {ip} на порт {dhcpClient.portNumber}");
            output = $"IP-адрес {ip} назначен вручную.";
        }
        else if (commandParts[1] == "/release")
        {
            output = ReleaseIPAddress();
        }
        else if (commandParts[1] == "/renew")
        {
            output = RenewIPAddress();
        }
        else
        {
            output = "Ошибка: Неизвестный параметр для ipconfig.";
        }
    }
    else
    {
        output = ShowIPAddressInfo();
    }
    break;


            case "netsh":
                if (commandParts.Length > 3 && commandParts[1] == "interface" && commandParts[2] == "ip")
                {
                    if (commandParts[3] == "set" && commandParts.Length >= 7 && commandParts[4] == "address")
                    {
                        if (commandParts[6] == "dhcp")
                        {
                            string interfaceName = commandParts[5].Replace("name=", "");
                            output = EnableDHCP(interfaceName);
                        }
                        else output = "Ошибка: Неверный формат команды.";
                    }
                    else output = "Ошибка: Неподдерживаемая команда.";
                }
                else output = "Ошибка: Неправильный формат команды.";
                break;

            case "dhcpdiscover":
                if (dhcpClient != null)
                {
                    int port = 0;
                    if (commandParts.Length > 1)
                    {
                        int.TryParse(commandParts[1], out port);
                    }

                    byte[] macAddress = dhcpClient.GetMacBytes();
                    byte[] ipBytes = dhcpHandler.HandlePacket(macAddress, macAddress, 1, port); // DISCOVER
ipBytes = dhcpHandler.HandlePacket(macAddress, macAddress, 3, port);        // REQUEST


                    if (ipBytes != null)
                    {
                        string ip = new System.Net.IPAddress(ipBytes).ToString();
                        dhcpClient.SetAssignedIP(ip);
                        logger.Log($"DHCPDISCOVER -> IP {ip}");
                        output = $"DHCPDISCOVER отправлен. Назначен IP: {ip}";
                    }
                    else
                    {
                        output = "Не удалось получить IP адрес.";
                    }
                }
                else output = "Ошибка: DHCPClient не найден!";
                break;

            case "dhcprelease":
    if (dhcpClient != null)
    {
        int port = 0;
        if (commandParts.Length > 1)
        {
            int.TryParse(commandParts[1], out port);
        }

        byte[] macAddress = dhcpClient.GetMacBytes();
        dhcpHandler.HandlePacket(macAddress, macAddress, 7, port); // RELEASE
        dhcpClient.ReleaseIP();
        logger.Log($"DHCPRELEASE -> MAC {BitConverter.ToString(macAddress)}");
        output = "DHCPRELEASE отправлен.";
    }
    else output = "Ошибка: DHCPClient не найден!";
    break;


            case "startdhcpserver":
                if (routerController != null && routerController.isActiveAndEnabled)
{
    routerController.StartDHCPServer();
    logger.Log("DHCP-сервер запущен на маршрутизаторе.");
    output = "DHCP-сервер запущен.";
}
else if (dhcpHandler != null && switchSimulator != null)
{
    dhcpHandler.StartDHCPServer();
    logger.Log("DHCP-сервер запущен на коммутаторе.");
    output = "DHCP-сервер запущен.";
}
else
{
    output = "Ошибка: ни одно устройство не поддерживает DHCP.";
}

                break;

            case "stopdhcpserver":
                if (dhcpHandler != null)
                {
                    dhcpHandler.StopDHCPServer();
                    logger.Log("DHCP-сервер остановлен.");
                    output = "DHCP-сервер остановлен.";
                }
                else output = "Ошибка: DHCPHandler не найден!";
                break;

            case "time":
                output = "Текущее время: " + DateTime.Now.ToString("HH:mm:ss");
                break;

            case "help":
    output = "Список команд:\n" +
         "ping <ip> — проверка доступности IP\n" +
         "tracert <ip> — трассировка маршрута\n" +
         "pathping <ip> — расширенная трассировка с потерями\n" +
         "ipconfig — показать текущий IP\n" +
         "ipconfig /release — освободить IP\n" +
         "ipconfig /renew — запросить IP по DHCP\n" +
         "netsh interface ip set address name=<имя> dhcp — включить DHCP\n" +
         "dhcpdiscover <порт> — запрос IP от клиента\n" +
         "dhcprelease <порт> — освободить IP клиента\n" +
         "startdhcpserver — запустить DHCP-сервер\n" +
         "stopdhcpserver — остановить DHCP-сервер\n" +
         "vlan add <id> [ip] — создать VLAN и интерфейс\n" +
         "vlan assign <порт> <vlan> — назначить порт VLAN'у\n" +
         "mirror port <источник> <зеркало> — включить зеркалирование порта\n" +
         "limit port <порт> <скорость> — ограничить пропускную способность\n" +
         "port load <порт> <мбит/с> — установить нагрузку вручную\n" +
         "port-speed [порт] — скорость порта (или всех)\n" +
         "cpu load <значение> — установить нагрузку на CPU\n" +
         "show run — текущая конфигурация коммутатора\n" +
         "show log — показать журнал\n" +
         "clear log — очистить журнал\n" +
         "diagnostics — диагностика текущей сетевой настройки\n";
    break;



            default:
                output = "Неизвестная команда. Введите 'help' для списка команд.";
                break;
        }

        AddOutput($"> {input}\n{output}");
        inputField.text = "";
    }

public string ShowIPAddressInfo()
{
    try
    {
        string output = "Информация об IP-адресах интерфейсов:\n";
        output += "\nИнтерфейс: Ethernet0\n";
        output += "Описание: Виртуальный адаптер Ethernet\n";
        output += "Тип: Ethernet\n";
        output += "Статус: Включен\n";

        string ip = dhcpClient != null && !string.IsNullOrEmpty(dhcpClient.assignedIP)
            ? dhcpClient.assignedIP
            : "Нет IP-адреса";

        string source = dhcpClient != null ? dhcpClient.GetIPSource() : "неизвестен";

        output += $"  IP-адрес: {ip}\n";
        output += $"  Маска подсети: 255.255.255.0\n";
        output += $"  Шлюз: 192.168.1.1\n";
        output += $"  Источник назначения: {source}\n";

        return output;
    }
    catch (Exception ex)
    {
        return $"Ошибка при получении информации об IP-адресах: {ex.Message}";
    }
}


    public string ReleaseIPAddress()
    {
        if (dhcpClient == null)
        {
            return "Ошибка: DHCPClient не найден!";
        }

        dhcpClient.ReleaseIP();
        return "Запрос на освобождение IP-адреса отправлен.";
    }

    public string RenewIPAddress()
    {
        if (dhcpClient == null)
        {
            return "Ошибка: DHCPClient не найден!";
        }

        dhcpClient.SendDHCPDiscover();
        return "Запрос на получение нового IP-адреса отправлен.";
    }

    public string EnableDHCP(string interfaceName)
    {
        if (dhcpClient == null)
        {
            return "Ошибка: DHCPClient не найден!";
        }

        dhcpClient.SendDHCPDiscover();
        return $"DHCP включен для интерфейса {interfaceName}. Запрашивается новый IP-адрес.";
    }

    public void AddOutput(string output)
    {
         Debug.Log("Добавление в outputText: " + output);
        outputText.text += output + "\n";
    }
}
