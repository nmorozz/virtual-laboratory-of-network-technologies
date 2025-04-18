using System;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class Terminal : MonoBehaviour
{
    public DHCPClient dhcpClient;
    public DHCPHandler dhcpHandler;
    public SwitchSimulator switchSimulator;

    public InputField inputField;
    public Text outputText;
    public Button processButton;
    public PingHandler pingHandler;

    private Logger logger;

    private void Start()
    {
        inputField.ActivateInputField();
        processButton.onClick.AddListener(ProcessInput);

        logger = Logger.Instance;

        if (dhcpHandler == null)
        {
            dhcpHandler = FindObjectOfType<DHCPHandler>();
            if (dhcpHandler == null)
            {
                Debug.LogError("DHCPHandler не найден в сцене!");
            }
        }

        if (dhcpClient == null)
        {
            dhcpClient = FindObjectOfType<DHCPClient>();
            if (dhcpClient == null)
            {
                Debug.LogError("DHCPClient не найден в сцене!");
            }
        }

        if (switchSimulator == null)
        {
            switchSimulator = FindObjectOfType<SwitchSimulator>();
            if (switchSimulator == null)
            {
                Debug.LogError("SwitchSimulator не найден в сцене!");
            }
        }
    }

    public void ProcessInput()
    {
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
                    int packetCount = 4;
                    pingHandler.PingAddress(address, packetCount, false, false, 32, false);
                    output = $"Пинг до {address}...";
                }
                else output = "Ошибка: Не указан адрес для пинга.";
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
                output = switchSimulator != null ? switchSimulator.GetStatusReport() : "Коммутатор не найден!";
                break;

            case "log":
                output = logger != null ? logger.GetFormattedLogs() : "Логгер недоступен.";
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
                    int port = int.Parse(commandParts[2]);
                    float limit = float.Parse(commandParts[3]);
                    switchSimulator.SetPortSpeedLimit(port, (int)limit);
                    logger.Log($"Ограничение скорости: порт {port} -> {limit} Мбит/с");
                    output = $"Ограничение для порта {port}: {limit} Мбит/с";
                }
                else output = "Использование: limit port <port> <mbps>";
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
if (commandParts.Length >= 4 && int.TryParse(commandParts[2], out int port) && int.TryParse(commandParts[3], out int vlan))
            {
                output = switchSimulator.AssignPortToVlan(port, vlan);
            }
            else output = "Использование: vlan assign <port> <vlan>";
        }
        else output = "Неизвестная подкоманда VLAN.";
    }
    else output = "Использование: vlan add <id> [ip] или vlan assign <port> <vlan>";
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
if (commandParts.Length >= 4 && commandParts[1] == "load" && int.TryParse(commandParts[2], out int portNum) && float.TryParse(commandParts[3], out float portLoad))
    {
        switchSimulator.SetTrafficLoad(portNum, portLoad);
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
                    switch (commandParts[1])
                    {
                        case "/release":
                            output = ReleaseIPAddress();
                            break;
                        case "/renew":
                            output = RenewIPAddress();
                            break;
                        default:
                            output = "Ошибка: Неизвестный параметр для ipconfig.";
                            break;
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
                    byte[] ipBytes = dhcpHandler.HandleRequestWithPort(macAddress, macAddress, port);

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
                    dhcpHandler.HandleRequestWithPort(macAddress, macAddress, port);
                    dhcpClient.ReleaseIP();
                    logger.Log($"DHCPRELEASE -> MAC {BitConverter.ToString(macAddress)}");
                    output = "DHCPRELEASE отправлен.";
                }
                else output = "Ошибка: DHCPClient не найден!";
                break;

            case "startdhcpserver":
                if (dhcpHandler != null)
                {
                    dhcpHandler.StartDHCPServer();
                    logger.Log("DHCP-сервер запущен.");
                    output = "DHCP-сервер запущен.";
                }
                else output = "Ошибка: DHCPHandler не найден!";
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
             "ping <адрес>\ntracert <адрес>\nipconfig\nipconfig /release\nipconfig /renew\n" +
             "netsh interface ip set address name=<интерфейс> dhcp\n" +
             "dhcpdiscover\n" +
             "dhcprelease\n" +
             "startdhcpserver\n" +
             "stopdhcpserver\n" +
             "mirror port <src> <dst>\n" +
             "limit port <port> <mbps>\n" +
             "vlan add <id> [ip]\n" +
             "vlan assign <порт> <vlan>\n" +
             "cpu load <значение>\n" +
             "port load <порт> <нагрузка>\n" +
             "show run\nshow log\nclear log\nlog";
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

            output += $"  IP-адрес: {ip}\n";
            output += "  Маска подсети: 255.255.255.0\n";
            output += "  Шлюз: 192.168.1.1\n";

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
        outputText.text += output + "\n";
    }
}
