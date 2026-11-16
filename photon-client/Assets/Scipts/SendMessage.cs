using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class SendMessage : MonoBehaviour
{
    public GameObject username;
    public GameObject password;
    public GameObject Item_name;
    public GameObject count;
    public FacebookHandler FBHandler;

    private RaiseEventListener eventListener;
    private Item itemSelected;

    // Start is called before the first frame update
    void Awake()
    {
        eventListener = GetComponent<RaiseEventListener>();
    }

    public void RemoveButtonOnClick(Inventory inventory)
    {
        itemSelected = inventory.itemSelected;
    }

    public void OnSendMessageButtonClicked(int eventCode)
    {
        // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
        if (PhotonNetwork.IsConnected)
        {
            byte[] content;
            string message = "";
            switch (eventCode)
            {
                case 2: // Log In
                    {
                        if (username.GetComponent<InputField>().text == string.Empty || password.GetComponent<InputField>().text == string.Empty)
                        {
                            eventListener.lobbyText.text = "Invalid Input";
                            return;
                        }
                        message = $"{username.GetComponent<InputField>().text},{password.GetComponent<InputField>().text}";
                        break;
                    }
                case 3: // Sign Up
                    {
                        if (username.GetComponent<InputField>().text == string.Empty || password.GetComponent<InputField>().text == string.Empty)
                        {
                            eventListener.lobbyText.text = "Invalid Input";
                            return;
                        }
                        message = $"{username.GetComponent<InputField>().text},{password.GetComponent<InputField>().text}";
                        break;
                    }
                case 4: // Add to Inventory
                    {
                        message = $"{Item_name.GetComponent<InputField>().text},{count.GetComponent<InputField>().text}";
                        break;
                    }
                case 5: // Remove from Inventory
                    {
                        message = $"{itemSelected.ID}";
                        break;
                    }
                // case 6 in Inventory.cs
                case 7: // Facebook LogIn
                    {
                        message = $"{FBHandler.info.name},{FBHandler.info.id}";
                        break;
                    }
                case 8: // Pass Reset
                    {
                        SendMail mailHandler = eventListener.mailHandler;
                        message = $"{mailHandler.Username.text},{mailHandler.NewPassword.text}";
                        break;
                    }
            }
            content = Encoding.Default.GetBytes(message);
            PhotonNetwork.RaiseEvent((byte)eventCode, content, RaiseEventOptions.Default, SendOptions.SendReliable);
        }
    }
}
