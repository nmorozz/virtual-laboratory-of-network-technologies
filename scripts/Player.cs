using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed_Move = 5f;
    private CharacterController player;
    
    void Start()
    {
        player = GetComponent<CharacterController>();
    }

    void Update()
    {
        Move();
    }

    void Move()
    {
        float x_Move = Input.GetAxis("Horizontal");
        float z_Move = Input.GetAxis("Vertical");

        // Берем направление движения относительно камеры
        Vector3 move_Direction = transform.right * x_Move + transform.forward * z_Move;
        
        // Обнуляем влияние вертикального наклона (игнорируем Y)
        move_Direction.y = 0f;

        // Нормализуем вектор, чтобы диагональное движение не ускоряло игрока
        if (move_Direction.magnitude > 1f)
            move_Direction.Normalize();

        player.Move(move_Direction * speed_Move * Time.deltaTime);
    }
}
