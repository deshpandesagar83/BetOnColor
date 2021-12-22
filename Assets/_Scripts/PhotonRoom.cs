using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PhotonRoom : MonoBehaviourPunCallbacks,IInRoomCallbacks
{
    // Game Object for Player 2 Info Panel
    [SerializeField]private GameObject player2InfoPanel;
    // Game Object for Player 2 Moves Panel
    [SerializeField]private GameObject player2MovesPanel;
    //Component for Number of Players Text in Top Panel
    [SerializeField]private Text numberOfPlayer;

    public static event Action playerDisconnect;    //Event when a player is disconnected
    private void Start(){
        // Small hack so that the second client to connect always displays 2/2
        if(!PhotonNetwork.IsMasterClient){
            numberOfPlayer.text = "2/2";
        }
    }
    
    public override void OnPlayerEnteredRoom(Player player){
        player2InfoPanel.SetActive(true);
        player2MovesPanel.SetActive(true);
        numberOfPlayer.text = "2/2";
    }
    public override void OnPlayerLeftRoom(Player player){
        player2InfoPanel.SetActive(false);
        player2MovesPanel.SetActive(false);
        numberOfPlayer.text = "1/2";
        playerDisconnect?.Invoke();
    }

    
}
