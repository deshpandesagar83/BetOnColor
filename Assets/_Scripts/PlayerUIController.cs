using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine;

public class PlayerUIController : MonoBehaviour
{
    [Header("Bottom Panel Controls")]
    [SerializeField] private Transform bottomPanel;             //Component for Bottom Panel in the UI
    private Button [] chipButtons;                              //Button Reference on the Bottom Panel
    [SerializeField] private Transform textHolderPanel;         //Component for the Text Holder Panel in Bottom Panel of UI
    // A Dictionary to hold the tags and text values of current bet placed from the Text Holder Panel
    Dictionary<string,Text> betValues = new Dictionary<string, Text>();   
    [SerializeField]private Button resetButton;                 //Reset Button on Game Info Panel on the UI 

    [Header("Info Panel Controls")]
    [SerializeField] private Text placeBetText;                 //Text component for the Place Bet text on UI
    [SerializeField] private Text infoText;                     //Text component for Info Text under Game Info Panel in the UI
    string initialInfoText;                                     //Variable to hold the initial value of the info Text
    
    [Header("Right Panel Controls")]
    [SerializeField] private Transform playerChipInfoPanel;     //Componenet for the Player Chip Info Panel in Player Info Panel in UI    
    // A Dictionary to hold the tags and text values of maximum available chips from the Player Chip Info Panel
    private Dictionary<string,Text> maxChipValue = new Dictionary<string, Text>();   
    // A new Dictionary was needed since Text cant be sent over on Photon                  
    private Dictionary<string,string> sendChipValueOverNetwork = new Dictionary<string, string>();

    [SerializeField]private Button greenButton;             //Button Component for the Green Button
    [SerializeField]private Button redButton;               //Button Component for the Red Button

    [Header("Left Panel Controls")]
    [SerializeField]private GameObject player2InfoPanel;        // Game Object for Player 2 Info Panel
    [SerializeField] private Transform player2ChipInfoPanel;    //Transform for Player 2 chip info panel in Player 2 Info Panel
    // A Dictionary to store player2 max chip values when it connects
    private Dictionary<string,Text> player2MaxChipValue = new Dictionary<string, Text>();
    
    [SerializeField]private GameObject player2MovesPanel;       // Game Object for Player 2 Moves Panel
    private Text player2Movetext;                               // Text component for Player 2 Move

    private PhotonView PV;                                          //The photon Network Component
    public static event Action<bool> onBetCompleted;                //Public variable for the event that is fired on bet completion
    private List<string> betNames = new List<string>();            //A list to hold which all chips have been bet

    private void OnEnable(){
        //Regestering event when player wins or loses a bet
        GameControl.onWinLoss += UpdateBetValues;
        PhotonRoom.playerDisconnect += player2Disconnect;
    }
    private void Start(){
        //Initializing the player2 panel
        foreach(Transform child in player2ChipInfoPanel){
            player2MaxChipValue.Add(child.tag,child.GetComponent<Text>());
            player2MaxChipValue[child.tag].text = "10";
        }
        
        PV = GetComponent<PhotonView>();
        if(!PhotonNetwork.IsMasterClient){
            player2InfoPanel.SetActive(true);
            player2MovesPanel.SetActive(true);
            PV.RPC("RPC_syncChipValues", RpcTarget.AllBuffered,sendChipValueOverNetwork);
        }
        //Initialize the three Dictionaries 
        foreach(Transform child in textHolderPanel){
            betValues.Add(child.tag,child.GetComponent<Text>());
            betValues[child.tag].text = "0";
        }
        foreach(Transform child in playerChipInfoPanel){
            maxChipValue.Add(child.tag,child.GetComponent<Text>());
            maxChipValue[child.tag].text = "10";
            sendChipValueOverNetwork.Add(child.tag,child.GetComponent<Text>().text);
        }

        // -1 because 10 Buttons and one Panel
        chipButtons = new Button[bottomPanel.childCount-1];
        for(int i=0;i<bottomPanel.childCount-1;i++){
            if(bottomPanel.GetChild(i).CompareTag("ChipButtons")){
                if(!bottomPanel.GetChild(i).TryGetComponent<Button>(out chipButtons[i]))
                    Debug.LogError("There is no Button Component on Child Object: "+bottomPanel.GetChild(i).name+". Make sure any new objects are added after the 10 buttons");
            }else Debug.LogError("No ChipButtons Tag found on "+bottomPanel.GetChild(i).name+" if it is not a chip button change the hierarchy to below the 10 buttons");
        }
        //Initialize the info text variable
        initialInfoText = infoText.text; 
        //Assigning a listener to the button since their functionality is almost identical 
        redButton.onClick.AddListener(delegate{OnPlaceBetClicked(false);}); 
        greenButton.onClick.AddListener(delegate{OnPlaceBetClicked(true);});
        //Since this Panel has only one child with Text component
        player2Movetext = player2MovesPanel.GetComponentInChildren<Text>();
        player2Movetext.text = "Thinking...";
    }
    
    private void OnDisable(){
        //De-regestering event when player wins or loses a bet
        GameControl.onWinLoss -= UpdateBetValues;
        PhotonRoom.playerDisconnect -= player2Disconnect;
    }
    
