using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PingHandler : MonoBehaviour
{
    public Terminal terminal;
    public SwitchSimulator switchSimulator;
    private const int SimulatedPingDelayMs = 100;
    private const int BufferSize = 32;

    private byte[] buffer = Encoding.ASCII.GetBytes(new string('a', BufferSize));

public void PingAddress(string address, int packetCount, bool infinitePing, bool resolveNames, int packetSize, bool noFragment)
{
    // Выбираем первый зарегистрированный IP клиента в switchSimulator
    string srcIP = switchSimulator?.GetRegisteredClientIPs().FirstOrDefault();

int? srcPort = switchSimulator?.FindPortByIP(srcIP);
int? dstPort = switchSimulator?.FindPortByIP(address);

// ↓ Теперь добавим проверку: можно ли общаться?
if (!switchSimulator.CanCommunicate(srcPort.Value, dstPort.Value))
{
    terminal.AddOutput("Ping невозможен: устройства находятся в разных VLAN.");
    return;
}
}

private IEnumerator SimulatePingCoroutine(string srcIP, string dstIP, int count, bool infinite, int packetSize)
{
    int? srcPort = switchSimulator.FindPortByIP(srcIP);
    int? dstPort = switchSimulator.FindPortByIP(dstIP);

    if (srcPort == null || dstPort == null)
    {
        terminal.AddOutput("Ошибка: не удалось определить порты по IP.");
        yield break;
    }

    if (!switchSimulator.CanCommunicate(srcPort.Value, dstPort.Value))
{
    terminal.AddOutput("Ping невозможен: устройства находятся в разных VLAN.");
    yield break;
}


    int sent = 0;
    while (infinite || sent < count)
    {
        yield return new WaitForSeconds(SimulatedPingDelayMs / 1000f);
        int delay = UnityEngine.Random.Range(1, 30);

        // Эмулируем приём пакета на целевой порт
        switchSimulator.HandleIncomingPacket(dstPort.Value, srcIP, dstIP);

        terminal.AddOutput($"Ответ от {dstIP}: байт={packetSize} время={delay}мс TTL=128");
        sent++;
    }
}



    public void Tracert(string address, int maxHops = 30)
{
    string srcIP = switchSimulator?.GetRegisteredClientIPs().FirstOrDefault();
    if (srcIP == null)
    {
        terminal.AddOutput("Ошибка: нет доступных клиентов.");
        return;
    }

    terminal.AddOutput($"> tracert {address}");
    terminal.AddOutput($"Трассировка маршрута к {address}, макс. хопов: {maxHops}");

    StartCoroutine(SimulateTracertCoroutine(address, maxHops));
}


    private IEnumerator SimulateTracertCoroutine(string dstIP, int maxHops)
{
    string srcIP = switchSimulator?.GetRegisteredClientIPs()
                           .FirstOrDefault(ip => ip != dstIP);


        if (srcIP == null)
        {
            terminal.AddOutput("Ошибка: исходный IP не определён.");
            yield break;
        }

        int? srcPort = switchSimulator.FindPortByIP(srcIP);
        int? dstPort = switchSimulator.FindPortByIP(dstIP);

        if (srcPort == null || dstPort == null)
        {
            terminal.AddOutput("Ошибка: не удалось определить порты.");
            yield break;
        }

        if (!switchSimulator.CanCommunicate(srcPort.Value, dstPort.Value))
        {
            terminal.AddOutput("Трассировка невозможна: устройства в разных VLAN.");
            yield break;
        }

        for (int ttl = 1; ttl <= maxHops; ttl++)
        {
            yield return new WaitForSeconds(SimulatedPingDelayMs / 1000f);
            string hopIP = $"192.168.{ttl}.1"; // симулируй IP
            int rtt = UnityEngine.Random.Range(5, 50);

            terminal.AddOutput($"{ttl}\t{rtt} мс\t{hopIP}");

            if (ttl >= 3) // например, достигли цели на 3 хопе
                break;
        }
    }

    public void Pathping(string address, int packetCount = 10)
{
    string srcIP = switchSimulator?.GetRegisteredClientIPs().FirstOrDefault();
    if (srcIP == null)
    {
        terminal.AddOutput("Ошибка: нет доступных клиентов.");
        return;
    }

    terminal.AddOutput($"> pathping {address}");
    terminal.AddOutput($"Подсчет статистики для {address}...");

    StartCoroutine(SimulatePathpingCoroutine(address, 3)); // можно 3 хопа
}


    private IEnumerator SimulatePathpingCoroutine(string dstIP, int hops)
{
    string srcIP = switchSimulator?.GetRegisteredClientIPs().FirstOrDefault(ip => ip != dstIP);

    if (srcIP == null)
    {
        terminal.AddOutput("Ошибка: не удалось определить исходный IP.");
        yield break;
    }

    int? srcPort = switchSimulator.FindPortByIP(srcIP);
    int? dstPort = switchSimulator.FindPortByIP(dstIP);

    if (srcPort == null || dstPort == null)
    {
        terminal.AddOutput("Ошибка: не удалось определить порты.");
        yield break;
    }

    if (!switchSimulator.CanCommunicate(srcPort.Value, dstPort.Value))
    {
        terminal.AddOutput("Pathping невозможен: устройства находятся в разных VLAN.");
        yield break;
    }

    // "Трассировка"
    for (int i = 1; i <= hops; i++)
    {
        yield return new WaitForSeconds(0.3f);
        terminal.AddOutput($"{i}\t{UnityEngine.Random.Range(5, 40)}мс\t192.168.{i}.1");
    }

    // "Статистика"
    terminal.AddOutput("Сбор статистики завершён:");
    for (int i = 1; i <= hops; i++)
    {
        yield return new WaitForSeconds(0.2f);
        terminal.AddOutput($"Хоп {i}: среднее RTT = {UnityEngine.Random.Range(10, 30)}мс, потеря пакетов: 0%");
    }
}


    public async Task<int> SimplePingAsync(string address)
    {
        await Task.Delay(SimulatedPingDelayMs);
        int? port = switchSimulator.FindPortByIP(address);
        return port != null ? UnityEngine.Random.Range(5, 30) : -1;
    }

    public async Task<List<int>> TraceRouteAsync(string address, int maxHops = 30)
    {
        List<int> rtts = new List<int>();
        for (int i = 0; i < maxHops; i++)
        {
            await Task.Delay(SimulatedPingDelayMs);
            int delay = UnityEngine.Random.Range(10, 40);
            rtts.Add(delay);
            if (i == 2) break; // как будто цель достигнута
        }
        return rtts;
    }
}
