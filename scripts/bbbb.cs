using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bbbb : MonoBehaviour
{
    public InputField inputField;
    public string letter;

    public void OnButtonClick()
    {
        inputField.text += letter;
    }
}
