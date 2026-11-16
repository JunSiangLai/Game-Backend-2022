using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public InputField email;
    public InputField username;
    public InputField password;
    PlayfabRegister playfabRegister;
    //public PlayfabLogin playfabLogin;

    // Start is called before the first frame update
    void Start()
    {
        //playfabLogin.username = username;
        //playfabLogin.password = password;
        //playfabLogin.lobbyManager = this;
        playfabRegister = GetComponent<PlayfabRegister>();
        playfabRegister.lobbyManager = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }
}
