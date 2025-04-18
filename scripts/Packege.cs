using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class Packege : MonoBehaviour
{
public Button button1;
public Text outputText;
private void Start()
    {
        // добавляем обработчик нажатия на кнопку 1
        button1.onClick.AddListener(HideButton1);
        // скрываем элементы, которые должны быть скрыты при запуске игры
        HideElements();
    }
    private void HideElements()
    {
        // скрываем кнопку 2, InputField и outputText
      
       
        outputText.gameObject.SetActive(false);
    }
    private void ShowElements()
    {
        // показываем кнопку 2, InputField и outputText
        outputText.gameObject.SetActive(true);
    }
    private void HideButton1()
    {
        // скрываем кнопку 1 и показываем остальные элементы
        button1.gameObject.SetActive(false);
        ShowElements();
    }
}
