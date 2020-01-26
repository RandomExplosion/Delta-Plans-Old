using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainMenuClient : MonoBehaviour
{
    public static MainMenuClient _menuController;
    public GameObject[] _menuPages;
    public TMP_InputField _joinCodeInputField;
    public TMP_InputField _playerCapInputField;

    private void Awake()
    {
        #region Singleton Init
        if (_menuController == null)       //If the singleton has not been initialised
        {
            _menuController = this;
        }
        else
        {
            if (_menuController != this)   //If there is more than one instance and this is not the first (If player returns to main menu)
            {
                Destroy(gameObject);        //Destroy this instance
                return;
            }
        }
        DontDestroyOnLoad(gameObject);
        #endregion
    }

    public void ChangeMenuPage(int index)
    {
        if (index <= _menuPages.Length)
        {
            for (int i = 0; i < _menuPages.Length; i++)
            {
                _menuPages[i].SetActive(i == index);
            }
        }
        else
        {
            Debug.LogError("You are trying to go to a page that doesn't exist");
            throw new System.IndexOutOfRangeException();
        }
    }

    public void QuitButtonPressed() //When the player presses quit
    {
        #if UNITY_EDITOR == false
                Application.Quit();                                 //Quit application if running in build
        #endif

        #if UNITY_EDITOR == true
                UnityEditor.EditorApplication.isPlaying = false;    //Exit playmode if running in editor
        #endif
    }


    #region MultiplayerMenu

    public void ValidatePlayerCap() //Clamp the player cap (input field in create game menu) to make sure it is within the allowed range then pass it on to the lobby
    {
        int currentValue = System.Convert.ToInt32(_playerCapInputField.text);

        if (currentValue > 20)
        {
            _playerCapInputField.SetTextWithoutNotify("20");
            return;
        }
        else if (currentValue < 6)
        {
            _playerCapInputField.SetTextWithoutNotify("6");
            return;
        }
    }

    #endregion
}
