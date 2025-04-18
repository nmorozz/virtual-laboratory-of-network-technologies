using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NetworkPing = System.Net.NetworkInformation.Ping;
using PingReply = System.Net.NetworkInformation.PingReply;
using PingOptions = System.Net.NetworkInformation.PingOptions;

public class PingHandler : MonoBehaviour
{
    public Terminal terminal;
    private const int Timeout = 3000;
    private const int BufferSize = 32;
    private byte[] buffer = Encoding.ASCII.GetBytes(new string('a', BufferSize));

    public async Task<int> SimplePingAsync(string address)
    {
        using (NetworkPing ping = new NetworkPing())
        {
            PingReply reply = await Task.Run(() => ping.Send(address, Timeout));
            return reply.Status == IPStatus.Success ? (int)reply.RoundtripTime : -1;
        }
    }

    public async Task<List<int>> TraceRouteAsync(string address, int maxHops = 30)
    {
        List<int> hopRTTs = new List<int>();
        using (NetworkPing ping = new NetworkPing())
        {
            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                PingOptions options = new PingOptions(ttl, true);
                PingReply reply = await Task.Run(() => ping.Send(address, Timeout, buffer, options));
                
                hopRTTs.Add(reply.Status == IPStatus.Success ? (int)reply.RoundtripTime : -1);
                await Task.Delay(500);
                if (reply.Status == IPStatus.Success) break;
            }
        }
        return hopRTTs;
    }

    public async void Tracert(string address, int maxHops = 30)
    {
        terminal.AddOutput($"> tracert {address}");
        terminal.AddOutput($"Трассировка маршрута к {address}, макс. хопов: {maxHops}");

        for (int ttl = 1; ttl <= maxHops; ttl++)
        {
            using (NetworkPing ping = new NetworkPing())
            {
                PingOptions options = new PingOptions(ttl, true);
                PingReply reply = ping.Send(address, Timeout, buffer, options);

                if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
                {
                    terminal.AddOutput($"{ttl}\t{reply.RoundtripTime} ms\t{reply.Address}");
                    if (reply.Status == IPStatus.Success) break;
                }
                else terminal.AddOutput($"{ttl}\t*\tЗапрос истек.");
            }
        }
    }

    public async void Pathping(string address, int packetCount = 10)
    {
        terminal.AddOutput($"> pathping {address}");
        terminal.AddOutput($"Подсчет статистики для {address}...");
        List<int> hopRTTs = await TraceRouteAsync(address, 30);
        foreach (var (rtt, index) in hopRTTs.Select((value, i) => (value, i + 1)))
        {
            terminal.AddOutput($"Хоп {index}: RTT = {rtt}ms");
        }
    }

    public async void PingAddress(string address, int packetCount, bool infinitePing, bool resolveNames, int packetSize, bool noFragment)
    {
        using (NetworkPing ping = new NetworkPing())
        {
            PingOptions options = new PingOptions { DontFragment = noFragment };
            terminal.AddOutput($"> ping {address} -n {packetCount} -l {packetSize}");
            terminal.AddOutput($"Обмен пакетами с {address}...");
            
            int pingsSent = 0;
            while (infinitePing || pingsSent < packetCount)
            {
                PingReply reply = ping.Send(address, Timeout, new byte[packetSize], options);
                terminal.AddOutput(reply.Status == IPStatus.Success 
                    ? $"Ответ от {reply.Address}: {reply.Buffer.Length} байт, {reply.RoundtripTime} мс, TTL={reply.Options?.Ttl ?? 0}" 
                    : $"Пинг не удался: {reply.Status}");
                
                await Task.Delay(100);
                pingsSent++;
            }
        }
    }
}
