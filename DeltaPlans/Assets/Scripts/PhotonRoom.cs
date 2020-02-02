using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PhotonRoom : MonoBehaviourPunCallbacks, IInRoomCallbacks
{

    /// <summary>
    /// NOTES FOR LOGGING OFF
    /// LEARN ABOUT CUSTOM USER PROPERTIES and use them for players being ready to play
    /// 
    /// </summary>


    //Room Info
    public static PhotonRoom _currentRoom;  //Singleton
    private PhotonView _pv;                 //Photon View (see photon docs)
    public bool _inGame = false;            //Has this game started

    //Player Info
    public Player[] _photonPlayers;         //Photon data for each Player
    public int _myPlayerIndex;              //Index of this player in _photonPlayers

    //UI Gunk
    public GameObject _readyToStartButton;
    public GameObject _notReadyToStartButton;
    public Transform _playerList;
    public GameObject _playerListEntryPrefab;

    //Vote to start prerequesites (all must be true for button to enable)
    private bool _notAloneInGame = false;
    private bool _inTeam = false;
    private float _lastsceneloadtime = 0;

    // Awake is called before the first frame update and before start
    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        SceneManager.sceneLoaded += OnSceneFinishedLoading;
        #region Singleton Init
        if (_currentRoom == null)       //If the singleton has not been initialised
        {
            _currentRoom = this;
        }
        else
        {
            if (_currentRoom != this)   //If there is more than one instance and this is not the first (If player joins a new game)
            {
                Destroy(_currentRoom.gameObject);       //Destroy the old instance
                _currentRoom = this;                    //Replace it with this
            }
        }
        DontDestroyOnLoad(gameObject);
        #endregion
    }

    // Start is called before the first frame update
    void Start()
    {
        _pv = GetComponent<PhotonView>();

    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
        SceneManager.sceneLoaded += OnSceneFinishedLoading;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
        SceneManager.sceneLoaded -= OnSceneFinishedLoading;
    }


    public override void OnJoinedRoom() //Called when the player joins the room
    {
        base.OnJoinedRoom();
        Debug.Log("Successfully Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        Debug.Log("Progressing to Teams Menu");
        _photonPlayers = PhotonNetwork.PlayerList;                                                                      //Cache all the info for the players in the game
        _myPlayerIndex = _photonPlayers.Length - 1;                                                                     //Get the index for this client's player (will be the last in the array at this point)
        PhotonNetwork.NickName = (_myPlayerIndex + 1).ToString();

        MainMenuClient._menuController.gameObject.SetActive(false);                                                     //Disable the Main menu because it will not be destroyed when the game loads
        if (PhotonNetwork.IsMasterClient)                                                                               //If this is the host client
        {
            ExitGames.Client.Photon.Hashtable readyProperties = new ExitGames.Client.Photon.Hashtable { };
            readyProperties.Add("readyToPlay", false);
            PhotonNetwork.LocalPlayer.SetCustomProperties(readyProperties);
            SceneManager.LoadScene(MultiplayerSettings._currentSettings._gameScene);                                    //Load the game scene (applies to all other clients as well after they join
        }
        else
        {
            _notAloneInGame = true;                                                                                     //Because we just joined and are not the host player there must be other people in the server
        }

        InvokeRepeating("UpdatePlayerListGUI", 1f, 0.5f);

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.Log("A new player has joined the room!");
        _photonPlayers = PhotonNetwork.PlayerList;
        _notAloneInGame = true;

        if (PhotonNetwork.IsMasterClient)
        {
            //Init Ready Property
            ExitGames.Client.Photon.Hashtable readyProperties = new ExitGames.Client.Photon.Hashtable { };
            readyProperties.Add("readyToPlay", false);
            newPlayer.SetCustomProperties(readyProperties);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.Log("Player: \"" + otherPlayer.NickName + "\" Has left the Game");

        //Find out which player just left and remove their entry in the player list
        for (int i = 0; i < _photonPlayers.Length; i++)
        {
            if (_photonPlayers[i].UserId == otherPlayer.UserId)
            {
                Debug.Log("Removing their Entry in the player list (Index: " + i.ToString() + ")");;
                Destroy(_playerList.GetChild(i).gameObject);
                _photonPlayers = PhotonNetwork.PlayerList;
                return;
            }
        }
    }

    void Update()
    {
        if (_lastsceneloadtime == 0f)
        {
            _lastsceneloadtime = Time.time;
        }
    }

    #region Game Prep RPCS

    [PunRPC]
    private void RPC_ChangeReadyState(bool state, PhotonMessageInfo info)
    {
        ExitGames.Client.Photon.Hashtable readyProperties = new ExitGames.Client.Photon.Hashtable { };
        readyProperties.Add("readyToPlay", state);
        info.Sender.SetCustomProperties(readyProperties);

        //_pv.RPC("UpdatePlayerListGUI", RpcTarget.All, false);
    }

    [PunRPC]
    private void UpdatePlayerListGUI()  //Sent by the master client to all other players, refreshes the ready state, nickname and team of each (can also regenerate the list)
    {

        _photonPlayers = PhotonNetwork.PlayerList;

        if(_photonPlayers.Length == 0)
        {
            Debug.LogError("Player List Is Empty, Something is VERY Wrong");
            return;
        }

        if (_playerList.childCount != _photonPlayers.Length)
        {
            //Delete all the current list entries
            for (int i = 0; i < _playerList.childCount; i++)
            {
                Destroy(_playerList.GetChild(i).gameObject);
            }

            //Instantiate new entries
            for (int i = 0; i < _photonPlayers.Length; i++)
            {
                GameObject newentry = Instantiate(_playerListEntryPrefab, _playerList);
                newentry.transform.GetChild(1).GetChild(1).GetComponent<Button>().onClick.AddListener(delegate { InvitePlayerToTeam(i); });
            }
        }

        //Update Ready to play Indicators, NickNames and Teams
        for (int i = 0; i < _photonPlayers.Length; i++)
        {
            bool readystate = (bool)_photonPlayers[i].CustomProperties["readyToPlay"];
            _playerList.GetChild(i).GetChild(2).GetChild(1).gameObject.SetActive(readystate);      //Appropriately toggle the ready to play indicator for the player
            _playerList.GetChild(i).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().SetText(_photonPlayers[i].NickName);
        } 
    }

    #endregion

    #region Team Organisation RPCS
        
        [PunRPC]
        public void RPC_TeamInvite()
        {
            
        }

    #endregion

    private void OnSceneFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if ((Time.time - _lastsceneloadtime) >= 0.001)
        {
            _lastsceneloadtime = Time.time;
            if (scene.buildIndex == MultiplayerSettings._currentSettings._menuscene)
            {
                MainMenuClient._menuController.ChangeMenuPage(0);               //When the menu scene is loaded (not for the first time) make sure it is on the root page
                MainMenuClient._menuController.gameObject.SetActive(true);      //And make sure it is turned on
            }
            else if (scene.buildIndex == MultiplayerSettings._currentSettings._gameScene)
            {
                MainMenuClient._menuController.gameObject.SetActive(false);     //When the game scene is loaded turn off the menu canvas
                _playerList = GameObject.FindGameObjectWithTag("PlayerListGUI").transform;
                GameObject.Find("JoinCodeDisplay").GetComponent<TextMeshProUGUI>().SetText("Join Code: " + PhotonNetwork.CurrentRoom.Name);
                //GameObject.Find("RefreshList").GetComponent<Button>().onClick.AddListener(delegate{ UpdatePlayerListGUI(true); });

                //Set up onclick listeners for the ready to play button and not ready to play button
                _readyToStartButton = GameObject.FindGameObjectWithTag("ReadyToStartButton");
                if (_readyToStartButton != null)
                {
                    _readyToStartButton.GetComponent<Button>().onClick.AddListener(OnReadyToPlayButtonPressed);
                }
                _notReadyToStartButton = GameObject.FindGameObjectWithTag("NotReadyToStartButton");
                if (_notReadyToStartButton != null)
                {
                    _notReadyToStartButton.GetComponent<Button>().onClick.AddListener(OnNotReadyToStartButtonPressed);
                    _notReadyToStartButton.SetActive(false);
                }

                if (PhotonNetwork.IsMasterClient)
                {
                    UpdatePlayerListGUI();
                }
            }  
        }
    }

    #region TeamSelectScreen

    public void OnReadyToPlayButtonPressed() //Client Only - Triggered By Button and sends rpc
    {
        _readyToStartButton.SetActive(false); //Hide vote to start button
        _notReadyToStartButton.SetActive(true); //Show the vote to cancel button
        _pv.RPC("RPC_ChangeReadyState", RpcTarget.MasterClient, true); //Tell the master client that we are now ready to play
    }

    public void OnNotReadyToStartButtonPressed()
    {
        _readyToStartButton.SetActive(true); //Show vote to start button
        _notReadyToStartButton.SetActive(false); //Hide the vote to cancel button
        _pv.RPC("RPC_ChangeReadyState", RpcTarget.MasterClient, false); //Tell the master client that we are not ready to play anymore
    }

    public void InvitePlayerToTeam(int playerIndex)
    {
        _pv.RPC("TeamInvite", _photonPlayers[playerIndex]);
    }

    private void UpdateTeamsGUI()
    {
        
    }

    private void OnApplicationQuit()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    #endregion
}
