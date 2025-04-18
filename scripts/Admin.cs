using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Admin : MonoBehaviour
{
    [Header("Названия C#-скриптов для запуска (пример: DHCPClient)")]
    public List<string> scriptNames; // Список названий скриптов

    void Start()
    {
        RunScripts();
    }

    void RunScripts()
    {
        foreach (string scriptName in scriptNames)
        {
            Type scriptType = Type.GetType(scriptName);
            if (scriptType != null)
            {
                MonoBehaviour scriptComponent = (MonoBehaviour)gameObject.AddComponent(scriptType);
                scriptComponent.enabled = true;
                Debug.Log($"Запущен {scriptName}");
            }
            else
            {
                Debug.LogError($"Ошибка: Скрипт {scriptName} не найден!");
            }
        }
    }
}
