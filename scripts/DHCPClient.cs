using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class DHCPClient : MonoBehaviour
{
    public DHCPHandler dhcpHandler;
    public SwitchSimulator switchSimulator;
    public int portNumber = 0;

    private UdpClient udpClient;
    private byte[] transactionID;
    private byte[] mac;
    public string assignedIP = "";

    public string GetSubnetMask()
    {
        return "255.255.255.0";
    }
public byte[] GetMacBytes()
{
    return mac;
}

    public string GetGateway()
{
    int vlanId = -1;
    if (switchSimulator != null && switchSimulator.TryGetVlanForPort(portNumber, out vlanId))
    {
        return $"192.168.{vlanId}.1";
    }
    return "0.0.0.0";
}


    void Start()
    {
        udpClient = new UdpClient();
        transactionID = new byte[4];
        new System.Random().NextBytes(transactionID);

        mac = GenerateMAC();
    }

    private byte[] GenerateMAC()
    {
        byte[] newMac = new byte[6];
        new System.Random().NextBytes(newMac);
        newMac[0] = (byte)(newMac[0] & 0xFE); // Убираем мультикастовый бит
        return newMac;
    }

    public void SendDHCPDiscover()
    {
        Debug.Log($"DHCPClient: Отправка DHCPDISCOVER (порт {portNumber})");
        byte[] offer = dhcpHandler.HandlePacket(transactionID, mac, 1, portNumber);
        HandleOffer(offer);
    }

    private void HandleOffer(byte[] packet)
    {
        if (packet == null) return;
        assignedIP = new IPAddress(packet).ToString();
        Debug.Log($"DHCPClient: Получен DHCPOFFER: {assignedIP}");

        // Отправка DHCPREQUEST
        byte[] ack = dhcpHandler.HandlePacket(transactionID, mac, 3, portNumber);
        HandleAck(ack);
    }
public void SetAssignedIP(string ip)
{
    assignedIP = ip;
}
    private void HandleAck(byte[] packet)
    {
        if (packet == null) return;

        string ackIP = new IPAddress(packet).ToString();
        if (ackIP == assignedIP)
        {
            Debug.Log($"DHCPClient: IP-адрес подтверждён: {assignedIP}");
        }
        else
        {
            Debug.LogWarning("DHCPClient: DHCPNAK получен, IP отклонён");
            assignedIP = "0.0.0.0";
        }
    }

    public void ReleaseIP()
    {
        Debug.Log("DHCPClient: Освобождение IP-адреса");
        dhcpHandler.HandlePacket(transactionID, mac, 7, portNumber); // DHCPRELEASE
        assignedIP = "0.0.0.0";
    }

    public string GetAssignedIP()
    {
        return assignedIP;
    }

    public string GetMacAddress()
    {
        return BitConverter.ToString(mac);
    }
}
