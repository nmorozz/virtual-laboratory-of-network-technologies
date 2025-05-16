using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LabAutomation : MonoBehaviour
{
    [Header("Кнопки запуска компьютеров")]
    public Button[] powerButtons;

    [Header("Кнопки открытия терминалов")]
    public Button[] terminalButtons;

    [Header("Поля ввода и кнопки отправки команд для ПК")]
    public InputField[] inputFields;
    public Button[] submitButtons;

    private void Start()
    {
        StartCoroutine(RunLabScript());
    }

    IEnumerator RunLabScript()
    {
        yield return new WaitForSeconds(1f);

        // Запуск всех ПК
        for (int i = 0; i < powerButtons.Length; i++)
        {
            powerButtons[i].onClick.Invoke();
            yield return new WaitForSeconds(0.5f);
        }

        // Открытие терминалов
        for (int i = 0; i < terminalButtons.Length; i++)
        {
            terminalButtons[i].onClick.Invoke();
            yield return new WaitForSeconds(0.5f);
        }

        // --- СЦЕНАРИЙ 1 --- ПК1 (101) и ПК2 (102) на одном коммутаторе ---
         yield return RunCommandsOnPC(0, new[]
        {
            "startdhcpserver",
            "vlan add 10 192.168.10.1",
            "vlan assign 1 10",
            "vlan assign 9 10",
            "vlan tag 10 untagged 1",
            "vlan tag 10 untagged 9",
            "limit port 1 100",
            "mirror port 9 1",
            "show run",
            "clear log",
            "dhcpdiscover 1",
            "dhcpdiscover 9",
            "ipconfig",
            "ping 192.168.10.100",
            "tracert 192.168.10.100",

            // ❗ Дополнения:
            "vlan assign 25 1",              // uplink в default VLAN
            "segmentation 1 forward 9",      // сегментация
            "vlan delete 9",                 // удаление порта из VLAN
            "vlan add 30 192.168.30.1",
            "vlan add 40",
            "vlan add 50",
            "vlan add 60",
            "vlan add 70",
            "vlan add 80",
            "vlan add 90",
            "vlan add 100",
            "show run"
        });

        // --- СЦЕНАРИЙ 2 --- Четыре ПК (101–104), два коммутатора ---
        yield return RunCommandsOnPC(1, new[] {
            "vlan add 20 192.168.20.1",
            "vlan assign 2 20",
            "vlan assign 10 20",
            "vlan tag 20 untagged 2",
            "vlan tag 20 untagged 10",
            "dhcpdiscover 2",
            "dhcpdiscover 10",
            "ipconfig"
        });

        yield return RunCommandsOnPC(2, new[] {
            "vlan assign 3 10",
            "vlan tag 10 untagged 3",
            "dhcpdiscover 3",
            "ipconfig"
        });

        yield return RunCommandsOnPC(3, new[] {
            "vlan assign 4 20",
            "vlan tag 20 untagged 4",
            "dhcpdiscover 4",
            "ipconfig"
        });

        // --- Проверки пинга ---

        // VLAN 10: ПК1 -> ПК3
        yield return RunCommandsOnPC(0, new[] {
            "ping 192.168.10.100",
            "tracert 192.168.10.100",
            "pathping 192.168.10.100"
        });

        // VLAN 20: ПК2 -> ПК4
        yield return RunCommandsOnPC(1, new[] {
            "ping 192.168.20.100",
            "tracert 192.168.20.100",
            "pathping 192.168.20.100"
        });

        // Между VLAN: блокировка
        yield return RunCommandsOnPC(2, new[] {
            "ping 192.168.20.100"
        });

        yield return RunCommandsOnPC(3, new[] {
            "ping 192.168.10.100"
        });

        Debug.Log("[Автоматизация] Все команды выполнены.");
    }

    IEnumerator RunCommandsOnPC(int pcIndex, string[] commands)
    {
        for (int i = 0; i < commands.Length; i++)
        {
            inputFields[pcIndex].text = commands[i];
            submitButtons[pcIndex].onClick.Invoke();
            Debug.Log($"[ПК{pcIndex + 1}] Ввод команды: {commands[i]}");
            yield return new WaitForSeconds(1.5f);
        }
    }
}
