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
    float x_Move = 0f;
    float z_Move = 0f;

    if (Input.GetKey(KeyCode.LeftArrow))
        x_Move = -1f;
    else if (Input.GetKey(KeyCode.RightArrow))
        x_Move = 1f;

    if (Input.GetKey(KeyCode.UpArrow))
        z_Move = 1f;
    else if (Input.GetKey(KeyCode.DownArrow))
        z_Move = -1f;

    Vector3 move_Direction = transform.right * x_Move + transform.forward * z_Move;
    move_Direction.y = 0f;

    if (move_Direction.magnitude > 1f)
        move_Direction.Normalize();

    player.Move(move_Direction * speed_Move * Time.deltaTime);
}

}
