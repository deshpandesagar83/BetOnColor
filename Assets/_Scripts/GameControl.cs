/*
* This is one of the Scripts I used in making a small betting game, the objective of the game is to place bet on a object
* The object can be either green or red
* The game is UI based, the logic for that is in another castle (Mario Refrence, script)
* The game has a countown timer to give the player certain time to make a bet
* This is a two player game, in a game only two players can join at a time
* I am chosing this script because I think it shows the range of my expreience with UI, networking, event handling, coroutines. There is more.
*/


/*Note:
* Here the Game Timer means the amount of time available for the player to place a bet
* The Pause Timer means a break time between two consecutive bets
* I have used two different coroutines for timers to try and optimize them. Stopping Coroutine and starting a new one
*/
using System;
using System.Collections;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine;


public class GameControl : MonoBehaviour
{
    private MeshRenderer betObject;                 // The mesh renderer of of this object
    [SerializeField]private GameObject playPanel;   //The Game Object for Playr Panel
    [SerializeField] private Text placeBetText;     // The text componet for Place Bet Text in the UI
    private string initialBetText;                  // String to store the initial value for place bet text
    
    [SerializeField] private Text timeText;         // The text component of the value of time on the UI
    private float gameTimeRemaining = 90;           // The amout of Max Game Time (90 = 1 Minutes 30 Secs)
    private float pauseTimeRemaining = 5;           // The amout of Max Pause Time 
    private WaitForSeconds waitTime;                // Variable to optimize Couroutine (creating new WaitforSeconds every time isnt efficient)

    private enum betState{                         // Enum for the state on which the player bet and Bet Object
        red,green,NOBET                            // No Bet means bet object color is default (Black)
    }
    betState playerBet,betColor;                    //Player bet is what player bet on, betColor is actual color of the object
    public static event Action<bool> onWinLoss;     //Event when win/loss is declared
    private bool isWin;                             //Variable for win/loss 
    
    private PhotonView PV;                          //The photon Network Component
    
    private void OnEnable(){
        //Regestering event when player makes a bet
        PlayerUIController.onBetCompleted += SetPlayerBetValue;
    }


    private void Start(){
        //Initializing all the values
        isWin = false;
        playerBet = betState.NOBET;
        betColor = betState.NOBET;
        initialBetText = placeBetText.text;
        betObject = GetComponent<MeshRenderer>();
        PV = GetComponent<PhotonView>();
        waitTime = new WaitForSeconds(1.0f);

        //Only start the coroutine on Master Client
        if(PhotonNetwork.IsMasterClient){
            StartCoroutine(StartGameCountDown());
        }
    }

    private void OnDisable(){
        //De-regestering event when player makes a bet
        PlayerUIController.onBetCompleted -= SetPlayerBetValue;
    }
    
    //CountDown function for the main Game timer
    private IEnumerator StartGameCountDown(){   
        var currentTime = gameTimeRemaining;
        while (currentTime > 0){
            int minutes = ((int)currentTime) / 60;
            int seconds = ((int)currentTime) % 60;
            // Resetting the timer text every sec
            timeText.text = (seconds < 10? String.Format("{0}:0{1}",minutes,seconds):String.Format("{0}:{1}",minutes,seconds));
            // Update the Time on All Other Clients
            PV.RPC("RPC_UpdateTimer", RpcTarget.OthersBuffered, timeText.text);
            yield return waitTime;
            currentTime--;
        }
        OnGameCountDownComplete();
        yield break;
    }
    
    // When the Game Timer reaches 0 make a roll for red or green and staring the pause timer
    private void OnGameCountDownComplete(){
        MakeRoll();
        playPanel.SetActive(false);
        //Stop the Game Timer and start the Pause Timer
        StopCoroutine(StartGameCountDown());
        StartCoroutine(StartPauseCountDown());
    }
    
    // Make a random roll for green and red
    private void MakeRoll(){
        bool randomRoll = (UnityEngine.Random.value>0.5);
        betColor = randomRoll? betState.green:betState.red;
        //Update the Bet Object and the Place Bet Text on both client
        PV.RPC("RPC_UpdateBetObject", RpcTarget.AllBuffered, betColor); 
    }
    
    //CountDown function for the Pause timer
    private IEnumerator StartPauseCountDown(){
        float currentTime = pauseTimeRemaining;
        while (currentTime > 0){
            int minutes = ((int)currentTime) / 60;
            int seconds = ((int)currentTime) % 60;
            // Resetting the timer text every sec
            timeText.text = (seconds < 10? String.Format("{0}:0{1}",minutes,seconds):String.Format("{0}:{1}",minutes,seconds));
            // Update the Time on All Other Clients
            PV.RPC("RPC_UpdateTimer", RpcTarget.OthersBuffered, timeText.text);
            yield return waitTime;
            currentTime--;
        }
        OnPauseCountDownComplete();
        yield break;
    }
    
    // When the Game Timer reaches 0, reset everything
    private void OnPauseCountDownComplete(){
        placeBetText.text = initialBetText;
        playerBet = betState.NOBET;
        betColor = betState.NOBET;
        betObject.material.color = Color.black;
        //Reset the Bet Object
        PV.RPC("RPC_UpdateBetObject", RpcTarget.AllBuffered, betColor);
        playPanel.SetActive(true);
        //Stop the Pause Timer and start the Game Timer
        StopCoroutine(StartPauseCountDown());
        StartCoroutine(StartGameCountDown());
    }
    
    // Event handler function
    private void SetPlayerBetValue(bool isGreen){
        //Is Green button pressed or Red button is pressed
        playerBet = (isGreen? betState.green:betState.red);
    }

    //RPC to update the timer text on all other clients
    [PunRPC]
    private void RPC_UpdateTimer(string time){
        timeText.text = time;
    }

    //RPC to Update the Bet Object and Place Bet Text on Master and other Clients
    [PunRPC]
    private void RPC_UpdateBetObject(betState betObjectState){
        switch(betObjectState){
            case betState.red:     betObject.material.color = Color.red;break;
            case betState.green:   betObject.material.color = Color.green;break;
            case betState.NOBET:   betObject.material.color = Color.black;break;
        }
        //The below behaviour is Asyncronous as playerBet is determined by the client and betObjectState is passed through Master
        if(betObjectState == betState.NOBET){
            placeBetText.text = initialBetText;
        }else{
            if(playerBet == betState.NOBET){
                placeBetText.text = String.Format("You Did Not Place A Bet");
            }else{
                if(betObjectState == playerBet){
                    placeBetText.text = "You Won";
                    isWin = true;
                }else{
                    placeBetText.text = "You Lost";
                    isWin = false;
                }
                onWinLoss?.Invoke(isWin);
            }
        }
        placeBetText.color = Color.white;
    }
}
