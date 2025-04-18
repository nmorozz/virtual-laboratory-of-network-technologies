using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class NetworkPacket : MonoBehaviour
{
    /// <summary>
    /// MAC-адрес отправителя.
    /// </summary>
    public string SourceMac;

    /// <summary>
    /// MAC-адрес получателя.
    /// Может быть пустым (широковещательный), если нужно отправить на все порты.
    /// </summary>
    public string DestinationMac;

    /// <summary>
    /// IP-адрес отправителя (если используется IP-связь).
    /// </summary>
    public string SourceIP;

    /// <summary>
    /// IP-адрес назначения (если используется IP-связь).
    /// </summary>
    public string DestinationIP;

    /// <summary>
    /// Тип пакета (например: ARP, DHCP, ICMP, DATA и т.д.).
    /// </summary>
    public string Protocol;

    /// <summary>
    /// Полезная нагрузка пакета.
    /// </summary>
    public string Payload;

    /// <summary>
    /// Таймштамп, когда пакет был создан.
    /// </summary>
    public DateTime Timestamp;

    /// <summary>
    /// VLAN, к которому принадлежит пакет (для разделения трафика).
    /// </summary>
    public int VlanId;

    public NetworkPacket()
    {
        Timestamp = DateTime.Now;
    }

    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss}] {Protocol} пакет: {SourceIP} ({SourceMac}) → {DestinationIP} ({DestinationMac}), VLAN {VlanId}, Данные: {Payload}";
    }
}
