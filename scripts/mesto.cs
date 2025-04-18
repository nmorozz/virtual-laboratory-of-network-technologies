using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mesto : MonoBehaviour
{
    [Header("Настройки подключения")]
    public int portNumber;
    public GameObject object1;
    public GameObject object2;
    public Vector3 rotationCoordinates;

    [Header("Ссылки на системы")]
    public SwitchSimulator switchSimulator;

    [Header("Информация (только для чтения)")]
    [SerializeField] private string vlanInfo = "Не подключен"; // Отображается в инспекторе

    void OnTriggerEnter(Collider other)
    {
        GameObject target = other.gameObject;

        if (target == object1 || target == object2)
        {
            target.transform.position = transform.position;
            target.transform.rotation = Quaternion.Euler(rotationCoordinates);

            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
                rb.constraints = RigidbodyConstraints.FreezePosition;

            if (switchSimulator != null)
            {
                int vlan = switchSimulator.GetVlanForPort(portNumber);
                vlanInfo = vlan != -1 ? $"VLAN {vlan}" : "VLAN не назначен"; // обновление инфо
                string log = $"К порту {portNumber} ({vlanInfo}) подключено устройство: {target.name}";
                switchSimulator.LogAction(log);
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

        if (target == object1 || target == object2)
        {
            if (switchSimulator != null)
            {
                int vlan = switchSimulator.GetVlanForPort(portNumber);
                vlanInfo = vlan != -1 ? $"VLAN {vlan}" : "VLAN не назначен"; // обновление инфо
                string log = $"От порта {portNumber} ({vlanInfo}) отключено устройство: {target.name}";
                switchSimulator.LogAction(log);
            }
        }
    }
}
