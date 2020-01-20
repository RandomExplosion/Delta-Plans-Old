using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainMenuClient : MonoBehaviour
{

    public GameObject[] _menuPages;
    public TMP_InputField _joinCodeInputField;
    public TMP_InputField _playerCapInputField;


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

    #region RootMenu
        public void PlayButtonPressed()
        {

        }

        public void OptionsButtonPressed()
        {

        }

        public void QuitButtonPressed()
        {
            #if UNITY_EDITOR == false
                        Application.Quit();
            #endif

            #if UNITY_EDITOR == true
                    UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    #endregion

    #region MultiplayerMenu

    public void ValidatePlayerCap()
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
