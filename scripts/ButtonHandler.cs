using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour
{
public GameObject cube; // ссылка на объект куба
public Button button; // ссылка на кнопку
public GameObject canvas;
public Color firstColor; // первый цвет, на который будет меняться цвет объекта
public Color secondColor; // второй цвет, на который будет меняться цвет объекта
private bool isSecondColor = false; // флаг, указывающий, какой цвет должен быть следующим
private int num = 0;
private void Start()
{
    canvas.SetActive(false);
    // добавляем обработчик события нажатия на кнопку
    button.onClick.AddListener(ChangeCubeColor);
    
}
private void ChangeCubeColor()
{
    
    // определяем, какой цвет должен быть следующим
    if (isSecondColor)
    {
        // присваиваем второй цвет кубу
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.material.color = secondColor;
        isSecondColor = false;
        num = 1;
          canvas.SetActive(true);
    }

    else
    {
        // присваиваем первый цвет кубу и запускаем таймер на смену цвета через секунду
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.material.color = firstColor;
        isSecondColor = true;
        StartCoroutine(ColorChangeTimer());
    }
}
private IEnumerator ColorChangeTimer()
{
    // ждем одну секунду
    yield return new WaitForSeconds(1f);
    // вызываем метод смены цвета для второго цвета
    ChangeCubeColor();
    // если второй раз меняем цвет

}
}