using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LabAutomationRouter : MonoBehaviour
{
    [Header("Кнопка запуска маршрутизатора")]
    public Button routerPowerButton;

    [Header("Кнопка открытия терминала маршрутизатора")]
    public Button terminalButton;

    [Header("Поле ввода и кнопка отправки команд")]
    public InputField inputField;
    public Button submitButton;

    private void Start()
    {
        StartCoroutine(RunRouterScenario());
    }

    IEnumerator RunRouterScenario()
    {
        yield return new WaitForSeconds(1f);

        // Включение маршрутизатора
        routerPowerButton.onClick.Invoke();
        yield return new WaitForSeconds(1f);

        // Открытие терминала
        terminalButton.onClick.Invoke();
        yield return new WaitForSeconds(1f);

        // Команды сценария 3
 string[] commands = {
        // Включение DHCP-сервера
        "startdhcpserver",
"vlan add 10",
    "vlan add 20",
        // Назначение VLAN
        "vlan assign 1 10",
        "vlan assign 2 10",
        "vlan assign 9 20",
        "vlan assign 10 20",

        // Запрос IP адресов по DHCP
        "dhcpdiscover 1",
        "dhcpdiscover 2",
        "dhcpdiscover 9",
        "dhcpdiscover 10",

        // Проверка конфигурации и статуса
        "show run",
        "show ip route",

        // Нагрузка
        "cpu load 45",
        "port load 1 12.5",
        "port load 2 7.3",

        // PING от всех клиентов ко всем IP
        "ping 192.168.10.101",
        "ping 192.168.20.100",
        "ping 192.168.20.101",

        "ping 192.168.10.101",
        "ping 192.168.20.100",
        "ping 192.168.20.101",

        "ping 192.168.10.100",
        "ping 192.168.10.101",
        "ping 192.168.20.101",

        "ping 192.168.10.100",
        "ping 192.168.10.101",
        "ping 192.168.20.100",

        // Завершение
        "show log",
        "clear log"
    };

        foreach (string cmd in commands)
        {
            inputField.text = cmd;
            submitButton.onClick.Invoke();
            Debug.Log($"[ROUTER] Ввод команды: {cmd}");
            yield return new WaitForSeconds(1.5f);
        }

        Debug.Log("[Автоматизация] Сценарий 3 завершён.");
    }
}
