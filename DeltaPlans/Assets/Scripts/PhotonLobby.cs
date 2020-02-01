using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotonLobby : MonoBehaviourPunCallbacks, ILobbyCallbacks
{

    /// <summary>
    /// This script handles All networking logic up until the player joins a room (then it is passed on to PhotonRoom.cs)
    /// </summary>

    //Static Public
    public static PhotonLobby lobby; //Singleton Reference to the current lobby

    public Button _playButton;  //Play button (made interactable when client connects to master server)
    public TMP_InputField _joinCodeInputField;
    public TextMeshProUGUI _joinCodeWaitingRoomDisplay; //Displays the Join code for the current room in the waiting room
    public TMP_InputField _playerCap;
    public GameObject _connectionLoadingLoop;
    public GameObject _joinLoadingLoop;
    public GameObject _creategameLoadingLoop;
    public Button _joinButton;
    public Button _createButton;


    private void Awake()
    {
        lobby = this;   //Singleton Initialisation
    }

    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();   //Connect to master photon server 
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Player client successfully connected to master server.");
        PhotonNetwork.AutomaticallySyncScene = true;    //Automatically sync the scene with the master client
        _playButton.interactable = true;
        _connectionLoadingLoop.SetActive(false);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);


    }

    public void CreateGame() //Called when a user attempts to create a new game
    {
        _creategameLoadingLoop.SetActive(true);
        _createButton.enabled = false;
        string joinCode = GenerateRoomCode();   
        int playerCap = System.Convert.ToInt32(_playerCap.text);
        RoomOptions roomOptions = new RoomOptions { IsOpen = true, IsVisible = false, MaxPlayers = System.Convert.ToByte(playerCap)};
        PhotonNetwork.CreateRoom(joinCode, roomOptions);
    }

    public void JoinGame() //Called when user attempts to join a game using a Join Code
    {
        try
        {
            PhotonNetwork.JoinRoom(_joinCodeInputField.text);
            _joinLoadingLoop.SetActive(true);
            _joinButton.enabled = false;
        }
        catch (System.Exception)
        {
            _joinLoadingLoop.SetActive(false);
            _joinButton.enabled = true;
        }
    }

    public void LeaveGame() //Called when a user prematurely leaves the game
    {
        Debug.Log("Leaving Room");
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LeaveLobby();
    }

    public override void OnJoinRoomFailed(short returnCode, string message) //Called when a user fails to join a room
    {
        Debug.Log("Failed to join room, Error code:" + returnCode.ToString());
        base.OnJoinRoomFailed(returnCode, message);
    }

    public static string GenerateRoomCode() //Generate a random 4 character code comprised of capital letters
    {
        string validCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string tempCode = "";

        for (int i = 0; i < 4; i++)
        {
            int characterID = Random.Range(0, validCharacters.Length);  //Pick a character in the string of valid characters
            tempCode = tempCode + validCharacters[characterID];         //Add it to the code
        }

        return tempCode;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
