using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pickup : MonoBehaviour
{
    public GameObject camera;
    public float distance = 15f;
    public float rotationSpeed = 1000f;

    private GameObject currentObject; // Объект, который сейчас поднят
    private bool canPickUp = false; // Флаг, указывающий, можно ли поднять объект
    private GameObject highlightedObject = null; // Объект, на который смотрит игрок
    private Color originalColor; // Исходный цвет объекта

    void Update()
    {
        CheckForObject();

        if (Input.GetKeyDown(KeyCode.E)) PickUp();
        if (Input.GetKeyDown(KeyCode.Q)) Drop();

        if (canPickUp && currentObject != null)
        {
            float rotation = Input.GetAxis("Mouse ScrollWheel") * rotationSpeed;
            currentObject.transform.Rotate(0f, rotation, 0f);
        }
    }

    void CheckForObject()
    {
        RaycastHit hit;
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, distance))
        {
            if (hit.transform.CompareTag("object"))
            {
                if (highlightedObject != hit.transform.gameObject)
                {
                    ResetHighlight();
                    highlightedObject = hit.transform.gameObject;
                    Renderer objRenderer = highlightedObject.GetComponent<Renderer>();
                    if (objRenderer != null)
                    {
                        originalColor = objRenderer.material.color;
                        objRenderer.material.color = Color.yellow; // Изменяем цвет объекта
                    }
                }
                return;
            }
        }
        ResetHighlight();
    }

    void ResetHighlight()
    {
        if (highlightedObject != null)
        {
            Renderer objRenderer = highlightedObject.GetComponent<Renderer>();
            if (objRenderer != null)
            {
                objRenderer.material.color = originalColor; // Возвращаем исходный цвет
            }
            highlightedObject = null;
        }
    }

    void PickUp()
    {
        if (highlightedObject == null) return;

        currentObject = highlightedObject;
        ResetHighlight(); // Вернуть цвет перед поднятием

        Rigidbody rb = currentObject.GetComponent<Rigidbody>();
        Collider col = currentObject.GetComponent<Collider>();

        if (rb != null) rb.isKinematic = true;
        if (col != null) col.isTrigger = true;

        currentObject.transform.parent = transform;
        currentObject.transform.localPosition = Vector3.zero;
        currentObject.transform.localEulerAngles = new Vector3(10f, 0f, 0f);
        canPickUp = true;
    }

    void Drop()
    {
        if (currentObject == null) return;

        currentObject.transform.parent = null;
        Rigidbody rb = currentObject.GetComponent<Rigidbody>();
        Collider col = currentObject.GetComponent<Collider>();

        if (rb != null) rb.isKinematic = false;
        if (col != null) col.isTrigger = false;

        canPickUp = false;
        currentObject = null;
    }
}
