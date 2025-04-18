using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class DHCPServer : MonoBehaviour
{
    private UdpClient udpServer;
    private IPEndPoint clientEndPoint;
    private Dictionary<string, string> allocatedIPs = new Dictionary<string, string>();
    private Queue<string> availableIPs = new Queue<string>(new[] { "192.168.1.100", "192.168.1.101", "192.168.1.102" });
    private string subnetMask = "255.255.255.0";
    private string gateway = "192.168.1.1";
    private string dns = "8.8.8.8";

    void Start()
    {
        udpServer = new UdpClient(67);
        udpServer.BeginReceive(ReceiveCallback, null);
        Debug.Log("DHCP Server Started...");
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        byte[] receivedBytes = udpServer.EndReceive(ar, ref clientEndPoint);
        DHCPMessage dhcpMessage = DHCPMessage.Parse(receivedBytes);
        ProcessDHCPMessage(dhcpMessage);
        udpServer.BeginReceive(ReceiveCallback, null);
    }

    private void ProcessDHCPMessage(DHCPMessage dhcpMessage)
    {
        switch (dhcpMessage.MessageType)
        {
            case DHCPMessageType.Discover:
                SendDHCPOffer(dhcpMessage.MACAddress);
                break;
            case DHCPMessageType.Request:
                SendDHCPAck(dhcpMessage.MACAddress);
                break;
            case DHCPMessageType.Release:
                ReleaseIP(dhcpMessage.MACAddress);
                break;
        }
    }

    private void SendDHCPOffer(string macAddress)
    {
        if (availableIPs.Count == 0)
        {
            Debug.Log("No available IPs");
            return;
        }
        string assignedIP = availableIPs.Dequeue();
        allocatedIPs[macAddress] = assignedIP;
        SendDHCPResponse(DHCPMessageType.Offer, macAddress, assignedIP);
    }

    private void SendDHCPAck(string macAddress)
    {
        if (!allocatedIPs.ContainsKey(macAddress)) return;
        string assignedIP = allocatedIPs[macAddress];
        SendDHCPResponse(DHCPMessageType.Ack, macAddress, assignedIP);
    }

    private void ReleaseIP(string macAddress)
    {
        if (allocatedIPs.ContainsKey(macAddress))
        {
            availableIPs.Enqueue(allocatedIPs[macAddress]);
            allocatedIPs.Remove(macAddress);
        }
    }

    private void SendDHCPResponse(DHCPMessageType type, string macAddress, string assignedIP)
    {
        DHCPMessage response = new DHCPMessage(type, macAddress, assignedIP, subnetMask, gateway, dns);
        byte[] responseData = response.Serialize();
        udpServer.Send(responseData, responseData.Length, clientEndPoint);
        Debug.Log($"Sent {type} to {macAddress} with IP {assignedIP}");
    }
}

public enum DHCPMessageType { Discover, Offer, Request, Ack, Release }

public class DHCPMessage
{
    public DHCPMessageType MessageType;
    public string MACAddress;
    public string AssignedIP;
    public string SubnetMask;
    public string Gateway;
    public string DNS;

    public DHCPMessage(DHCPMessageType type, string mac, string ip, string mask, string gw, string dns)
    {
        MessageType = type;
        MACAddress = mac;
        AssignedIP = ip;
        SubnetMask = mask;
        Gateway = gw;
        DNS = dns;
    }

    public byte[] Serialize()
    {
        return Encoding.ASCII.GetBytes($"{MessageType},{MACAddress},{AssignedIP},{SubnetMask},{Gateway},{DNS}");
    }

    public static DHCPMessage Parse(byte[] data)
    {
        string decoded = Encoding.ASCII.GetString(data);
        string[] parts = decoded.Split(',');
        return new DHCPMessage(
            Enum.Parse<DHCPMessageType>(parts[0]),
            parts[1],
            parts.Length > 2 ? parts[2] : "",
            parts.Length > 3 ? parts[3] : "",
            parts.Length > 4 ? parts[4] : "",
            parts.Length > 5 ? parts[5] : ""
        );
    }
}
