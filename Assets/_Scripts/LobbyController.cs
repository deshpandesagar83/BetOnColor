// /*Note:
// * In this script I have used Text Mesh Pro but everywhere else I have used basic Unity UI
// * This is to showcase I am comfortable with both of these options
// */
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

public class LobbyController : MonoBehaviourPunCallbacks
{
    // Initializing Loading Button, Play button
    [SerializeField] private GameObject loading_btn;
    [SerializeField] private GameObject play_btn;
    // To display the error message when there is a connection problem
    [SerializeField] private GameObject failedToJoin_txt;
    [SerializeField] private GameObject reason_txt;

    //Text component for Reason game object
    private TMP_Text reasonText;
    private int maxPlayers = 2;
    // Creating a singleton variable
    public static LobbyController LC;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        //Making the public variable the singleton functionality
        if (LobbyController.LC == null)
        {
            LobbyController.LC = this;
        }
        else
        {
            if (LobbyController.LC != this)
            {
                Destroy(LobbyController.LC.gameObject);
                LobbyController.LC = this;
            }
        }
        DontDestroyOnLoad(this.gameObject);

        reasonText = reason_txt.GetComponent<TMP_Text>();

    }

    private void Start()
    {

    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Server Regoin: " + PhotonNetwork.CloudRegion);
        loading_btn.SetActive(false);
        play_btn.SetActive(true);
    }

    public void OnPlayClick()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Join Random Room Failed: " + message);
        RoomOptions myRoomOptions = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = (byte)maxPlayers };
        //Create random room number
        string number = Random.Range(0, 999).ToString();
        PhotonNetwork.CreateRoom("Room Number: " + number, myRoomOptions);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            reasonText.text = "Host has Disconneted";
            PhotonNetwork.LoadLevel(1);
        }
        Debug.Log("Joined a room");
    }

    // Creation can fail if randomly same number is assigned twice
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Create Room Failed: " + message);
        RoomOptions myRoomOptions = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = (byte)maxPlayers };
        string number = Random.Range(0, 999).ToString();
        PhotonNetwork.CreateRoom("Room Number: " + number, myRoomOptions);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        //Handled disconnect to some amount. Only works on Lobby Scene
        if (failedToJoin_txt != null)
        {
            failedToJoin_txt.SetActive(true);
        }
        if (reason_txt != null)
        {
            reasonText.text = cause.ToString();
            reason_txt.SetActive(true);
        }
    }
    public override void OnLeftRoom()
    {
        PhotonNetwork.LoadLevel(0);
    }
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        PhotonNetwork.LeaveRoom();
    }







}











