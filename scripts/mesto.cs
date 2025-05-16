using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mesto : MonoBehaviour
{
    [Header("Настройки подключения")]
    public int portNumber;
    public GameObject object1;
    public GameObject object2;
    public GameObject object3;
    public Vector3 rotationCoordinates;
public RouterController routerController; // ← добавь поле
    [Header("Ссылки на системы")]
    public SwitchSimulator switchSimulator;

    [Header("Информация (только для чтения)")]
    [SerializeField] private string vlanInfo = "Не подключен"; // Отображается в инспекторе

void Start() {
    if (switchSimulator == null)
        switchSimulator = FindObjectOfType<SwitchSimulator>();
    if (routerController == null)
        routerController = FindObjectOfType<RouterController>();
}
void Update() {
    if (switchSimulator != null && switchSimulator.isActiveAndEnabled) {
        int vlanId;
        if (switchSimulator.TryGetVlanForPort(portNumber, out vlanId))
            vlanInfo = "VLAN: " + vlanId;
        else
            vlanInfo = "VLAN не назначен";
    } else if (routerController != null && routerController.isActiveAndEnabled) {
        // Для RouterController можно использовать порт = VLAN
        vlanInfo = $"VLAN: {portNumber}";
    }
}

    void OnTriggerEnter(Collider other)
    {
        GameObject target = other.gameObject;

        if (target == object1 || target == object2 || target == object3)
        {
            target.transform.position = transform.position;
            target.transform.rotation = Quaternion.Euler(rotationCoordinates);

            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
                rb.constraints = RigidbodyConstraints.FreezePosition;

            if (switchSimulator != null && switchSimulator.isActiveAndEnabled)
{
    int vlan = switchSimulator.GetVlanForPort(portNumber);
    vlanInfo = vlan != -1 ? $"VLAN {vlan}" : "VLAN не назначен";
    string log = $"К порту {portNumber} ({vlanInfo}) подключено устройство: {target.name}";
    switchSimulator.LogAction(log);
}
else if (routerController != null && routerController.isActiveAndEnabled)
{
    vlanInfo = $"VLAN {portNumber}";
    string log = $"К порту {portNumber} (Router) подключено устройство: {target.name}";
    routerController.LogAction(log);
}


            CableConnection connection = target.GetComponentInParent<CableConnection>();
            if (connection != null)
            {
                Debug.Log($"CableConnection найден на объекте: {connection.gameObject.name}");
                connection.NotifyObjectPlaced(target);
            }
            else
            {
                Debug.LogWarning("CableConnection не найден у объекта или его родителей: " + target.name);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        GameObject target = other.gameObject;

        if (target == object1 || target == object2 || target == object3)
        {
            if (switchSimulator != null && switchSimulator.isActiveAndEnabled)
{
    int vlan = switchSimulator.GetVlanForPort(portNumber);
    vlanInfo = vlan != -1 ? $"VLAN {vlan}" : "VLAN не назначен";
    string log = $"К порту {portNumber} ({vlanInfo}) подключено устройство: {target.name}";
    switchSimulator.LogAction(log);
}
else if (routerController != null && routerController.isActiveAndEnabled)
{
    vlanInfo = $"VLAN {portNumber}";
    string log = $"К порту {portNumber} (Router) подключено устройство: {target.name}";
    routerController.LogAction(log);
}

        }
    }
}
