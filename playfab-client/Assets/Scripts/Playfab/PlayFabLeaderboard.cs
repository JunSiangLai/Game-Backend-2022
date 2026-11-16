using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using PlayFab;
using PlayFab.ClientModels;

public class PlayFabLeaderboard : MonoBehaviour
{
    int score { get; set; }
    public GameObject leaderboardPrefab;
    public GameObject listContent;
    public Text scoreText;
    public Text buffcountText;
    public ProfilePrefab profilePrefab;
    bool buffItem;
    string itemID;
    float buffTime;

    void Start()
    {
        InvokeRepeating("GetScoreFromServer", 0.2f, 0.25f);
        InvokeRepeating("InventoryRequest", 0.2f, 0.25f);
        buffItem = false;
        itemID = "";

        AccountRequest();
    }

    void Update()
    {
        if (buffTime > 0f)
        {
            buffTime -= Time.deltaTime;
            if (buffTime <= 0f)
                buffItem = false;
        }
    }

#region Score/VirtualCurrency

    public void OnAddScoreClicked()
    {
        score = 1;
        if (buffItem)
            score = 2;
        
        var request = new AddUserVirtualCurrencyRequest
        {
            Amount = score,
            VirtualCurrency = "MS"
        };

        PlayFabClientAPI.AddUserVirtualCurrency(request, UpdateScore, OnError);
    }

    void OnForceUpdateScore()
    {
        var request = new AddUserVirtualCurrencyRequest
        {
            Amount = 0,
            VirtualCurrency = "MS"
        };

        PlayFabClientAPI.AddUserVirtualCurrency(request, UpdateScore, OnError);
    }

    void UpdateScore(ModifyUserVirtualCurrencyResult result)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "Mesos",
                    Value = result.Balance,
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnScoreUpdate, OnError);
    }

    void OnScoreUpdate(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("Successful update statistics");
        ChanceBuff();
    }

    void OnMushmomScoreUpdate(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("Successful update statistics");
    }

    public void GetScoreFromServer()
    {
        var request = new GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string>
            {
                "Mesos"
            }
        };
        
        PlayFabClientAPI.GetPlayerStatistics(request, UpdateScoreOnClient, OnError);
    }
    
    void UpdateScoreOnClient(GetPlayerStatisticsResult result)
    {
        foreach (var eachStat in result.Statistics)
            score = eachStat.Value;

        scoreText.text = score.ToString();
    }

    void ChanceBuff()
    {
        int chance  = Random.Range(0,5);
        if (chance != 0)
            return;

        var request = new PurchaseItemRequest
        {
            CatalogVersion = "Inventory",
            ItemId = "Mushmom",
            VirtualCurrency = "MS",
            Price = 5
        };
        
        PlayFabClientAPI.PurchaseItem(request, BuffResult, OnError);
    }

    void BuffResult(PurchaseItemResult result)
    {
        //Debug.Log(result.CustomData.ToString());
        OnForceUpdateScore();
    }

#endregion

#region Leaderboard
    public void OnGetLeaderboardClicked(bool Mesos)
    {
        string stat = "Mesos";
        if (!Mesos)
            stat = "Mushmoms";
        var request = new GetLeaderboardRequest
        {
            StatisticName = stat,
            StartPosition = 0,
            MaxResultsCount = 10
        };
        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardDisplay, OnError);
    }

    void OnLeaderboardDisplay(GetLeaderboardResult result)
    {
        foreach(Transform child in listContent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        foreach (var entry in result.Leaderboard)
        {
            CreateLeaderboardGameobject(entry.Position, entry.DisplayName, entry.PlayFabId, entry.StatValue);
        }
    }

    void CreateLeaderboardGameobject(int position, string username, string ID, int value)
    {
        GameObject prefab = GameObject.Instantiate(leaderboardPrefab, listContent.transform);
        prefab.GetComponent<LeaderboardPrefab>().username.text = username;
        prefab.GetComponent<LeaderboardPrefab>().playfabID.text = ID;
        prefab.GetComponent<LeaderboardPrefab>().value.text = value.ToString();
        prefab.GetComponent<LeaderboardPrefab>().position.text = position.ToString();
    }

#endregion

#region Inventory
    void InventoryRequest()
    {
        var request = new GetUserInventoryRequest
        {

        };

        PlayFabClientAPI.GetUserInventory(request, OnInventoryUpdate, OnError);
    }

    void OnInventoryUpdate(GetUserInventoryResult result)
    {
        buffcountText.text = result.Inventory.Count.ToString();

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "Mushmoms",
                    Value = result.Inventory.Count,
                }
            }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnMushmomScoreUpdate, OnError);

        foreach (var item in result.Inventory)
        {
            if (item.RemainingUses != 0)
            {
                itemID = item.ItemInstanceId;
            }
        }
    }

    public void ConsumeItemRequest()
    {
        if (buffcountText.text == "0")
            return;
        var request = new ConsumeItemRequest
        {
            ConsumeCount = 1,
            ItemInstanceId = itemID
        };

        PlayFabClientAPI.ConsumeItem(request, OnConsumeItemUpdate, OnConsumeItemFail);
    }

    void OnConsumeItemUpdate(ConsumeItemResult result)
    {
        buffItem = true;
        buffTime = 10f;
    }

    void OnConsumeItemFail(PlayFabError error)
    {
        Debug.Log(error.GenerateErrorReport());
    }

#endregion

#region Account
    void AccountRequest()
    {
        var request = new GetAccountInfoRequest
        {
        };

        PlayFabClientAPI.GetAccountInfo(request, OnAccountUpdate, OnError);
    }

    void OnAccountUpdate(GetAccountInfoResult result)
    {
        profilePrefab.name.text = $"Name: {result.AccountInfo.Username}";
        profilePrefab.id.text = $"ID: {result.AccountInfo.PlayFabId}";
        profilePrefab.email.text = $"Email: {result.AccountInfo.PrivateInfo.Email}";
    }

#endregion

    void OnError(PlayFabError error)
    {
        Debug.Log(error.GenerateErrorReport());
    }
}
