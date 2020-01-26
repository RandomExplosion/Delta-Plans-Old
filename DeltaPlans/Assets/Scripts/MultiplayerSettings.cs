using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerSettings : MonoBehaviour
{

    public static MultiplayerSettings _currentSettings; //Singleton

    //Scenes
    public int _menuscene = 0;
    public int _gameScene = 1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        #region Singleton Init
        if (_currentSettings == null)       //If the singleton has not been initialised
        {
            _currentSettings = this;        
        }
        else
        {
            if (_currentSettings != this)   //If there is more than one instance and this is not the first (If player returns to main menu)
            {
                Destroy(gameObject);        //Destroy this instance
                return;
            }
        }
        DontDestroyOnLoad(gameObject);
        #endregion
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
