using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PhotonRoom : MonoBehaviourPunCallbacks, IInRoomCallbacks
{

    //Room Info
    public static PhotonRoom _currentRoom;  //Singleton
    private PhotonView _pv;                 //Photon View (see photon docs)
    public bool _inGame = false;            //Has this game started

    //Player Info
    public Player[] _photonPlayers;         //Photon data for each Player
    public bool[] _startVoteSheet;          //Array of bools to keep track of who wants to start
    public int _myPlayerIndex;              //Index of this player in _photonPlayers

    //UI Gunk
    public GameObject _voteToStartButton;
    public GameObject _cancelVote;
    public Transform _playerList;
    public GameObject _playerListEntryPrefab;

    //Vote to start prerequesites (all must be true for button to enable)
    private bool _notAloneInGame = false;
    private bool _inTeam = false;


    // Awake is called before the first frame update and before start
    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

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
        Debug.Log("Successfully Joined Room: " + PhotonNetwork.CurrentRoom);
        Debug.Log("Progressing to Teams Menu");
        _photonPlayers = PhotonNetwork.PlayerList;                  //Cache all the info for the players in the game
        _myPlayerIndex = _photonPlayers.Length - 1;                 //Get the index for this client's player (will be the last in the array at this point)
        PhotonNetwork.NickName = (_myPlayerIndex + 1).ToString();
        GameObject listentry = Instantiate(_playerListEntryPrefab, _playerList);    //Add an entry in the player list gui for us
        //Display Nickname
        MainMenuClient._menuController.gameObject.SetActive(false); //Disable the Main menu because it will not be destroyed when the game loads
        if (PhotonNetwork.IsMasterClient)                           //If this is the host client
        {
            _startVoteSheet = new bool[_photonPlayers.Length];  //Then there can't be anyone else on so initialise the votesheet
            SceneManager.LoadScene(MultiplayerSettings._currentSettings._gameScene);   //Load the game scene (applies to all other clients as well)
        }
        else
        {
            //There are other people in the server
            _notAloneInGame = true;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.Log("A new player has joined the room!");
        _photonPlayers = PhotonNetwork.PlayerList;
        _notAloneInGame = true;
        GameObject listentry = Instantiate(_playerListEntryPrefab, _playerList);    //Add an entry in the player list gui for this user
        if (PhotonNetwork.IsMasterClient)                           //If this is the master client send the new player the current votesheet (does not include the new player)
        {
            _pv.RPC("RPC_SendCurrentVotes", newPlayer, _startVoteSheet);
        }
                                                                    //Update our own copy of the votesheet with the new entry
        bool[] tempvotesheet = new bool[_photonPlayers.Length];
        for (int i = 0; i < tempvotesheet.Length; i++)
        {
            if (i != tempvotesheet.Length - 1)                      //If this isn't the last entry in the sheet
            {
                tempvotesheet[i] = _startVoteSheet[i];
            }
            else
            {
                tempvotesheet[i] = false;                           //This is the newly joined client's entry (the last) and will remain false until they say otherwise
            }
        }

        //Update the votesheet
        _startVoteSheet = tempvotesheet;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region Team Organisation RPCS
    [PunRPC]
    private void RPC_VoteToStart(int senderIndex) //Sent to All clients to signify that the sender is ready to start the game
    {
        _startVoteSheet[senderIndex] = true;

        //Update the player list to say that they want to start

        if (!PhotonNetwork.IsMasterClient)  //Stop here if not the master client
            return;
    }

    [PunRPC]
    private void RPC_CancelVoteToStart(int senderIndex) //Sent to All clients to signify that the sender is ready to start the game
    {
        _startVoteSheet[senderIndex] = false;

        //Update the player list to say that they want to start
    }

    [PunRPC]
    void RPC_SendCurrentVotes(bool[] votesheet) //Sent by the master client when a player joins the game to inform them of who wants to start
    {
        //Save the recieved Votesheet and add one entry for the newly joined player

        bool[] tempvotesheet = new bool[votesheet.Length + 1]; 
        for (int i = 0; i < tempvotesheet.Length; i++)
        {
            if (i != tempvotesheet.Length - 1) //If this isn't the last entry in the sheet
            {
                tempvotesheet[i] = votesheet[i]; 
            }
            else
            {
                tempvotesheet[i] = false;       //This is the newly joined client's entry and will remain false until they say otherwise
            }
        }

        //Update the votesheet
        _startVoteSheet = tempvotesheet;
    }

    #endregion

    private void OnSceneFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == MultiplayerSettings._currentSettings._menuscene)
        {
            MainMenuClient._menuController.ChangeMenuPage(0);
            MainMenuClient._menuController.gameObject.SetActive(true);
        }
        else if (scene.buildIndex == MultiplayerSettings._currentSettings._gameScene)
        {
            MainMenuClient._menuController.gameObject.SetActive(false);
        }

        _voteToStartButton = GameObject.FindGameObjectWithTag("VoteYesToStart");
        if (_voteToStartButton != null)
        {
            _voteToStartButton.GetComponent<Button>().onClick.AddListener(OnVoteYesButtonPressed);
        }
        _cancelVote = GameObject.FindGameObjectWithTag("RetractVote");
        if (_cancelVote != null)
        {
            _cancelVote.GetComponent<Button>().onClick.AddListener(OnCancelVoteButtonPressed);
        }
    }
    

    #region TeamSelectScreen

    public void OnVoteYesButtonPressed() //Client Only - Triggered By Button and sends rpc
    {
        _voteToStartButton.SetActive(false); //Hide vote to start button
        _cancelVote.SetActive(true); //Show the vote to cancel button
        _pv.RPC("RPC_VoteToStart", RpcTarget.AllViaServer, _myPlayerIndex); //Consider using buffered rpc
    }

    public void OnCancelVoteButtonPressed()
    {
        _voteToStartButton.SetActive(true); //Show vote to start button
        _cancelVote.SetActive(false); //Hide the vote to cancel button
        _pv.RPC("RPC_CancelVoteToStart", RpcTarget.AllViaServer, _myPlayerIndex); //Consider using buffered rpc
    }

    private void UpdateTeamsGUI()
    {
        
    }

    private void UpdatePlayerListGUI()
    {
        //Update Ready to play Indicators
        for (int i = 0; i < _playerList.childCount; i++)
        {
            _playerList.GetChild(i).GetChild(1).GetChild(1).gameObject.SetActive(_startVoteSheet[i]);
        }
    }

    #endregion
}
