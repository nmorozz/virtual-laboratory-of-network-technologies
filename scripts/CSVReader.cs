using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class CSVReader : MonoBehaviour {
    // Объявляем переменные
    public TextMesh outputText; // Текстовое поле для вывода данных
    public GameObject button; // Кнопка для запуска чтения данных
    private string[] lines; // Массив строк для хранения данных из CSV-файла
    private int currentLine = 0; // Индекс текущей строки в массиве данных

    void Start()
    {
        // Задаем путь к файлу CSV
        string filePath = "D:/Program Files (x86)/Unity Hub/project/diplom/Assets/wire.csv";
        // Считываем все строки из файла и сохраняем их в массив
        lines = File.ReadAllLines(filePath);
        // Запускаем корутину для построчного чтения и вывода данных
        StartCoroutine(ReadAndShowLines());
    }

    // Корутина для построчного чтения и вывода данных
    IEnumerator ReadAndShowLines()
    {
        // Цикл, выполняющийся до тех пор, пока не будут прочитаны все строки
        while (currentLine < lines.Length)
        {
            // Разбиваем текущую строку на отдельные значения, используя запятую в качестве разделителя
            string[] values = lines[currentLine].Split(',');
            string outputLine = "";

            // Собираем строку для вывода, объединяя все значения через пробел
            for (int i = 0; i < values.Length; i++)
            {
                outputLine += values[i] + " ";
            }

            // Выводим строку в текстовое поле и переходим к следующей строке
            outputText.text += outputLine + "\n";
            currentLine++;

            // Ждем 1 секунду перед чтением следующей строки
            yield return new WaitForSeconds(1f);
        }
    }

    // Метод, вызываемый каждый кадр
    void Update()
    {
        // Если пользователь нажал кнопку "Submit" и кнопка запуска корутины доступна для нажатия
        if (Input.GetButtonDown("Submit") && button.GetComponent<Button>().interactable)
        {
            // Запускаем корутину для построчного чтения и вывода данных
            StartCoroutine(ReadAndShowLines());
        }
    }
}