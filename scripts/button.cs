using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;
using Newtonsoft.Json;

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

    Debug.Log("Start: " + gameObject.name);

        if (terminalButton == null) Debug.LogWarning("terminalButton не назначен!");
        if (webButton == null) Debug.LogWarning("webButton не назначен!");
        if (commandButton == null) Debug.LogWarning("commandButton не назначен!");
        if (backButton == null) Debug.LogWarning("backButton не назначен!");
        if (inputField == null) Debug.LogWarning("inputField не назначен!");
        if (outputText == null) Debug.LogWarning("outputText не назначен!");

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

    private IEnumerator SendSyncData(object data)
    {
        string json = JsonConvert.SerializeObject(data);

        byte[] postData = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl + "/sync", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Состояние успешно отправлено на сервер.");
            }
            else
            {
                Debug.LogError("Ошибка при отправке sync: " + request.error);
            }
        }
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
        outputText.text = "";

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
