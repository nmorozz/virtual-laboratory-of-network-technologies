using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class DHCPHandler : MonoBehaviour
{
    private Dictionary<int, Dictionary<string, string>> vlanLeases = new(); // vlanId -> mac -> ip
    private Dictionary<int, List<string>> vlanAllocatedIPs = new(); // vlanId -> list of used IPs
    private Dictionary<int, string> vlanIpPoolStart = new(); // vlanId -> start IP
    private Dictionary<int, string> vlanIpPoolEnd = new();   // vlanId -> end IP

    private bool serverRunning = true;

    private SwitchSimulator switchSim;

private RouterController routerController;

public void AssignRouterController(RouterController controller)
{
    routerController = controller;
}

    public void AssignSwitchSimulator(SwitchSimulator sim)
    {
        switchSim = sim;
    }

    public byte[] HandlePacket(byte[] transactionID, byte[] mac, byte messageType, int portNumber)
{
    int vlanId = switchSim?.GetVlanForPort(portNumber) ?? -1;

    if (vlanId == -1 && routerController != null)
    {
        vlanId = portNumber; // если порт = VLAN для RouterController
    }

    if (!serverRunning || (switchSim == null && routerController == null))
        return null;

    string macStr = BitConverter.ToString(mac);

    if (vlanId == -1)
    {
        Debug.LogWarning($"DHCPHandler: порт {portNumber} не принадлежит ни одному VLAN");
        return null;
    }

    Debug.Log($"[DHCPHandler] Обработка запроса. MAC: {macStr}, Port: {portNumber}, VLAN: {vlanId}, Type: {messageType}");

    InitVlanPools(vlanId);

    switch (messageType)
    {
        case 1: // DHCPDISCOVER
            Debug.Log($"DHCPHandler: DISCOVER от {macStr} на порту {portNumber} (VLAN {vlanId})");
            return OfferIP(macStr, vlanId);

        case 3: // DHCPREQUEST
            Debug.Log($"DHCPHandler: REQUEST от {macStr} на порту {portNumber} (VLAN {vlanId})");
            return AcknowledgeIP(macStr, vlanId);

        case 7: // DHCPRELEASE
            Debug.Log($"DHCPHandler: RELEASE от {macStr} (VLAN {vlanId})");
            ReleaseIP(macStr, vlanId);
            return null;

        default:
            Debug.LogWarning("DHCPHandler: Неизвестный тип сообщения DHCP");
            return null;
    }
}



    // Исправленный метод, который теперь принимает int portNumber
    public byte[] HandleRequestWithPort(byte[] transactionID, byte[] mac, int portNumber)
{
    return HandlePacket(transactionID, mac, 7, portNumber); // DHCPRELEASE
}


   private void InitVlanPools(int vlanId)
{
    if (!vlanLeases.ContainsKey(vlanId))
    {
        Debug.Log($"[DHCPHandler] Инициализация пула для VLAN {vlanId}");

        vlanLeases[vlanId] = new Dictionary<string, string>();
        vlanAllocatedIPs[vlanId] = new List<string>();
        vlanIpPoolStart[vlanId] = $"192.168.{vlanId}.100";
        vlanIpPoolEnd[vlanId] = $"192.168.{vlanId}.200";
    }
    else
    {
        Debug.Log($"[DHCPHandler] Пул VLAN {vlanId} уже инициализирован");
    }
}


   private byte[] OfferIP(string macStr, int vlanId)
{
    Debug.Log($"[DHCPHandler] macStr: {macStr}, VLAN: {vlanId}");

    if (vlanLeases[vlanId].ContainsKey(macStr))
    {
        Debug.Log($"[DHCPHandler] Уже назначен IP: {vlanLeases[vlanId][macStr]}");
        return IPAddress.Parse(vlanLeases[vlanId][macStr]).GetAddressBytes();
    }

    string newIP = GenerateUniqueIP(vlanId);
    if (newIP != null)
    {
        vlanLeases[vlanId][macStr] = newIP;
        vlanAllocatedIPs[vlanId].Add(newIP);
        Debug.Log($"[DHCPHandler] Назначен новый IP: {newIP}");
        return IPAddress.Parse(newIP).GetAddressBytes();
    }

    Debug.LogWarning($"[DHCPHandler] Не удалось сгенерировать IP для MAC: {macStr} в VLAN {vlanId}");
    return null;
}



    private byte[] AcknowledgeIP(string macStr, int vlanId)
    {
        if (vlanLeases[vlanId].ContainsKey(macStr))
        {
            return IPAddress.Parse(vlanLeases[vlanId][macStr]).GetAddressBytes();
        }

        return null;
    }

    private void ReleaseIP(string macStr, int vlanId)
    {
        if (vlanLeases[vlanId].ContainsKey(macStr))
        {
            string releasedIP = vlanLeases[vlanId][macStr];
            vlanLeases[vlanId].Remove(macStr);
            vlanAllocatedIPs[vlanId].Remove(releasedIP);
        }
    }

    private string GenerateUniqueIP(int vlanId)
    {
        IPAddress start = IPAddress.Parse(vlanIpPoolStart[vlanId]);
        IPAddress end = IPAddress.Parse(vlanIpPoolEnd[vlanId]);
        byte[] startBytes = start.GetAddressBytes();
        byte[] endBytes = end.GetAddressBytes();

        for (int i = startBytes[3]; i <= endBytes[3]; i++)
        {
            string candidateIP = $"192.168.{vlanId}.{i}";
            if (!vlanAllocatedIPs[vlanId].Contains(candidateIP))
            {
                return candidateIP;
            }
        }

        Debug.LogWarning($"DHCPHandler: IP-пул VLAN {vlanId} исчерпан!");
        return null;
    }

    public void StartDHCPServer()
    {
        serverRunning = true;
        Debug.Log("DHCP-сервер запущен");
    }

    public void StopDHCPServer()
    {
        serverRunning = false;
        vlanLeases.Clear();
        vlanAllocatedIPs.Clear();
        Debug.Log("DHCP-сервер остановлен");
    }
}

public static class SwitchSimulatorExtensions
{
    public static bool TryGetVlanForPort(this SwitchSimulator sim, int port, out int vlanId)
    {
        vlanId = sim.GetVlanForPort(port);
        return vlanId != -1;
    }
}
