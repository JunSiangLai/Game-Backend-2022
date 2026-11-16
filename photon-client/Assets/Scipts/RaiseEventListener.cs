using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ExitGames.Client.Photon;
using Photon.Pun;

public class RaiseEventListener : MonoBehaviourPun
{
    public Text lobbyText;

    public Canvas lobbyCanvas;
    public CanvasGroup lobbycanvasGroup;
    public Canvas inventoryCanvas;
    public CanvasGroup inventorycanvasGroup;

    public Inventory inventory;
    public SendMail mailHandler;

    private bool transition = false;

    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    private void Update()
    {
        if (transition)
        {
            LobbyInventoryTransition();
        }
    }

    private void OnEvent(EventData obj)
    {
        Debug.Log("EventCode: " + obj.Code);
        switch (obj.Code)
        {
            case 2: // Log In
                {
                    if ((bool)obj.Parameters[245])
                    {
                        LobbyInventoryTransitionPrep();
                    }
                    else
                    {
                        lobbyText.text = (string)obj.Parameters[244];
                    }
                    break;
                }
            case 3: // Sign Up
                {
                    lobbyText.text = obj.CustomData.ToString();
                    break;
                }
            case 4: // Add
                {
                    if ((bool)obj.Parameters[244])
                    {
                        inventory.InventoryCustomUpdate((string)obj.Parameters[245]);
                    }
                    else
                        Debug.Log((string)obj.Parameters[245]);
                    break;
                }
            case 5: // Remove
                {
                    if ((bool)obj.Parameters[244])
                    {
                        if (!string.IsNullOrEmpty((string)obj.Parameters[245]))
                            inventory.InventoryCustomUpdate((string)obj.Parameters[245]);
                        else
                            inventory.InventoryCustomDelete((string)obj.Parameters[243]);
                    }
                    else
                        Debug.Log((string)obj.Parameters[245]);
                    break;
                }
            // case 6 in Inventory.cs
            case 7: // Facebook
                {
                    if ((bool)obj.Parameters[245])
                    {
                        LobbyInventoryTransitionPrep();
                    }
                    else
                    {
                        Debug.Log(obj.CustomData.ToString());
                    }
                    break;
                }
            case 8:
                {
                    if ((bool)obj.Parameters[245])
                    {
                        mailHandler.CodeAccepted();
                    }
                    break;
                }
            case 9: // Disconnect
                {
                    Com.MyCompany.MyGame.Launcher.Disconnect();
                    break;
                }
        }
    }

    public void LobbyInventoryTransitionPrep()
    {
        inventorycanvasGroup.alpha = 0;
        inventoryCanvas.gameObject.SetActive(true);
        inventorycanvasGroup.interactable = false;
        lobbycanvasGroup.interactable = false;
        transition = true;

        Inventory.ReqUpdate();
    }

    private void LobbyInventoryTransition()
    {
        if (lobbycanvasGroup.alpha < 0.2 && inventorycanvasGroup.alpha < 1)
        {
            inventorycanvasGroup.alpha += Time.deltaTime;
        }
        else if (inventorycanvasGroup.alpha == 1)
        {
            transition = false;
            inventorycanvasGroup.interactable = true;
        }

        if (lobbycanvasGroup.alpha > 0)
        {
            lobbycanvasGroup.alpha -= Time.deltaTime;
        }
        else if (lobbyCanvas.gameObject.activeSelf && transition == false)
        {
            lobbycanvasGroup.alpha = 1;
            lobbycanvasGroup.interactable = true;
            lobbyCanvas.gameObject.SetActive(false);
        }
    }
}