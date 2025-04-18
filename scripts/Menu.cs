using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    //обозначение кнопок
    public Button one;
    public Button two;
    public Button three;
    public Button Exit;

    void Start()
    {
        //методы ожидания нажатия
        one.onClick.AddListener(toOne);
        two.onClick.AddListener(toTwo);
        three.onClick.AddListener(toThree);
        Exit.onClick.AddListener(toWindows);
    }
        //созданные методы
        void toOne(){
            SceneManager.LoadScene("1");
        }
        void toTwo(){
            SceneManager.LoadScene("2");
        }
        void toThree(){
            SceneManager.LoadScene("3");
        }
        void toWindows(){
           Application.Quit(); 
        }
}
