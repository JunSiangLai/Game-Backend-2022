using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using PlayFab;
using PlayFab.ClientModels;

public class PlayfabFriends : MonoBehaviour
{
    List<FriendInfo> _friends = null;
    public GameObject friendPrefab;
    public GameObject listContent;
    public InputField friendID;
    public GameObject tempSearch;

    // Unity
    void CreateFriendGameobject(FriendInfo info)
    {
        GameObject prefab = GameObject.Instantiate(friendPrefab, listContent.transform);
        prefab.GetComponent<FriendPrefab>().username.text = info.Username;
        prefab.GetComponent<FriendPrefab>().playfabID.text = $"ID: {info.FriendPlayFabId}";
        prefab.GetComponent<FriendPrefab>().lastLogin.text = $"Last Seen: {info.Profile.LastLogin.ToString()}";
    }
    void OnEnable()
    {
        GetFriends();
        tempSearch = null;
    }

    // Get
    void GetFriends()
    {
        PlayFabClientAPI.GetFriendsList(new GetFriendsListRequest
        {
            IncludeSteamFriends = false,
            IncludeFacebookFriends = false,
            XboxToken = null,
            ProfileConstraints = new PlayerProfileViewConstraints()
            {
                ShowDisplayName = true,
                ShowLastLogin = true
            }

        }, result => {
            _friends = result.Friends;
            DisplayFriends(_friends); // triggers your UI
        }, DisplayPlayFabError);
    }
    void DisplayFriends(List<FriendInfo> friendsCache)
    {
        Debug.Log(string.Format("There are {0} friend(s)", friendsCache.Count));
        foreach(Transform child in listContent.transform)
        {
            Destroy(child.gameObject);
        }
        friendsCache.ForEach(f => CreateFriendGameobject(f));
    }
    void DisplayPlayFabError(PlayFabError error) { Debug.Log(error.GenerateErrorReport()); }
    public void OnGetFriendsListClicked()
    {
        GetFriends();
        if (tempSearch != null)
            Destroy(tempSearch.gameObject);
    }

    // Add
    enum FriendIdType { PlayFabId, Username, Email, DisplayName };
    void AddFriend(FriendIdType idType, string friendId)
    {
        var request = new AddFriendRequest();
        switch (idType)
        {
            case FriendIdType.PlayFabId:
                request.FriendPlayFabId = friendId;
                break;
            case FriendIdType.Username:
                request.FriendUsername = friendId;
                break;
            case FriendIdType.Email:
                request.FriendEmail = friendId;
                break;
            case FriendIdType.DisplayName:
                request.FriendTitleDisplayName = friendId;
                break;
        }
        // Execute request and update friends when we are done
        PlayFabClientAPI.AddFriend(request, result => {
            Debug.Log("Friend added successfully!");
            GetFriends();
        }, DisplayPlayFabError);
    }
    public void OnAddFriendClicked()
    {
        // second parameter should be another player account in your title
        AddFriend(FriendIdType.PlayFabId, friendID.text);
        if (tempSearch != null)
            Destroy(tempSearch.gameObject);
    }

    // Remove
    void RemoveFriend(FriendIdType idType, string friendId)
    {
        var request = new RemoveFriendRequest();
        switch (idType)
        {
            case FriendIdType.PlayFabId:
                request.FriendPlayFabId = friendId;
                break;
        }
        // Execute request and update friends when we are done
        PlayFabClientAPI.RemoveFriend(request, result => {
            Debug.Log("Friend removed successfully!");
            GetFriends();
        }, DisplayPlayFabError);
    }

    public void OnRemoveFriendClicked()
    {
        // second parameter should be another player account in your title
        RemoveFriend(FriendIdType.PlayFabId, friendID.text);
        if (tempSearch != null)
            Destroy(tempSearch.gameObject);
    }
#region Profile
    public void ProfileRequest(string ID)
    {
        var request = new GetPlayerProfileRequest
        {
            PlayFabId = ID,
            ProfileConstraints = new PlayerProfileViewConstraints()
            {
                ShowDisplayName = true,
                ShowLastLogin = true
            }
        };

        PlayFabClientAPI.GetPlayerProfile(request, OnPlayerProfileUpdate, OnError);
    }

    void OnPlayerProfileUpdate(GetPlayerProfileResult result)
    {
        if (tempSearch != null)
            Destroy(tempSearch.gameObject);
        tempSearch = GameObject.Instantiate(friendPrefab, listContent.transform);
        tempSearch.GetComponent<FriendPrefab>().username.text = result.PlayerProfile.DisplayName;
        tempSearch.GetComponent<FriendPrefab>().playfabID.text = $"ID: {result.PlayerProfile.PlayerId}";
        tempSearch.GetComponent<FriendPrefab>().lastLogin.text = $"Last Seen: {result.PlayerProfile.LastLogin.ToString()}";
        tempSearch.GetComponent<Image>().color = new Color(0.5f, 0.5f, 1f, 0.7f);
    }

#endregion

    void OnError(PlayFabError error)
    {
        Debug.Log(error.GenerateErrorReport());
    }
}
