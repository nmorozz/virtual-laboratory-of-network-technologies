using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class button : MonoBehaviour
{
    public Button terminalButton;
    public Button webButton;
    public Button commandButton;
    public Button backButton;
    public Button[] webButtons;
    public InputField inputField;
    public Text outputText;
    
    private enum State { Main, WebInterface, Terminal, Content }
    private State currentState = State.Main;
    private string apiUrl = "http://127.0.0.1:5000/api";
    
    private void Start()
    {
        terminalButton.onClick.AddListener(ShowTerminal);
        webButton.onClick.AddListener(ShowWebInterface);
        commandButton.onClick.AddListener(ExecuteCommand);
        backButton.onClick.AddListener(GoBack);
        
        for (int i = 0; i < webButtons.Length; i++)
        {
            int index = i;
            webButtons[i].onClick.AddListener(() => OnWebButtonClicked(index));
        }
        
        HideAll();
    }
    
    private void HideAll()
    {
        foreach (Button btn in webButtons)
        {
            btn.gameObject.SetActive(false);
        }
        commandButton.gameObject.SetActive(false);
        inputField.gameObject.SetActive(false);
        outputText.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
        webButton.gameObject.SetActive(true);
        terminalButton.gameObject.SetActive(true);
    }
    
    private void ShowTerminal()
    {
        currentState = State.Terminal;
        webButton.gameObject.SetActive(false);
        terminalButton.gameObject.SetActive(false);
        inputField.gameObject.SetActive(true);
        outputText.gameObject.SetActive(true);
        commandButton.gameObject.SetActive(true);
        backButton.gameObject.SetActive(true);
    }
    
    private void ShowWebInterface()
    {
        currentState = State.WebInterface;
        webButton.gameObject.SetActive(false);
        terminalButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(true);
        foreach (Button btn in webButtons)
        {
            btn.gameObject.SetActive(true);
        }
    }
    
    private void OnWebButtonClicked(int index)
    {
        currentState = State.Content;
        string[] endpoints = { "switch", "ports", "vlans", "routing", "security", "qos", "diagnostics", "firmware", "snmp" };
        string[] titles = {
            "Мониторинг состояния коммутатора", "Управление портами", "VLAN",
            "Маршрутизация и коммутация", "Безопасность", "QoS",
            "Диагностика и отладка", "Обновление прошивки и резервное копирование",
            "SNMP и удаленное управление"
        };
        
        if (index < 0 || index >= endpoints.Length)
        {
            Debug.LogError("Некорректный индекс кнопки: " + index);
            return;
        }
        
        foreach (Button btn in webButtons)
            btn.gameObject.SetActive(false);
        
        outputText.gameObject.SetActive(true);
        backButton.gameObject.SetActive(true);
        
        Debug.Log("Запрос к API: " + apiUrl + "/" + endpoints[index]);
        FetchData(endpoints[index], titles[index]);
    }
    
    private void ExecuteCommand()
    {
        outputText.text = "Выполнение команды: " + inputField.text;
    }
    
    private void FetchData(string endpoint, string title)
    {
        StartCoroutine(GetRequest(apiUrl + "/" + endpoint, title));
    }
    
    private IEnumerator GetRequest(string url, string title)
{
    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            Debug.Log("Ответ от сервера: " + response);
            outputText.text = title + ":\n" + response;
        }
        else
        {
            outputText.text = "Ошибка загрузки данных: " + request.error;
            Debug.LogError("Ошибка запроса " + url + ": " + request.error);
        }
    }
}
    
    private void GoBack()
    {
        outputText.text = ""; // Очистка текстового поля
        
        switch (currentState)
        {
            case State.Terminal:
            case State.WebInterface:
                HideAll();
                break;
            case State.Content:
                ShowWebInterface();
                break;
        }
    }
}
