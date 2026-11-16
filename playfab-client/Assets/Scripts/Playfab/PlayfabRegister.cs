using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class PlayfabRegister : MonoBehaviour
{
    public LobbyManager lobbyManager;

    bool CheckforInput(bool username)
    {
        bool allowContinue = true;
        if (username)
        {
            if (lobbyManager.username.text == "")
                allowContinue = false;
        }
        if (lobbyManager.email.text == "")
                allowContinue = false;
        if (lobbyManager.password.text == "")
                allowContinue = false;

        return allowContinue;
    }
    public void OnRegisterClicked()
    {
        if (!CheckforInput(true))
            return;
        
        RegisterPlayFabUserRequest req = new RegisterPlayFabUserRequest
        {
            // You should use the values entered from the input field to register
            // There are certain email addresses not available
            // e.g. abc@test.com so ensure you try something else
            Email = lobbyManager.email.text,
            Username = lobbyManager.username.text,
            DisplayName = lobbyManager.username.text,
            Password = lobbyManager.password.text,
            RequireBothUsernameAndEmail = true
        };
        PlayFabClientAPI.RegisterPlayFabUser(req,
        // Callback function to handle successful registration
        // You can display the messages to the interface from here
        res =>
        {
            Debug.Log("User is successfully registered");
            lobbyManager.LoadGameScene();
        },
        // Callback function for any error encountered
        // You can display the messages to the interface from here
        err =>
        {
            Debug.Log("Error: " + err.ErrorMessage);
        }
        );
    }

    public void OnLoginClicked()
    {
        //if (!CheckforInput(false))
            //return;
        
        LoginWithEmailAddressRequest req = new LoginWithEmailAddressRequest
        {
            // You should use the values entered from input field
            Email = lobbyManager.email.text,
            Password = lobbyManager.password.text
        };
        PlayFabClientAPI.LoginWithEmailAddress(req,
            // Another way to register the callback function to handle
            // success and failure cases
            OnLoginSuccess, // Function defined below
            OnLoginFailure // Function defined below
        );
    }
    private void OnLoginSuccess(LoginResult result)
    {
        // Debug.Log("Congratulations, you made your first successful API call!");
        Debug.Log("Login is successful");
        lobbyManager.LoadGameScene();
    }
    private void OnLoginFailure(PlayFabError error)
    {
        // Debug.LogWarning("Something went wrong with your first API call. :(");
        // Debug.LogError("Here's some debug information:");
        Debug.LogError("Login failed with error: \n" + error.GenerateErrorReport());
    }
}
