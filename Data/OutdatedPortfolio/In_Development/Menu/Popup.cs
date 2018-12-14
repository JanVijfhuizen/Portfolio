using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Popup : MonoBehaviour {

    public Text text;
    public InputField inputField;
    public delegate void OnExit(string text);
    public OnExit onExit;
    public Button continueBut, cancelBut;

    public void Open(string text, string input, bool buttons, OnExit func)
    {
        gameObject.SetActive(true);
        onExit = func;
        this.text.text = text;
        if (input.Length > 0)
        {
            inputField.gameObject.SetActive(true);
            inputField.text = input;
        }
        if (buttons)
        {
            continueBut.gameObject.SetActive(true);
            cancelBut.gameObject.SetActive(true);
        }
    }

    public void Close()
    {
        inputField.gameObject.SetActive(false);
        continueBut.gameObject.SetActive(false);
        cancelBut.gameObject.SetActive(false);
        gameObject.SetActive(false);

        inputField.text = "";
        text.text = "";
    }

	public void Done()
    {
        onExit(inputField.text);
        Close();
    }

    public void Cancel()
    {
        Close();
    }
}
