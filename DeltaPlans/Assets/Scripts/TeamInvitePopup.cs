using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using TMPro;

public class TeamInvitePopup : MonoBehaviour
{
    public static TeamInvitePopup _currentPopup;

    public Button _acceptButton;
    public Button _declineButton;
    public TextMeshProUGUI _popupText;

    private void Awake()
    {
        _currentPopup = this;

        _acceptButton.onClick.AddListener(PhotonRoom._currentRoom.AcceptPendingInvite);
        _declineButton.onClick.AddListener(delegate { PhotonRoom._currentRoom._pendingInviteSource = -1; });
        gameObject.SetActive(false);
    }

    public void ShowPopup()
    {
        gameObject.SetActive(true);
        _popupText.SetText(PhotonRoom._currentRoom._photonPlayers[PhotonRoom._currentRoom._pendingInviteSource].NickName + " wants you on their team!");
    }

}