    // Method called when the event occurs Updating all the Bet values
    private void UpdateBetValues(bool isWin){
        if(isWin){
            //Get all the Key names which had some value when the bet was placed
            foreach(var index in betNames){
                // Its a win so multiply the original betValues by two and add them to existing alues
                int inititalValue = int.Parse(maxChipValue[index].text);
                int betValue = int.Parse(betValues[index].text)*2;
                maxChipValue[index].text = (inititalValue + betValue).ToString();
            }
            string playerAction = "Player 2 Won Last Round";
            PV.RPC("RPC_syncPlayer2Text", RpcTarget.OthersBuffered, playerAction);
        }else{
            string playerAction = "Thinking";
            PV.RPC("RPC_syncPlayer2Text", RpcTarget.OthersBuffered, playerAction);
        }
        
        //Set the buttons back to interactable
        greenButton.interactable = true;
        redButton.interactable = true;
        resetButton.interactable = true;
        foreach(var chipButton in chipButtons){
            chipButton.interactable = true;
        }

        infoText.text = initialInfoText;
        infoText.color = Color.white;
        
        checkGameOver();
        // Update the values before making the RPC
        foreach(var chipValue in maxChipValue){
            sendChipValueOverNetwork[chipValue.Key] = chipValue.Value.text; 
        }
        PV.RPC("RPC_syncChipValues", RpcTarget.AllBuffered, sendChipValueOverNetwork);
        //Clear the list and reset all the Bet Values
        betNames.Clear();
        OnResetBetValues();
    }
    //Check if its game over, if it is then refresh the coins
    private void checkGameOver(){
        int sum = 0;
        foreach(var chipValue in maxChipValue){
            int value = int.Parse(chipValue.Value.text);
            sum += value;
        }
        //if sume is 0 that means its a game over
        if(sum == 0){
            foreach(var chipValue in maxChipValue){
                chipValue.Value.text = "10";
            }
        }
    }
    //Method called when a player is disconnect and that event is fired
    private void player2Disconnect(){
        foreach(var chipValue in player2MaxChipValue){
            chipValue.Value.text = "10"; 
        }
        player2Movetext.text = "Thinking...";
        player2InfoPanel.SetActive(false);
        player2MovesPanel.SetActive(false);
    }
    //Function for when a bet is placed
    private void OnPlaceBetClicked(bool isGreen){ 
        int totalBet = CalculateTotal();
        if(isGreen){
            placeBetText.text = String.Format("You Bet {0} Chips On Green",totalBet);  
            placeBetText.color = Color.green;
            string playerAction = String.Format("Player 2 Bet {0} Chips On Green",totalBet);
            BetCompleted(true,playerAction);
        }else{
            placeBetText.text = String.Format("You Bet {0} Chips On Red", totalBet);  
            placeBetText.color = Color.red;
            string playerAction = String.Format("Player 2 Bet {0} Chips On Red",totalBet);
            BetCompleted(false,playerAction);
        }          
    }
    // Method called to calculate the total bet and set the max available chip values
    private int CalculateTotal(){
        int sum = 0;
        //Loop through all the Bet Values and add them up
        foreach(var betValue in betValues){
            if(maxChipValue.ContainsKey(betValue.Key)){
                int value = int.Parse(betValue.Value.text);
                int maxValue = int.Parse(maxChipValue[betValue.Key].text);
                // Subtract that value from current max as soon as the vet is placed
                maxValue -= value;
                maxChipValue[betValue.Key].text = maxValue.ToString();
                // Add only those values greater than 0 to the List which will act as the key to the dictionary
                if(value>0) betNames.Add(betValue.Key);            
                sum += value; 
            }else{
                Debug.LogError("The Dictonary does not contian the text value. Check the Text objects of Player Chip Info Panel and Text Holder Panel. Also make sure the tags are correctly set");
            }
        }
        return sum;    
    }
    // When the bet is completed do some UI changes and fire an event
    private void BetCompleted(bool isGreen,string playerAction){ 
        PV.RPC("RPC_syncPlayer2Text", RpcTarget.OthersBuffered, playerAction);
        //Once Bet is placed make sure the buttons are disabled 
        greenButton.interactable = false;
        redButton.interactable = false;
        resetButton.interactable = false;
        foreach(var chipButton in chipButtons){
            chipButton.interactable = false;
        }
        infoText.text = "The Bet Has Been Placed";
        infoText.color = Color.white;
        onBetCompleted?.Invoke(isGreen);
    }

    //Public Funtion assigned to all the Buttons in Bottom Panel UI
    public void ChipClicked(Text textValue){
        //Get the set text values set from the inspector
        int currentValue = int.Parse(textValue.text);
        //Check if the Dictonary contians the name (Helps when the UI is changed)
        if(maxChipValue.ContainsKey(textValue.tag)){
            //Get the max value for the color of chip with the same tag
            int maxValue = int.Parse(maxChipValue[textValue.tag].text);
            if(currentValue<maxValue){
                //Increment by 10 as per instructions
                currentValue += 10;
                textValue.text = currentValue.ToString();
                infoText.text = initialInfoText;
                infoText.color = Color.white;
            }else{
                //Info text helps the player to know that it cant be done
                infoText.text = "Maximum Reached Cannot Do That";
                infoText.color = Color.red;
            }
        }else{
            Debug.LogError("The Dictonary does not contian the text value. Check the Text objects of Player Chip Info Panel and Text Holder Panel. Also make sure the tags are correctly set");
        }
    }

    //Public Function called when the reset icon is clicked in Bottom Panel
    public void OnResetBetValues(){
        foreach(var betvalue in betValues){
            betvalue.Value.text = "0";
        }
        infoText.text = initialInfoText;
        infoText.color = Color.white;
    }

    //RPC method for syncronizing the Chip values when Player 2 Connects
    [PunRPC]
    private void RPC_syncChipValues(Dictionary<string,string> chipValues){
        foreach(var chipValue in chipValues){
            player2MaxChipValue[chipValue.Key].text = chipValue.Value;
        }
    }
    //RPC method for syncronizing the action of Player 2
    [PunRPC]
    private void RPC_syncPlayer2Text(string playerAction){
        player2Movetext.text = playerAction;
    }
}
