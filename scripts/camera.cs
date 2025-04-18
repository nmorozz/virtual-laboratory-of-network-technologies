using System.Collections;
 using System.Collections.Generic;
  using UnityEngine;

public class camera : MonoBehaviour { 
  // переменные для хранения значений перемещения камеры 
  private float X, Y, Z; 
  // скорость, с которой происходитещение камеры (настраивается в инспекторе)
  public int speeds; 
  // переменные для хранения углов поворота камеры на оси x и y
   private float eulerX = 0, eulerY = 0; 
   void Start () {
    Cursor.lockState = CursorLockMode.None;    // скрытие курсора при запуске игры
}

void Update () {
    X = Input.GetAxis("Mouse X") * speeds * Time.deltaTime;     // получение изменения позиции мыши по оси X
    Y = -Input.GetAxis("Mouse Y") * speeds * Time.deltaTime;    // получение изменения позиции мыши по оси Y (приводит к инверсии направления движения)
    eulerX = (transform.rotation.eulerAngles.x + Y) % 360;     // установка нового угла по оси X путем добавления изменения по оси Y к текущему углу
    eulerY = (transform.rotation.eulerAngles.y + X) % 360;     // установка нового угла по оси Y путем добавления изменения по оси X к текущему углу
    transform.rotation = Quaternion.Euler(eulerX, eulerY, 0);   // установка нового повор камеры с использованием новых значений углов
    
    if (Input.GetKeyUp (KeyCode.Escape)) {      // обнаружение события нажатия клавиши Escape
        Cursor.lockState = CursorLockMode.None;  // разблокировка курсора при нажатии на Escape
    }
}
}