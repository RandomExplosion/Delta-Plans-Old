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
    public int _pendingInviteSource;
    private bool _teamChanged;              //Marker for team list rebuilding

    //Vote to start prerequesites (all must be true for button to enable)
    private bool _notAloneInGame = false;
    private bool _inTeam = false;
    private float _lastsceneloadtime = 0;

    #region Classes

    [System.Serializable]
    public class Team
    {
        //Array of all Members (Note: index 0 is the team leader)
        public List<string> _members;
        public string _teamName;

        public Team(string leader, string teamName)
        {
            _members = new List<string>();
            _members.Add(leader);
            _teamName = teamName;
        }

        public bool IsPlayerInTeam(string userId)
        {
            foreach (string member in _members)
            {
                if (userId == member)
                {
                    return true;
                }
            }
            return false;
        }

        public void ChangeTeamName(string newName)
        {
            if (newName != "")
            {
                _teamName = newName; 
            }
        }

        public void AddPlayerToTeam(string userId)
        {
            _members.Add(userId);
        }

        public void RemovePlayerFromTeam(string userId)
        {
            _members.Remove(userId);
        }
    } 

    #endregion

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
            ExitGames.Client.Photon.Hashtable startingProperties = new ExitGames.Client.Photon.Hashtable { };
            startingProperties.Add("readyToPlay", false);
            startingProperties.Add("inTeam", false);
            PhotonNetwork.LocalPlayer.SetCustomProperties(startingProperties);
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
            ExitGames.Client.Photon.Hashtable startingProperties = new ExitGames.Client.Photon.Hashtable { };
            startingProperties.Add("readyToPlay", false);
            startingProperties.Add("inTeam", false);
            newPlayer.SetCustomProperties(startingProperties);
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

        if (_playerList.childCount != _photonPlayers.Length || _teamChanged == true)
        {
            _teamChanged = false;   //Reset Team Changed Marker

            //Delete all the current list entries
            for (int i = 0; i < _playerList.childCount; i++)
            {
                Destroy(_playerList.GetChild(i).gameObject);
            }

            //Instantiate new entries
            for (int i = 0; i < _photonPlayers.Length; i++)
            {
                GameObject newentry = Instantiate(_playerListEntryPrefab, _playerList);
                newentry.transform.GetChild(1).GetChild(1).GetComponent<Button>().onClick.AddListener(delegate { InvitePlayerToTeam(i); });   //Set Up Listener for Invite Button

                if (i == PhotonNetwork.LocalPlayer.ActorNumber - 1)
                {
                    newentry.transform.GetChild(1).GetChild(1).gameObject.GetComponent<Button>().interactable = false;  //Disable Invite Button
                }
                else if ((bool)_photonPlayers[i].CustomProperties["inTeam"] == true)    //If the player is already in a team
                {
                    newentry.transform.GetChild(1).GetChild(1).gameObject.SetActive(false);     //Hide Invite Button
                    newentry.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().SetText((string)_photonPlayers[i].CustomProperties["teamName"]);  //Display Team Name in place of Invite Button
                }
            }
        }

        //Update Ready to play Indicators, NickNames and Teams
        for (int i = 0; i < _photonPlayers.Length; i++)
        {
            _playerList.GetChild(i).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().SetText(_photonPlayers[i].NickName);
            bool readystate = (bool)_photonPlayers[i].CustomProperties["readyToPlay"];
            _playerList.GetChild(i).GetChild(2).GetChild(1).gameObject.SetActive(readystate);      //Appropriately toggle the ready to play indicator for the player
        } 
    }

    #endregion

    #region Team Organisation RPCS
        
    [PunRPC]
    public void RPC_TeamInvite(PhotonMessageInfo messageInfo)    //Sent to other players to invite them to your team
    {
        _pendingInviteSource = messageInfo.Sender.ActorNumber-1;
        TeamInvitePopup._currentPopup.ShowPopup();
    }

    [PunRPC]
    public void RPC_TeamInviteAccepted(PhotonMessageInfo messageinfo, string leaderId)   //Sent by the invitee to the masterclient to confirm a team change
    {
        _photonPlayers = PhotonNetwork.PlayerList;

        //If this is the master client
        if (PhotonNetwork.IsMasterClient)
        {
            List<Team> teams = PhotonNetwork.CurrentRoom.CustomProperties["Teams"] as List<Team>;

            //Remove the acceptee from any team they might be in
            for (int i = 0; i < teams.Count; i++)
            {
                if (teams[i].IsPlayerInTeam(messageinfo.Sender.UserId))
                {
                    teams[i].RemovePlayerFromTeam(messageinfo.Sender.UserId);
                }
            }

            bool joinExistingTeam = false;
            int existingteamindex = 0;

            //Check if the leader already has a team
            for (int i = 0; i < teams.Count; i++)
            {
                if (teams[i]._members[0] == leaderId)
                {
                    joinExistingTeam = true;    //We need to add the player to that team
                    existingteamindex = i;      //Record the index of the team
                }
            }

            Player teamLeader = null;
            for (int i = 0; i < _photonPlayers.Length; i++)
            {
                if (_photonPlayers[i].UserId == leaderId)
                {
                    teamLeader = _photonPlayers[i];
                    break;
                }
            }
            

            //If the team leader has a team already
            if (joinExistingTeam == true)
            {
                //Add the team to that
                teams[existingteamindex]._members.Add(messageinfo.Sender.UserId);
            }
            else
            {
                //Make a new team
                teams.Add(new Team(leaderId, teamLeader.NickName));
                //Add the player to that team
                teams[teams.Count - 1].AddPlayerToTeam(messageinfo.Sender.UserId);
            }

            //Serialise the new teams into a hashtable and Update the room properties
            ExitGames.Client.Photon.Hashtable updatedTeams = new ExitGames.Client.Photon.Hashtable { };
            updatedTeams.Add("Teams", teams);
            PhotonNetwork.CurrentRoom.SetCustomProperties(updatedTeams);

            //Mark the player as in a team
            ExitGames.Client.Photon.Hashtable inTeam = new ExitGames.Client.Photon.Hashtable { };
            inTeam.Add("inTeam", true);
            inTeam.Add("teamName", true);
            messageinfo.Sender.SetCustomProperties(inTeam);
            _pv.RPC("RPC_TeamChange", RpcTarget.All);
        }
    }

    [PunRPC]
    public void RPC_TeamChange()
    {
        _teamChanged = true;
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
        _pv.RPC("RPC_TeamInvite", _photonPlayers[playerIndex]);
    }

    public void AcceptPendingInvite()
    {
        _pv.RPC("RPC_TeamInviteAccepted", RpcTarget.MasterClient, _photonPlayers[_pendingInviteSource].UserId);
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
