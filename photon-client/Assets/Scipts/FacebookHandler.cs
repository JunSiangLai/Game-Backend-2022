using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Facebook.Unity;
using Facebook.MiniJSON;
using System;

public class FacebookHandler : MonoBehaviour
{
    public Text code;
    public Text timer;
    public Canvas canvas;
    public RawImage image;
    public Text Username;
    public GameObject request;
    public GameObject result;
    private FBResponse response;
    private FBAccount account;
    public FBAccInfo info;
    private bool codeSent;
    private float time = 0;
    private float expire = 0;

    private void Awake()
    {
        FB.Init(SetInit, OnHideUnity);
        time = 0;
        expire = 0;
        codeSent = false;
    }

    private void Update()
    {
        if (!request)
            return;

        if (codeSent)
        {
            if (time <= 0)
            {
                StartCoroutine(PostAuthPoll("https://graph.facebook.com/v2.6/device/login_status"));
                time = 5;
            }
            else
                time -= Time.deltaTime;
        }

        if (expire > 0)
        {
            expire -= Time.deltaTime;
            UpdateTimer();
        }
    }

    private void UpdateTimer()
    {
        timer.text = $"Expires in {TimeSpan.FromSeconds(expire).Minutes} minutes & {TimeSpan.FromSeconds(expire).Seconds} seconds";
    }

    void SetInit()
    {
        if (FB.IsLoggedIn)
        {
            Debug.Log("Logged in");
        }
        else
            Debug.Log("Not logged in");
    }

    void OnHideUnity(bool isGameShown)
    {
        if (isGameShown)
        {
            Time.timeScale = 1;
        }
        else
            Time.timeScale = 0;

    }

    public void FBLogIn()
    {
        StartCoroutine(PostRequest("https://graph.facebook.com/v2.6/device/login"));
    }

    public void FBLogInCancel()
    {
        codeSent = false;
        time = 0;
    }

    IEnumerator PostRequest(string url)
    {
        WWWForm form = new WWWForm();
        form.AddField("access_token", "YOUR_FB_APP_ID|YOUR_FB_CLIENT_TOKEN");
        form.AddField("scope", "public_profile");

        UnityWebRequest uwr = UnityWebRequest.Post(url, form);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.DataProcessingError || uwr.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            if (uwr.downloadHandler.data.Length > 0 && uwr.downloadHandler.error == string.Empty)
            {
                Debug.Log("Received: " + uwr.downloadHandler.text);
                response = JsonUtility.FromJson<FBResponse>(uwr.downloadHandler.text);
                if (response.error != null)
                {
                    Debug.Log(response.error);
                }
                else
                {
                    code.text = response.user_code;
                    expire = response.expires_in;
                    codeSent = true;
                }
            }
        }
    }
    IEnumerator PostAuthPoll(string url)
    {
        WWWForm form = new WWWForm();
        form.AddField("access_token", "YOUR_FB_APP_ID|YOUR_FB_CLIENT_TOKEN");
        form.AddField("code", response.code);

        UnityWebRequest uwr = UnityWebRequest.Post(url, form);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.DataProcessingError || uwr.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            if (uwr.downloadHandler.data.Length > 0 && uwr.downloadHandler.error == string.Empty)
            {
                Debug.Log("Received: " + uwr.downloadHandler.text);
                account = JsonUtility.FromJson<FBAccount>(uwr.downloadHandler.text);
                if (account.access_token == null)
                {
                    Debug.Log("Account Error");
                }
                else
                {
                    StartCoroutine(GetRequest("https://graph.facebook.com/v2.3/me?"));
                    //FB.API("/me?fields=name", HttpMethod.GET, , callbackData);
                    codeSent = false;
                    time = 0;
                }
            }
        }
    }

    IEnumerator GetRequest(string url)
    {
        UnityWebRequest uwr = UnityWebRequest.Get($"{url}access_token={account.access_token}");
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.DataProcessingError || uwr.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            if (uwr.downloadHandler.data.Length > 0 && uwr.downloadHandler.error == string.Empty)
            {
                Debug.Log("Received: " + uwr.downloadHandler.text);
                FBBaseAccInfo info = JsonUtility.FromJson<FBBaseAccInfo>(uwr.downloadHandler.text);
                if (info.name == null)
                {
                    Debug.Log("Cant get info");
                }
                else
                {
                    StartCoroutine(GetRequest2($"https://graph.facebook.com/{info.id}?fields=id,name,picture.width(300).height(300)&access_token={account.access_token}"));
                }
            }
        }
    }
    IEnumerator GetRequest2(string url)
    {
        UnityWebRequest uwr = UnityWebRequest.Get(url);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.DataProcessingError || uwr.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            if (uwr.downloadHandler.data.Length > 0 && uwr.downloadHandler.error == string.Empty)
            {
                Debug.Log("Received: " + uwr.downloadHandler.text);
                var accInfo = Json.Deserialize(uwr.downloadHandler.text) as Dictionary<string, object>; ;
                if (accInfo["name"] == null)
                {
                    Debug.Log("Cant get info");
                }
                else
                {
                    info = new FBAccInfo();
                    info.picture = new Image();
                    info.picture.data = new ImageData();
                    info.id = (string)accInfo["id"];
                    info.name = (string)accInfo["name"];
                    if (accInfo.TryGetValue("picture", out Dictionary<string, object> picture))
                    {
                        if (picture.TryGetValue("data", out Dictionary<string, object> data))
                        {
                            info.picture.data.height = (long)data["height"];
                            info.picture.data.is_silhouette = (bool)data["is_silhouette"];
                            info.picture.data.url = (string)data["url"];
                            info.picture.data.width = (long)data["width"];
                            Debug.Log(data["url"].GetType());
                        }
                    }
                    else
                        Debug.Log("Cant find info");
                    Username.text = "Welcome, " + info.name;
                    StartCoroutine(DownloadImage(info.picture.data.url));
                }
            }
        }
    }

    IEnumerator DownloadImage(string MediaUrl)
    {
        UnityWebRequest imagewr = UnityWebRequestTexture.GetTexture(MediaUrl);
        yield return imagewr.SendWebRequest();
        if (imagewr.result == UnityWebRequest.Result.ConnectionError || imagewr.result == UnityWebRequest.Result.DataProcessingError || imagewr.result == UnityWebRequest.Result.ProtocolError)
            Debug.Log(imagewr.error);
        else
        {
            image.texture = ((DownloadHandlerTexture)imagewr.downloadHandler).texture;
            request.SetActive(false);
            result.SetActive(true);
        }
    }

    public void Redirect()
    {
        Application.OpenURL("https://www.facebook.com/device");
        GUIUtility.systemCopyBuffer = response.user_code;
    }
}

public class FBResponse
{
    public string error;

    public string code;

    public string user_code;

    public string verification_uri;

    public uint expires_in;

    public uint interval;
}

public class FBAccount
{
    public string access_token;

    public uint expires_in;
}

public class FBBaseAccInfo
{
    public string name;

    public string id;
}

public class FBAccInfo
{
    public string name;

    public string id;

    //public string email;

    public Image picture;
}

public class Image
{
    public ImageData data;
}

public class ImageData
{
    public long height;
    public bool is_silhouette;
    public string url;
    public long width;
}