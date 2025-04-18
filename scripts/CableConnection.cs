using UnityEngine;

public class CableConnection : MonoBehaviour
{
    public GameObject object1; // например, eth
    public GameObject object2; // например, path

    private bool object1Placed = false;
    private bool object2Placed = false;

    public Color connectedColor = Color.green;

    private Renderer rend1;
    private Renderer rend2;

    void Start()
    {
        rend1 = object1.GetComponent<Renderer>();
        rend2 = object2.GetComponent<Renderer>();
    }

    public void NotifyObjectPlaced(GameObject obj)
    {
        if (obj == object1)
        {
            object1Placed = true;
            Debug.Log("object1 установлен");
        }
        else if (obj == object2)
        {
            object2Placed = true;
            Debug.Log("object2 установлен");
        }

        // Если оба установлены — меняем цвет
        if (object1Placed && object2Placed)
        {
            SetConnectedColor();
        }
    }

    private void SetConnectedColor()
    {
        if (rend1 != null) rend1.material.color = connectedColor;
        if (rend2 != null) rend2.material.color = connectedColor;

        Debug.Log("Оба объекта установлены — соединение завершено");
    }
}
