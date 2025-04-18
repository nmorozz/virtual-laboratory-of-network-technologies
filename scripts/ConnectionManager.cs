using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public GameObject[] switchPorts;  // Порты на коммутаторе
    public GameObject[] computerPorts; // Порты на компьютерах
    public GameObject[] cables;        // Два провода

    private Dictionary<GameObject, bool> portStatus = new Dictionary<GameObject, bool>();

    void Start()
    {
        // Инициализируем состояние портов (все Down)
        foreach (GameObject port in switchPorts)
            portStatus[port] = false;
        
        foreach (GameObject port in computerPorts)
            portStatus[port] = false;
    }

    void OnTriggerEnter(Collider other)
    {
        foreach (GameObject cable in cables)
        {
            if (other.gameObject == cable.transform.GetChild(0).gameObject) // Часть провода для компьютера
            {
                GameObject connectedPort = FindConnectedPort(computerPorts, cable.transform.GetChild(0).gameObject);
                if (connectedPort != null)
                {
                    portStatus[connectedPort] = true;
                    Debug.Log(connectedPort.name + " is now UP");
                }
            }
            else if (other.gameObject == cable.transform.GetChild(1).gameObject) // Часть провода для коммутатора
            {
                GameObject connectedPort = FindConnectedPort(switchPorts, cable.transform.GetChild(1).gameObject);
                if (connectedPort != null)
                {
                    portStatus[connectedPort] = true;
                    Debug.Log(connectedPort.name + " is now UP");
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        foreach (GameObject cable in cables)
        {
            if (other.gameObject == cable.transform.GetChild(0).gameObject) // Часть провода для компьютера
            {
                GameObject disconnectedPort = FindConnectedPort(computerPorts, cable.transform.GetChild(0).gameObject);
                if (disconnectedPort != null)
                {
                    portStatus[disconnectedPort] = false;
                    Debug.Log(disconnectedPort.name + " is now DOWN");
                }
            }
            else if (other.gameObject == cable.transform.GetChild(1).gameObject) // Часть провода для коммутатора
            {
                GameObject disconnectedPort = FindConnectedPort(switchPorts, cable.transform.GetChild(1).gameObject);
                if (disconnectedPort != null)
                {
                    portStatus[disconnectedPort] = false;
                    Debug.Log(disconnectedPort.name + " is now DOWN");
                }
            }
        }
    }

    private GameObject FindConnectedPort(GameObject[] ports, GameObject cableEnd)
    {
        foreach (GameObject port in ports)
        {
            if (Vector3.Distance(port.transform.position, cableEnd.transform.position) < 0.2f)
                return port;
        }
        return null;
    }

    public bool GetPortStatus(GameObject port)
    {
        return portStatus.ContainsKey(port) ? portStatus[port] : false;
    }
}
