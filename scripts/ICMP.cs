using System;
using System.Net.NetworkInformation;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;

public class ICMP : MonoBehaviour
{
    public Terminal terminal;

    public void PingAddress(string address, int packetCount, bool infinitePing, bool resolveNames, int packetSize, bool noFragment)
    {
        try
        {
            var ping = new System.Net.NetworkInformation.Ping();
            int timeout = 1000;
            PingOptions options = new PingOptions { DontFragment = noFragment };

            terminal.AddOutput($"> ping {address} -n {packetCount} -l {packetSize} -t {infinitePing} -a {resolveNames} -f {noFragment}");
            terminal.AddOutput($"Обмен пакетами с {address}...");

            if (address == "localhost")
            {
                terminal.AddOutput("Проверка доступности IPv4 и IPv6 для localhost:");
                PingLocalhost(ping, timeout, packetSize, options);
                return;
            }

            int pingsSent = 0;
            while (infinitePing || pingsSent < packetCount)
            {
                PingReply reply = ping.Send(address, timeout, new byte[packetSize], options);

                if (reply.Status == IPStatus.Success)
                {
                    string resolvedAddress = resolveNames ? reply.Address.ToString() : reply.Address.ToString();
                    terminal.AddOutput($"Ответ от {resolvedAddress}: число байт={reply.Buffer.Length} время={reply.RoundtripTime}мс TTL={reply.Options?.Ttl ?? 0}");
                }
                else
                {
                    terminal.AddOutput($"Пинг не удался: {reply.Status}");
                }

                pingsSent++;
                if (infinitePing)
                {
                    Thread.Sleep(1000);
                }
            }

            terminal.AddOutput($"\nСтатистика Ping для {address}:");
            terminal.AddOutput($"Пакетов: отправлено = {pingsSent}, получено = {pingsSent}, потеряно = 0 (0% потерь)");
        }
        catch (Exception ex)
        {
            terminal.AddOutput($"Ошибка: {ex.Message}");
        }
    }

    public void Traceroute(string address, int maxHops)
    {
        try
        {
            terminal.AddOutput($"> tracert {address}");
            terminal.AddOutput($"Трассировка маршрута к {address} с максимальным числом прыжков {maxHops}:");

            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                var ping = new System.Net.NetworkInformation.Ping();
                PingOptions options = new PingOptions(ttl, true);
                byte[] buffer = new byte[32];
                PingReply reply = ping.Send(address, 1000, buffer, options);

                if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.Success)
                {
                    terminal.AddOutput($"{ttl}\t{reply.RoundtripTime}мс\t{reply.Address}");

                    if (reply.Status == IPStatus.Success)
                    {
                        terminal.AddOutput("Трассировка завершена.");
                        break;
                    }
                }
                else
                {
                    terminal.AddOutput($"{ttl}\t*\t*\t* Превышен интервал ожидания.");
                }
            }
        }
        catch (Exception ex)
        {
            terminal.AddOutput($"Ошибка: {ex.Message}");
        }
    }

    public void Pathping(string address, int maxHops)
    {
        try
        {
            terminal.AddOutput($"> pathping {address}");
            terminal.AddOutput($"Трассировка маршрута к {address} с максимальным числом переходов {maxHops}:");

            Dictionary<string, int> hopStats = new Dictionary<string, int>();

            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                var ping = new System.Net.NetworkInformation.Ping();
                PingOptions options = new PingOptions(ttl, true);
                byte[] buffer = new byte[32];
                PingReply reply = ping.Send(address, 1000, buffer, options);

                if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.Success)
                {
                    string hopAddress = reply.Address.ToString();
                    if (!hopStats.ContainsKey(hopAddress))
                        hopStats[hopAddress]++;

                    terminal.AddOutput($"{ttl}\t{reply.RoundtripTime}мс\t{hopAddress}");

                    if (reply.Status == IPStatus.Success)
                    {
                        terminal.AddOutput("Трассировка завершена.");
                        break;
                    }
                }
                else
                {
                    terminal.AddOutput($"{ttl}\t*\t*\t* Превышен интервал ожидания.");
                }
            }

            terminal.AddOutput("\nСводная статистика маршрута:");
            foreach (var hop in hopStats)
            {
                terminal.AddOutput($"Узел: {hop.Key}, Пакетов: {hop.Value}");
            }
        }
        catch (Exception ex)
        {
            terminal.AddOutput($"Ошибка: {ex.Message}");
        }
    }

    private void PingLocalhost(System.Net.NetworkInformation.Ping ping, int timeout, int packetSize, PingOptions options)
    {
        try
        {
            PingReply replyIPv4 = ping.Send("127.0.0.1", timeout, new byte[packetSize], options);
            if (replyIPv4.Status == IPStatus.Success)
            {
                terminal.AddOutput($"Ответ от 127.0.0.1: число байт={replyIPv4.Buffer.Length} время={replyIPv4.RoundtripTime}мс TTL={replyIPv4.Options?.Ttl ?? 0}");
            }
        }
        catch
        {
            terminal.AddOutput("Ошибка: IPv4 недоступен для localhost");
        }

        try
        {
            PingReply replyIPv6 = ping.Send("::1", timeout, new byte[packetSize], options);
            if (replyIPv6.Status == IPStatus.Success)
            {
                terminal.AddOutput($"Ответ от ::1: число байт={replyIPv6.Buffer.Length} время={replyIPv6.RoundtripTime}мс TTL={replyIPv6.Options?.Ttl ?? 0}");
            }
        }
        catch
        {
            terminal.AddOutput("Ошибка: IPv6 недоступен для localhost");
        }
    }
}
