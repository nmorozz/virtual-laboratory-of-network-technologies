using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class DHCPClient : MonoBehaviour
{
    public DHCPHandler dhcpHandler;
    private bool isManualIp = false;
    public string GetIPSource() => ipSource;
    public string ipSource = "unknown"; // "dhcp" или "manual"

    public SwitchSimulator switchSimulator;
    public int portNumber = 0;
    public RouterController routerController;
    public bool isUsingRouter = false;

    private UdpClient udpClient;
    private byte[] transactionID;
    private byte[] mac;
    public string assignedIP = "";

    [Header("Ручной MAC (оставь пустым для автогенерации)")]
    public string manualMac = "";

    void Start()
    {
        udpClient = new UdpClient();
        transactionID = new byte[4];
        new System.Random().NextBytes(transactionID);

        if (!string.IsNullOrWhiteSpace(manualMac))
            mac = ParseMac(manualMac);
        else
            mac = GenerateMAC();

        Debug.Log($"DHCPClient MAC: {BitConverter.ToString(mac)}");

        if (portNumber == 0)
        {
            var allClients = FindObjectsOfType<DHCPClient>();
            portNumber = System.Array.IndexOf(allClients, this) + 1;
            Debug.Log($"DHCPClient [{gameObject.name}]: назначен portNumber = {portNumber}");
        }

        Debug.Log($"DHCPClient on {gameObject.name}, порт {portNumber}");

        if (!string.IsNullOrEmpty(assignedIP) && assignedIP != "0.0.0.0")
        {
            isManualIp = true;
            ipSource = "manual";

            if (switchSimulator != null && switchSimulator.isActiveAndEnabled)
                switchSimulator.RegisterClientIP(portNumber, assignedIP);
            else if (routerController != null && routerController.isActiveAndEnabled)
                routerController.RegisterClientIP(portNumber, assignedIP);

            // Добавление вручную назначенного IP в словарь
            if (switchSimulator != null)
                switchSimulator.ForceAddIP(portNumber, assignedIP);

            Debug.Log($"[MANUAL] IP из инспектора: {assignedIP}");
        }
        else
        {
            StartCoroutine(WaitForVlanThenDiscover());
        }
    }

    private IEnumerator WaitForVlanThenDiscover()
    {
        while (true)
        {
            int vlan = -1;
            bool ok = false;
            if (switchSimulator != null)
                ok = switchSimulator.TryGetVlanForPort(portNumber, out vlan);
            else if (routerController != null)
                ok = routerController.TryGetVlanForPort(portNumber, out vlan);

            if (ok && vlan != -1) break;
            yield return new WaitForSeconds(0.2f);
        }

        SendDHCPDiscover();
    }

private static Dictionary<int, HashSet<int>> vlanUsedIPs = new();

public void AssignFallbackIP()
{
    if (assignedIP != "0.0.0.0") return;

    // Найти соответствующее mesto
    mesto matchedMesto = FindMatchingMesto();
    if (matchedMesto == null) return;

    // Получить VLAN
    int vlanId = -1;
    if (matchedMesto.switchSimulator != null)
        vlanId = matchedMesto.switchSimulator.GetVlanForPort(portNumber);
    else if (matchedMesto.routerController != null)
        vlanId = portNumber;

    if (vlanId == -1)
    {
        Debug.LogWarning($"[FallbackIP] VLAN не найден для порта {portNumber}");
        return;
    }

    // Найти незанятый IP
    if (!vlanUsedIPs.ContainsKey(vlanId))
        vlanUsedIPs[vlanId] = new HashSet<int>();

    int ipHost = 100;
    while (vlanUsedIPs[vlanId].Contains(ipHost)) ipHost++;
    vlanUsedIPs[vlanId].Add(ipHost);

    string ip = $"192.168.{vlanId}.{ipHost}";
    assignedIP = ip;
    isManualIp = true;
    ipSource = "manual";

    Debug.Log($"[FallbackIP] Назначен IP {ip} клиенту на порту {portNumber}");

    if (switchSimulator != null && switchSimulator.isActiveAndEnabled)
        switchSimulator.RegisterClientIP(portNumber, ip);
    else if (routerController != null && routerController.isActiveAndEnabled)
        routerController.RegisterClientIP(portNumber, ip);
}

    private byte[] ParseMac(string macStr)
    {
        return macStr.Split('-').Select(s => Convert.ToByte(s, 16)).ToArray();
    }
private mesto FindMatchingMesto()
{
    // Найти все объекты mesto в сцене
    mesto[] allMestos = FindObjectsOfType<mesto>();

    foreach (var m in allMestos)
    {
        if (m.portNumber == this.portNumber)
        {
            return m;
        }
    }

    Debug.LogWarning($"[FallbackIP] Не найдено подходящего mesto для порта {portNumber}");
    return null;
}

    private byte[] GenerateMAC()
    {
        byte[] newMac = new byte[6];
        newMac[0] = 0x02; // локально администрируемый адрес
        newMac[1] = 0x00;
        newMac[2] = 0x00;
        newMac[3] = 0x00;
        newMac[4] = 0x00;
        newMac[5] = (byte)portNumber; // уникальность по порту
        return newMac;
    }

    public void SendDHCPDiscover()
    {
        Debug.Log($"DHCPClient: Отправка DHCPDISCOVER (порт {portNumber})");
        byte[] offer = dhcpHandler?.HandlePacket(transactionID, mac, 1, portNumber); // DISCOVER
        HandleOffer(offer);
        Debug.Log($"Запрашиваю IP для MAC: {BitConverter.ToString(mac)} на порту {portNumber}");
    }

    private void HandleOffer(byte[] packet)
    {
        if (packet == null) return;
        assignedIP = new IPAddress(packet).ToString();
        isManualIp = false;
        ipSource = "dhcp";

        if (switchSimulator != null && switchSimulator.isActiveAndEnabled)
            switchSimulator.RegisterClientIP(portNumber, assignedIP);
        else if (routerController != null && routerController.isActiveAndEnabled)
            routerController.RegisterClientIP(portNumber, assignedIP);

        Debug.Log($"[DHCP] Получен DHCPOFFER: {assignedIP}");

        byte[] ack = dhcpHandler?.HandlePacket(transactionID, mac, 3, portNumber);   // REQUEST
        HandleAck(ack);
    }

   private void HandleAck(byte[] packet)
{
    if (packet == null)
    {
        Debug.LogWarning("DHCPClient: DHCPNAK получен, IP отклонён");
        assignedIP = "0.0.0.0";

        // КОСТЫЛЬ: Назначить IP вручную, если DHCP не дал ACK
        AssignFallbackIP();
        return;
    }

    string ackIP = new IPAddress(packet).ToString();
    if (ackIP == assignedIP)
    {
        Debug.Log($"DHCPClient: IP-адрес подтверждён: {assignedIP}");
    }
    else
    {
        Debug.LogWarning("DHCPClient: DHCPNAK получен, IP отклонён");
        assignedIP = "0.0.0.0";

        // КОСТЫЛЬ: Назначить IP вручную, если DHCP дал другой
        AssignFallbackIP();
    }
}


    public void ReleaseIP()
    {
        Debug.Log("DHCPClient: Освобождение IP-адреса");
        dhcpHandler?.HandlePacket(transactionID, mac, 7, portNumber); // DHCPRELEASE
        assignedIP = "0.0.0.0";
    }

    public void SetAssignedIP(string ip)
    {
        assignedIP = ip;
        isManualIp = true;
        ipSource = "manual";

        if (switchSimulator != null && switchSimulator.isActiveAndEnabled)
            switchSimulator.RegisterClientIP(portNumber, ip);
        else if (routerController != null && routerController.isActiveAndEnabled)
            routerController.RegisterClientIP(portNumber, ip);

        if (switchSimulator != null)
            switchSimulator.ForceAddIP(portNumber, ip);

        Debug.Log($"[MANUAL] Назначен IP вручную клиенту на порту {portNumber}: {ip}");
    }

    public string GetAssignedIP() => assignedIP;

    public string GetMacAddress() => BitConverter.ToString(mac);

    public byte[] GetMacBytes() => mac;

    public string GetSubnetMask() => "255.255.255.0";

    public string GetGateway()
    {
        int vlanId = -1;
        if (switchSimulator != null && switchSimulator.TryGetVlanForPort(portNumber, out vlanId))
        {
            return $"192.168.{vlanId}.1";
        }
        return "0.0.0.0";
    }

    public string GetIP() => assignedIP;
}
